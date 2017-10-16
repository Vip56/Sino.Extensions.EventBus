using RabbitMQ.Client;
using RabbitMQ.Client.Framing;
using Sino.Extensions.EventBus.Configuration;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Sino.Extensions.EventBus.Common
{
    /// <summary>
    /// 提供Event发送所携带的附加信息
    /// </summary>
    public class BasicPropertiesProvider : IBasicPropertiesProvider
    {
        private readonly RabbitMqConfiguration _config;

        public BasicPropertiesProvider(RabbitMqConfiguration config)
        {
            _config = config;
        }

        public IBasicProperties GetProperties<TMessage>(Action<IBasicProperties> custom = null)
        {
            var properties = new BasicProperties
            {
                MessageId = Guid.NewGuid().ToString(),
                Headers = new Dictionary<string, object>(),
                Persistent = _config.PersistentDeliveryMode
            };
            custom?.Invoke(properties);
            properties.Headers.Add(PropertyHeaders.Sent, DateTime.UtcNow.ToString("u"));
            properties.Headers.Add(PropertyHeaders.MessageType, GetTypeName(typeof(TMessage)));
            return properties;
        }

        private string GetTypeName(Type type)
        {
            var name = $"{type.Namespace}.{type.Name}";
            if (type.GenericTypeArguments.Length > 0)
            {
                var shouldInsertComma = false;
                name += '[';
                foreach (var genericType in type.GenericTypeArguments)
                {
                    if (shouldInsertComma)
                        name += ",";
                    name += $"[{GetTypeName(genericType)}]";
                    shouldInsertComma = true;
                }
                name += ']';
            }
            name += $", {type.GetTypeInfo().Assembly.GetName().Name}";
            return name;
        }
    }
}
