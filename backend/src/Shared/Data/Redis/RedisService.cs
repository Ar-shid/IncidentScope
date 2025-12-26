using StackExchange.Redis;

namespace IncidentScope.Data.Redis;

public class RedisService
{
    private readonly IConnectionMultiplexer _redis;

    public RedisService(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public IDatabase GetDatabase(int db = -1) => _redis.GetDatabase(db);

    public async Task<bool> AcquireLockAsync(string key, TimeSpan expiry, string? value = null)
    {
        var db = GetDatabase();
        value ??= Guid.NewGuid().ToString();
        return await db.StringSetAsync(key, value, expiry, When.NotExists);
    }

    public async Task ReleaseLockAsync(string key, string? value = null)
    {
        var db = GetDatabase();
        if (value != null)
        {
            // Lua script to ensure we only delete if value matches
            const string script = @"
                if redis.call('get', KEYS[1]) == ARGV[1] then
                    return redis.call('del', KEYS[1])
                else
                    return 0
                end";
            await db.ScriptEvaluateAsync(script, new RedisKey[] { key }, new RedisValue[] { value });
        }
        else
        {
            await db.KeyDeleteAsync(key);
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        var db = GetDatabase();
        var json = System.Text.Json.JsonSerializer.Serialize(value);
        await db.StringSetAsync(key, json, expiry);
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        var db = GetDatabase();
        var json = await db.StringGetAsync(key);
        if (!json.HasValue) return default;
        return System.Text.Json.JsonSerializer.Deserialize<T>(json!);
    }
}

