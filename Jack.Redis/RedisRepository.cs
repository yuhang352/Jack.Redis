using Jack.Common.Extensions;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Jack.Redis
{
    public class RedisRepository : IRedisRepository
    {
        private readonly ConnectionMultiplexer _conn;

        private IDatabase _database;

        public RedisRepository(ConnectionMultiplexer conn)
        {
            Console.WriteLine("RedisRepository:" + conn.Configuration);
            if (conn == null || !conn.IsConnected)
            {
                throw new Exception("no load redis.json file，connection fail");
            }

            _conn = conn;
            _database = _conn.GetDatabase();
        }

        private IServer GetServer()
        {
            EndPoint[] endPoints = _conn.GetEndPoints();
            return _conn.GetServer(endPoints.First());
        }

        public void ChangeDatabase(int dbNum = -1)
        {
            _database = _conn.GetDatabase(dbNum);
        }

        public async Task<bool> SetAsync(string key, object value, DateTime expiry)
        {
            string text = value as string;
            if (text != null)
            {
                return await _database.StringSetAsync(key, text, expiry - DateTime.Now);
            }

            return await _database.StringSetAsync(key, value.SerializeObject(), expiry - DateTime.Now);
        }

        public async Task<bool> SetAsync(string key, object value, TimeSpan? expiry = null)
        {
            string text = value as string;
            if (text != null)
            {
                return await _database.StringSetAsync(key, text, expiry);
            }

            return await _database.StringSetAsync(key, value.SerializeObject(), expiry);
        }

        public async Task<bool> SetAsync(List<KeyValuePair<RedisKey, RedisValue>> keyValues)
        {
            List<KeyValuePair<RedisKey, RedisValue>> list = keyValues.Select((KeyValuePair<RedisKey, RedisValue> p) => new KeyValuePair<RedisKey, RedisValue>(p.Key, p.Value)).ToList();
            return await _database.StringSetAsync(list.ToArray());
        }

        public async Task<string> GetValueAsync(string key)
        {
            return await _database.StringGetAsync(key);
        }

        public async Task<T> GetValueAsync<T>(string key)
        {
            RedisValue redisValue = await _database.StringGetAsync(key);
            if (redisValue.HasValue)
            {
                return JsonSerializerExtensions.DeserializeObject<T>(redisValue);
            }

            return default(T);
        }

        public async Task<RedisValue[]> GetValueAsync(List<string> listKey)
        {
            RedisKey[] keys = ((IEnumerable<string>)listKey.ToList()).Select((Func<string, RedisKey>)((string redisKey) => redisKey)).ToArray();
            return await _database.StringGetAsync(keys);
        }

        public async Task<bool> ExistAsync(string key)
        {
            return await _database.KeyExistsAsync(key);
        }

        public async Task<bool> RemoveAsync(string key)
        {
            return await _database.KeyDeleteAsync(key);
        }

        public async Task ClearAsync()
        {
            EndPoint[] endPoints = _conn.GetEndPoints();
            for (int i = 0; i < endPoints.Length; i++)
            {
                _ = endPoints[i];
                IServer server = GetServer();
                foreach (RedisKey item in server.Keys(-1, default(RedisValue), 250, 0L))
                {
                    await _database.KeyDeleteAsync(item);
                }
            }
        }

        public async Task<bool> HashExistsAsync(string key, string dataKey)
        {
            return await _database.HashExistsAsync(key, dataKey);
        }

        public async Task<bool> HashSetAsync<T>(string key, string dataKey, T t)
        {
            return await _database.HashSetAsync(key, dataKey, t.SerializeObject());
        }

        public async Task<bool> HashDeleteAsync(string key, string dataKey)
        {
            return await _database.HashDeleteAsync(key, dataKey);
        }

        public async Task<long> HashDeleteAsync(string key, List<RedisValue> dataKeys)
        {
            return await _database.HashDeleteAsync(key, dataKeys.ToArray());
        }

        public async Task<T> HashGeAsync<T>(string key, string dataKey)
        {
            return JsonSerializerExtensions.DeserializeObject<T>(await _database.HashGetAsync(key, dataKey));
        }

        public async Task<double> HashIncrementAsync(string key, string dataKey, double val = 1.0)
        {
            return await _database.HashIncrementAsync(key, dataKey, val);
        }

        public async Task<double> HashDecrementAsync(string key, string dataKey, double val = 1.0)
        {
            return await _database.HashDecrementAsync(key, dataKey, val);
        }

        public async Task<List<T>> HashKeysAsync<T>(string key)
        {
            return ConvetList<T>(await _database.HashKeysAsync(key));
        }

        public async Task<List<T>> HashValuesAsync<T>(string key)
        {
            List<T> result = new List<T>();
            HashEntry[] array = await _database.HashGetAllAsync(key);
            for (int i = 0; i < array.Length; i++)
            {
                HashEntry hashEntry = array[i];
                _ = (string)hashEntry.Name;
                if (!hashEntry.Value.IsNullOrEmpty)
                {
                    T item = JsonSerializerExtensions.DeserializeObject<T>(hashEntry.Value);
                    result.Add(item);
                }
            }

            return result;
        }

        public async Task<HashEntry[]> HashValueAllAsync(string key)
        {
            return await _database.HashGetAllAsync(key);
        }

        public async Task<long> ListRemoveAsync<T>(string key, T value)
        {
            string text = ((value is string) ? value.ToString() : value.SerializeObject());
            return await _database.ListRemoveAsync(key, text, 0L);
        }

        public async Task<List<T>> ListRangeAsync<T>(string key)
        {
            return ConvetList<T>(await _database.ListRangeAsync(key, 0L, -1L));
        }

        public async Task<long> ListRightPushAsync<T>(string key, T value)
        {
            return await _database.ListRightPushAsync(key, ConvertJson(value));
        }

        public async Task<T> ListRightPopAsync<T>(string key)
        {
            string text = await _database.ListRightPopAsync(key);
            if (typeof(T).Name.Equals(typeof(string).Name))
            {
                return JsonConvert.DeserializeObject<T>("'" + text + "'");
            }

            return text.DeserializeObject<T>();
        }

        public async Task<long> ListLeftPushAsync<T>(string key, T value)
        {
            return await _database.ListLeftPushAsync(key, ConvertJson(value));
        }

        public async Task<T> ListLeftPopAsync<T>(string key)
        {
            string text = await _database.ListLeftPopAsync(key);
            if (typeof(T).Name.Equals(typeof(string).Name))
            {
                return JsonConvert.DeserializeObject<T>("'" + text + "'");
            }

            return text.DeserializeObject<T>();
        }

        public async Task<long> ListLengthAsync(string key)
        {
            return await _database.ListLengthAsync(key);
        }

        public async Task<bool> SortedSetAddAsync<T>(string key, T value, double score)
        {
            return await _database.SortedSetAddAsync(key, ConvertJson(value), score);
        }

        public async Task<bool> SortedSetRemoveAsync<T>(string key, T value)
        {
            return await _database.SortedSetRemoveAsync(key, ConvertJson(value));
        }

        public async Task<List<T>> SortedSetRangeByRankAsync<T>(string key)
        {
            return ConvetList<T>(await _database.SortedSetRangeByRankAsync(key, 0L, -1L));
        }

        public async Task<long> SortedSetLengthAsync(string key)
        {
            return await _database.SortedSetLengthAsync(key);
        }

        public void Subscribe(string subChannel, Action<RedisChannel, RedisValue> handler = null)
        {
            _conn.GetSubscriber().Subscribe(subChannel, delegate (RedisChannel channel, RedisValue message)
            {
                if (handler == null)
                {
                    Console.WriteLine(subChannel + " 订阅收到消息：" + (string)message);
                }
                else
                {
                    handler(channel, message);
                }
            });
        }

        public void SubscribeAsync(string subChannel, Action<RedisChannel, RedisValue> handler = null)
        {
            _conn.GetSubscriber().SubscribeAsync(subChannel, delegate (RedisChannel channel, RedisValue message)
            {
                if (handler == null)
                {
                    Console.WriteLine(subChannel + " 订阅收到消息：" + (string)message);
                }
                else
                {
                    handler(channel, message);
                }
            });
        }

        public long Publish<T>(string channel, T msg)
        {
            return _conn.GetSubscriber().Publish(channel, ConvertJson(msg));
        }

        public Task<long> PublishAsync<T>(string channel, T msg)
        {
            return _conn.GetSubscriber().PublishAsync(channel, ConvertJson(msg));
        }

        public void Unsubscribe(string channel)
        {
            _conn.GetSubscriber().Unsubscribe(channel);
        }

        public void UnsubscribeAsync(string channel)
        {
            _conn.GetSubscriber().UnsubscribeAsync(channel);
        }

        public void UnsubscribeAll()
        {
            _conn.GetSubscriber().UnsubscribeAll();
        }

        public void UnsubscribeAllAsync()
        {
            _conn.GetSubscriber().UnsubscribeAllAsync();
        }

        private string ConvertJson<T>(T value)
        {
            if (!(value is string))
            {
                return value.SerializeObject();
            }

            return value.ToString();
        }

        private List<T> ConvetList<T>(RedisValue[] values)
        {
            List<T> list = new List<T>();
            for (int i = 0; i < values.Length; i++)
            {
                T item = JsonConvert.DeserializeObject<T>((string)values[i]);
                list.Add(item);
            }

            return list;
        }
    }
}
