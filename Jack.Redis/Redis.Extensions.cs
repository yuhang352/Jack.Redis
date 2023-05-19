using Jack.Common.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using System;

namespace Jack.Redis.Extensions
{
    public static class RedisExtensions
    {
        public static IServiceCollection AddJackRedisStep(this IServiceCollection services, IConfiguration configuration)
        {
            if (services == null)
            {
                throw new ArgumentNullException("services");
            }

            services.AddTransient<IRedisRepository, RedisRepository>();
            RedisOption redisOption = (configuration.GetSection("RedisConfig").Value).DeserializeObject<RedisOption>();
            ConfigurationOptions configurationOptions = ConfigurationOptions.Parse(redisOption.RedisConnectionString, ignoreUnknown: true);
            configurationOptions.AbortOnConnectFail = false;
            ConnectionMultiplexer connectionMultiplexer = ConnectionMultiplexer.Connect(configurationOptions);
            connectionMultiplexer.ConnectionFailed += MuxerConnectionFailed;
            connectionMultiplexer.ConnectionRestored += MuxerConnectionRestored;
            connectionMultiplexer.ErrorMessage += MuxerErrorMessage;
            connectionMultiplexer.ConfigurationChanged += MuxerConfigurationChanged;
            connectionMultiplexer.HashSlotMoved += MuxerHashSlotMoved;
            connectionMultiplexer.InternalError += MuxerInternalError;
            services.AddSingleton(connectionMultiplexer);
            return services;
        }

        private static void MuxerConfigurationChanged(object sender, EndPointEventArgs e)
        {
            Console.WriteLine("Configuration changed: " + e.EndPoint);
        }

        private static void MuxerErrorMessage(object sender, RedisErrorEventArgs e)
        {
            Console.WriteLine("ErrorMessage: " + e.Message);
        }

        private static void MuxerConnectionRestored(object sender, ConnectionFailedEventArgs e)
        {
            Console.WriteLine("ConnectionRestored: " + e.EndPoint);
        }

        private static void MuxerConnectionFailed(object sender, ConnectionFailedEventArgs e)
        {
            Console.WriteLine("重新连接：Endpoint failed: " + e.EndPoint?.ToString() + ", " + e.FailureType.ToString() + ((e.Exception == null) ? "" : (", " + e.Exception.Message)));
        }

        private static void MuxerHashSlotMoved(object sender, HashSlotMovedEventArgs e)
        {
            Console.WriteLine("HashSlotMoved:NewEndPoint" + e.NewEndPoint?.ToString() + ", OldEndPoint" + e.OldEndPoint);
        }

        private static void MuxerInternalError(object sender, InternalErrorEventArgs e)
        {
            Console.WriteLine("InternalError:Message" + e.Exception.Message);
        }
    }
}
