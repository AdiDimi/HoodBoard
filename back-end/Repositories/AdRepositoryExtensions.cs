using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace AdsApi.Repositories;

public enum RepositoryMode { InMemory, RedisJson }

public sealed class AdsRepositorySettings
{
    public RepositoryMode Mode { get; set; } = RepositoryMode.RedisJson;
    public string? RedisConnection { get; set; } = "redis:6379";
    public int IdempotencyTtlSeconds { get; set; } = 600;
    public bool UseOutboxWriter { get; set; } = true;
    public int OutboxLockTtlSeconds { get; set; } = 30; // TTL for distributed lock when writing json
    public bool ForceRebuild { get; set; } = false; // If true, clear Redis dataset at startup and reseed from Data
}

public static class AdRepositoryExtensions
{
    private const string SectionName = "Ads:Repository";

    public static IServiceCollection AddAdsRepository(this IServiceCollection services, IConfiguration config, Action<AdsRepositorySettings>? overrideCfg = null)
    {
        services.AddOptions<AdsRepositorySettings>().Bind(config.GetSection(SectionName)).PostConfigure(o => overrideCfg?.Invoke(o));

        // Read config early to decide which implementations to register
        var settings = config.GetSection(SectionName).Get<AdsRepositorySettings>() ?? new AdsRepositorySettings();

        if (settings.Mode == RepositoryMode.RedisJson)
        {
            try
            {
                var mux = ConnectionMultiplexer.Connect(settings.RedisConnection ?? "localhost:6379");
                services.AddSingleton<IConnectionMultiplexer>(sp => mux);

                services.AddSingleton<IAdRepository>(sp => {
                    var opts = sp.GetRequiredService<IOptions<AdsRepositorySettings>>();
                    return new AdRedisJsonRepository(mux, opts);
                });

                if (settings.UseOutboxWriter)
                {
                    services.AddHostedService<AdsApi.Workers.OutboxWriter>();
                }
            }
            catch
            {
                services.AddSingleton<IAdRepository, InMemoryAdRepository>();
            }
        }
        else
        {
            services.AddSingleton<IAdRepository, InMemoryAdRepository>();
        }

        return services;
    }

    public static async Task InitializeAdsRepositoryAsync(this IHost app)
    {
        using var scope = app.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IAdRepository>();
        await repo.InitializeAsync();
    }
}
