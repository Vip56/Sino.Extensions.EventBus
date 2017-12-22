using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Sino.Extensions.EventBus.Configuration;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using Microsoft.Extensions.Logging;

namespace Sino.Extensions.EventBus.Consumer
{
    /// <summary>
    /// 事件消费者工厂
    /// </summary>
    public class EventingBasicConsumerFactory : IConsumerFactory
    {
        /// <summary>
        /// 记录已经被处理但未被ACK的事件
        /// </summary>
        private readonly ConcurrentBag<string> _processedButNotAcked;
        private readonly ILogger _logger;
        private readonly ILogger<EventingRawConsumer> _consumerLogger;

        public EventingBasicConsumerFactory(ILogger<EventingBasicConsumerFactory> logger, ILogger<EventingRawConsumer> consumerLogger)
        {
            _logger = logger;
            _consumerLogger = consumerLogger;
            _processedButNotAcked = new ConcurrentBag<string>();
        }

        public IRawConsumer CreateConsumer(IConsumerConfiguration cfg, IModel channel)
        {
            ConfigureQos(channel, cfg.PrefetchCount);
            var rawConsumer = new EventingRawConsumer(channel, _consumerLogger);

            rawConsumer.Received += (sender, args) =>
            {
                Task.Run(() =>
                {
                    if (_processedButNotAcked.Contains(args.BasicProperties.MessageId))
                    {
                        BasicAck(channel, args);
                        return;
                    }
                    _logger.LogInformation($"ConsumerMessageId: {args.BasicProperties.MessageId}");
                    rawConsumer
                        .OnMessageAsync(sender, args)
                        .ContinueWith(t =>
                        {
                            if (t.Status == TaskStatus.Faulted || t.Exception != null)
                            {
                                _logger.LogError(500, $"处理事件: MessageId: {args.BasicProperties.MessageId} 出现错误");
                                if (t.Exception != null)
                                {
                                    _logger.LogError(500, t.Exception, $"接收事件: MessageId: {args.BasicProperties.MessageId} 出现异常");
                                }
                                return;
                            }
                            if (cfg.NoAck || rawConsumer.AcknowledgedTags.Contains(args.DeliveryTag))
                            {
                                _logger.LogError(500, $"处理事件: MessageId: {args.BasicProperties.MessageId} 出现错误");
                                return;
                            }
                            BasicAck(channel, args);
                        });
                });
            };

            return rawConsumer;
        }

        protected void ConfigureQos(IModel channel, ushort prefetchCount)
        {
            _logger.LogDebug($"Setting QoS\n  Prefetch Size: 0\n  Prefetch Count: {prefetchCount}\n  global: false");
            channel.BasicQos(
                prefetchSize: 0,
                prefetchCount: prefetchCount,
                global: false
            );
        }

        protected void BasicAck(IModel channel, BasicDeliverEventArgs args)
        {
            try
            {
                _logger.LogDebug($"Ack:ing message with id {args.DeliveryTag}.");
                channel.BasicAck(
                    deliveryTag: args.DeliveryTag,
                    multiple: false
                );
            }
            catch (AlreadyClosedException)
            {
                _logger.LogWarning("Unable to ack message, channel is allready closed.");
                _processedButNotAcked.Add(args.BasicProperties.MessageId);
            }
        }
    }
}
