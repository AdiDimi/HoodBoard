namespace AdsApi.Repositories;

public static class RedisScripts
{
    public const string UpsertJsonWithOutboxAndIdem = @"
local adKey     = KEYS[1]
local indexKey  = KEYS[2]
local streamKey = KEYS[3]
local idemKey   = KEYS[4]

local payload   = ARGV[1]
local op        = ARGV[2]
local adId      = ARGV[3]
local ts        = ARGV[4]
local ttlSec    = tonumber(ARGV[5])

if redis.call('EXISTS', idemKey) == 1 then
  return 0
end

redis.call('JSON.SET', adKey, '$', payload)
redis.call('SADD', indexKey, adId)
redis.call('XADD', streamKey, '*', 'op', op, 'id', adId, 'ts', ts)
redis.call('SET', idemKey, adId, 'EX', ttlSec)

return 1
";
}
