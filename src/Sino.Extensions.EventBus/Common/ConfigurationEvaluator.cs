using Sino.Extensions.EventBus.Configuration;
using Sino.Extensions.EventBus.Attributes;
using System;
using System.Reflection;

namespace Sino.Extensions.EventBus.Common
{
    /// <summary>
    /// 提供Event相关配置参数
    /// </summary>
    public class ConfigurationEvaluator : IConfigurationEvaluator
    {
        private readonly RabbitMqConfiguration _clientConfig;
        private readonly INamingConventions _conventions;

        public ConfigurationEvaluator(RabbitMqConfiguration clientConfig, INamingConventions conventions)
        {
            _clientConfig = clientConfig;
            _conventions = conventions;
        }

        #region IConfigurationEvaluator

        public SubscriptionConfiguration GetConfiguration<TMessage>(Action<ISubscriptionConfigurationBuilder> configuration)
        {
            return GetConfiguration(typeof(TMessage), configuration);
        }

        public SubscriptionConfiguration GetConfiguration(Type messageType, Action<ISubscriptionConfigurationBuilder> configuration = null)
        {
            configuration = configuration ?? (builder => { });

            // 根据注解属性获取Exchange、Queue和Routing Key值
            configuration = (builder =>
            {
                builder
                    .WithExchange(ExchangeAction(messageType))
                    .WithQueue(QueueAction(messageType));

                var routingAttr = GetAttribute<RoutingAttribute>(messageType);
                if (routingAttr != null)
                {
                    if (routingAttr.NullableNoAck.HasValue)
                    {
                        builder.WithNoAck(routingAttr.NullableNoAck.Value);
                    }
                    if (routingAttr.PrefetchCount > 0)
                    {
                        builder.WithPrefetchCount(routingAttr.PrefetchCount);
                    }
                    if (routingAttr.RoutingKey != null)
                    {
                        builder.WithRoutingKey(routingAttr.RoutingKey);
                    }
                }
            }) + configuration;

            var routingKey = _conventions.QueueNamingConvention(messageType);
            var queueConfig = new QueueConfiguration(_clientConfig.Queue)
            {
                QueueName = routingKey,
                NameSuffix = _conventions.SubscriberQueueSuffix(messageType)
            };

            var exchangeConfig = new ExchangeConfiguration(_clientConfig.Exchange)
            {
                ExchangeName = _conventions.ExchangeNamingConvention(messageType)
            };

            var cfgBuilder = new SubscriptionConfigurationBuilder(queueConfig, exchangeConfig, routingKey);
            configuration?.Invoke(cfgBuilder);
            return cfgBuilder.Configuration;
        }

        public PublishConfiguration GetConfiguration<TMessage>(Action<IPublishConfigurationBuilder> configuration)
        {
            return GetConfiguration(typeof(TMessage), configuration);
        }

        public PublishConfiguration GetConfiguration(Type messageType, Action<IPublishConfigurationBuilder> configuration)
        {
            configuration = configuration ?? (builder => { });
            
            // 根据注解属性获取Exchange和Routing Key值
            configuration = (builder =>
            {
                builder.WithExchange(ExchangeAction(messageType));
                var routingAttr = GetAttribute<RoutingAttribute>(messageType);
                if (routingAttr?.RoutingKey != null)
                {
                    builder.WithRoutingKey(routingAttr.RoutingKey);
                }
            }) + configuration;

            var exchangeConfig = new ExchangeConfiguration(_clientConfig.Exchange)
            {
                ExchangeName = _conventions.ExchangeNamingConvention(messageType)
            };
            var routingKey = _conventions.QueueNamingConvention(messageType);
            var cfgBuilder = new PublishConfigurationBuilder(exchangeConfig, routingKey);
            configuration?.Invoke(cfgBuilder);
            return cfgBuilder.Configuration;
        }

        #endregion

        private static TAttribute GetAttribute<TAttribute>(Type type) where TAttribute : Attribute
        {
            var attr = type.GetTypeInfo().GetCustomAttribute<TAttribute>();
            return attr;
        }

        private static Action<IExchangeConfigurationBuilder> ExchangeAction(Type messageType)
        {
            var exchangeAttr = GetAttribute<ExchangeAttribute>(messageType);
            if (exchangeAttr == null)
            {
                return builder => { };
            }
            return builder =>
            {
                if (!string.IsNullOrWhiteSpace(exchangeAttr.Name))
                {
                    builder.WithName(exchangeAttr.Name);
                }
                if (exchangeAttr.NullableDurability.HasValue)
                {
                    builder.WithDurability(exchangeAttr.NullableDurability.Value);
                }
                if (exchangeAttr.NullableAutoDelete.HasValue)
                {
                    builder.WithDurability(exchangeAttr.NullableAutoDelete.Value);
                }
                if (exchangeAttr.Type != ExchangeType.Unknown)
                {
                    builder.WithType(exchangeAttr.Type);
                }
            };
        }

        private static Action<IQueueConfigurationBuilder> QueueAction(Type messageType)
        {
            var queueAttr = GetAttribute<QueueAttribute>(messageType);
            if (queueAttr == null)
            {
                return builder => { };
            }
            return builder =>
            {
                if (!string.IsNullOrWhiteSpace(queueAttr.Name))
                {
                    builder.WithName(queueAttr.Name);
                }
                if (queueAttr.NullableDurability.HasValue)
                {
                    builder.WithDurability(queueAttr.NullableDurability.Value);
                }
                if (queueAttr.NullableExclusitivy.HasValue)
                {
                    builder.WithDurability(queueAttr.NullableExclusitivy.Value);
                }
                if (queueAttr.NullableAutoDelete.HasValue)
                {
                    builder.WithDurability(queueAttr.NullableAutoDelete.Value);
                }
                if (queueAttr.MessageTtl > 0)
                {
                    builder.WithArgument(QueueArgument.MessageTtl, queueAttr.MessageTtl);
                }
                if (queueAttr.MaxPriority > 0)
                {
                    builder.WithArgument(QueueArgument.MaxPriority, queueAttr.MaxPriority);
                }
                if (!string.IsNullOrWhiteSpace(queueAttr.DeadLeterExchange))
                {
                    builder.WithArgument(QueueArgument.DeadLetterExchange, queueAttr.DeadLeterExchange);
                }
                if (!string.IsNullOrWhiteSpace(queueAttr.Mode))
                {
                    builder.WithArgument(QueueArgument.QueueMode, queueAttr.Mode);
                }
            };
        }
    }
}
