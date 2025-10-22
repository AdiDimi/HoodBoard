using System.Diagnostics;
using System.Text.Json;
using AdsApi.Repositories;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace AdsApi.Workers;

public sealed class OutboxWriter : BackgroundService
{
    private readonly IDatabase _db;
    private readonly ILogger<OutboxWriter> _log;
    private readonly string _stream = "ads-outbox";
    private readonly string _group  = "writer";
    private readonly string _consumer = Environment.MachineName + "-" + Guid.NewGuid().ToString("N")[..6];
    private readonly string _jsonPath;
    private readonly JsonSerializerOptions _json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };
    private readonly int _lockTtlSeconds;
    private readonly TimeSpan _lockTtl;

    public OutboxWriter(IConnectionMultiplexer mux, IWebHostEnvironment env, ILogger<OutboxWriter> log, IOptions<AdsRepositorySettings> opts)
    {
        _db = mux.GetDatabase();
        _log = log;
        _jsonPath = Path.Combine(env.ContentRootPath, "Data", "ads.json");
        Directory.CreateDirectory(Path.GetDirectoryName(_jsonPath)!);
        _lockTtlSeconds = opts?.Value?.OutboxLockTtlSeconds > 0 ? opts.Value.OutboxLockTtlSeconds : 30;
        _lockTtl = TimeSpan.FromSeconds(_lockTtlSeconds);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using (_log.BeginScope(new Dictionary<string, object?> { ["worker"]="OutboxWriter", ["stream"]=_stream, ["group"]=_group, ["consumer"]=_consumer }))
        {
            _log.LogInformation("Outbox writer starting");
            try { await _db.StreamCreateConsumerGroupAsync(_stream, _group, "0-0", createStream: true ); } catch {}
          
            var batchSize = 200;
            var flushEvery = TimeSpan.FromMilliseconds(500);
            var last = Stopwatch.StartNew();
            var pending = new List<StreamEntry>();

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var entries = await _db.StreamReadGroupAsync(_stream, _group, _consumer, ">", batchSize);
                    if (entries.Length > 0) { pending.AddRange(entries); _log.LogDebug("Pulled {Count} outbox events", entries.Length); }

                    if (pending.Count >= batchSize || last.Elapsed >= flushEvery)
                    {
                        if (pending.Count > 0)
                        {
                            var swFlush = Stopwatch.StartNew();
                            var ads = await LoadAllAdsFromRedisJson();
                            // Attempt to acquire distributed lock and write
                            var wrote = await TryWriteWithLockAsync(ads, stoppingToken);
                            if (wrote)
                            {
                                foreach (var e in pending) await _db.StreamAcknowledgeAsync(_stream, _group, e.Id);
                                swFlush.Stop();
                                _log.LogInformation("Flushed {Count} events to json in {ElapsedMs} ms", pending.Count, swFlush.ElapsedMilliseconds);
                                pending.Clear();
                            }
                            else
                            {
                                _log.LogWarning("Failed to acquire lock to write outbox; will retry later and keep pending events");
                            }
                        }
                        last.Restart();
                    }

                    if (entries.Length == 0) await Task.Delay(50, stoppingToken);
                }
                catch (OperationCanceledException) { }
                catch (Exception ex) { _log.LogError(ex, "Outbox loop error (will retry)"); await Task.Delay(2000, stoppingToken); }
            }

            if (pending.Count > 0)
            {
                var ads = await LoadAllAdsFromRedisJson();
                var wrote = await TryWriteWithLockAsync(ads, stoppingToken);
                if (wrote)
                {
                    foreach (var e in pending) await _db.StreamAcknowledgeAsync(_stream, _group, e.Id);
                    _log.LogInformation("Flushed remaining {Count} events before stop", pending.Count);
                }
                else
                {
                    _log.LogWarning("Could not acquire lock to flush remaining events before stop");
                }
            }
            _log.LogInformation("Outbox writer stopped");
        }
    }

    private async Task<bool> TryWriteWithLockAsync(List<Ad> ads, CancellationToken ct)
    {
        var lockKey = "ads:json:lock";
        var token = Guid.NewGuid().ToString("N");
        var maxWait = TimeSpan.FromSeconds(10);
        var sw = Stopwatch.StartNew();
        var attempt = 0;
        while (sw.Elapsed < maxWait && !ct.IsCancellationRequested)
        {
            attempt++;
            try
            {
                var acquired = await _db.LockTakeAsync(lockKey, token, _lockTtl);
                if (acquired)
                {
                    try
                    {
                        var tmp = _jsonPath + $".{_consumer}.tmp";
                        await using (var fs = File.Create(tmp)) await System.Text.Json.JsonSerializer.SerializeAsync(fs, new { ads }, _json, ct);
                        // Replace target atomically
                        File.Replace(tmp, _jsonPath, null, true);
                        return true;
                    }
                    finally
                    {
                        try { await _db.LockReleaseAsync(lockKey, token); } catch (Exception ex) { _log.LogWarning(ex, "Failed to release redis lock"); }
                    }
                }
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Error while attempting to acquire redis lock (attempt {Attempt})", attempt);
            }

            // backoff
            await Task.Delay(Math.Min(500 * attempt, 2000), ct);
        }

        _log.LogWarning("Failed to acquire distributed lock after {ElapsedMs} ms", sw.ElapsedMilliseconds);
        return false;
    }

    private async Task<List<Ad>> LoadAllAdsFromRedisJson()
    {
        var ids = (await _db.SetMembersAsync("ads:index")).Select(v => (string)v).ToArray();
        if (ids.Length == 0) return new();
        var keys = ids.Select(id => (RedisKey)$"ads:{id}").ToArray();
        var res = await _db.JsonMGetAsync(keys, "$");
        var list = new List<Ad>(ids.Length);
        foreach (var item in (RedisResult[])res!)
        {
            if (item.IsNull) continue;
            using var doc = JsonDocument.Parse((string)item!);
            var elem = doc.RootElement[0].GetRawText();
            list.Add(System.Text.Json.JsonSerializer.Deserialize<Ad>(elem, _json)!);
        }
        return list.OrderByDescending(a => a.CreatedAt).ToList();
    }
}
