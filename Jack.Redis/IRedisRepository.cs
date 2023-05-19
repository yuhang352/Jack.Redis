using StackExchange.Redis;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace Jack.Redis
{
    public interface IRedisRepository
    {
        void ChangeDatabase(int dbNum = -1);

        Task<bool> SetAsync(string key, object value, DateTime expiry);

        Task<bool> SetAsync(string key, object value, TimeSpan? expiry = null);

        Task<bool> SetAsync(List<KeyValuePair<RedisKey, RedisValue>> keyValues);

        Task<string> GetValueAsync(string key);

        Task<T> GetValueAsync<T>(string key);

        Task<RedisValue[]> GetValueAsync(List<string> listKey);

        Task<bool> ExistAsync(string key);

        Task<bool> RemoveAsync(string key);

        Task ClearAsync();

        Task<bool> HashExistsAsync(string key, string dataKey);

        Task<bool> HashSetAsync<T>(string key, string dataKey, T t);

        Task<bool> HashDeleteAsync(string key, string dataKey);

        Task<long> HashDeleteAsync(string key, List<RedisValue> dataKeys);

        Task<T> HashGeAsync<T>(string key, string dataKey);

        Task<double> HashIncrementAsync(string key, string dataKey, double val = 1.0);

        Task<double> HashDecrementAsync(string key, string dataKey, double val = 1.0);

        Task<List<T>> HashKeysAsync<T>(string key);

        Task<List<T>> HashValuesAsync<T>(string key);

        Task<HashEntry[]> HashValueAllAsync(string key);

        Task<long> ListRemoveAsync<T>(string key, T value);

        Task<List<T>> ListRangeAsync<T>(string key);

        Task<long> ListRightPushAsync<T>(string key, T value);

        Task<T> ListRightPopAsync<T>(string key);

        Task<long> ListLeftPushAsync<T>(string key, T value);

        Task<T> ListLeftPopAsync<T>(string key);

        Task<long> ListLengthAsync(string key);

        Task<bool> SortedSetAddAsync<T>(string key, T value, double score);

        Task<bool> SortedSetRemoveAsync<T>(string key, T value);

        Task<List<T>> SortedSetRangeByRankAsync<T>(string key);

        Task<long> SortedSetLengthAsync(string key);

        void Subscribe(string subChannel, Action<RedisChannel, RedisValue> handler = null);

        void SubscribeAsync(string subChannel, Action<RedisChannel, RedisValue> handler = null);

        long Publish<T>(string channel, T msg);

        Task<long> PublishAsync<T>(string channel, T msg);

        void Unsubscribe(string channel);

        void UnsubscribeAsync(string channel);

        void UnsubscribeAll();

        void UnsubscribeAllAsync();
    }
}
