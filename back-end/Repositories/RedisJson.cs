using StackExchange.Redis;

namespace AdsApi.Repositories;

public static class RedisJson
{
    public static Task<RedisResult> JsonSetAsync(this IDatabase db, string key, string path, string json)
        => db.ExecuteAsync("JSON.SET", key, path, json);
    public static Task<RedisResult> JsonGetAsync(this IDatabase db, string key, string path = "$")
        => db.ExecuteAsync("JSON.GET", key, path);
    public static Task<RedisResult> JsonMGetAsync(this IDatabase db, RedisKey[] keys, string path = "$")
        => db.ExecuteAsync("JSON.MGET", keys.Cast<object>().Append(path).ToArray());
}
