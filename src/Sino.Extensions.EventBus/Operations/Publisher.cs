using Sino.Extensions.EventBus.Channel;
using Sino.Extensions.EventBus.Common;
using Sino.Extensions.EventBus.Configuration;
using Sino.Extensions.EventBus.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Sino.Extensions.EventBus.Operations
{
    /// <summary>
    /// Event发送提供器
    /// </summary>
    public class Publisher : IPublisher
    {
        private readonly IChannelFactory _channelFactory;
        private readonly ITopologyProvider _topologyProvider;
        private readonly IMessageSerializer _serializer;
        private readonly IPublishAcknowledger _acknowledger;
        private readonly IBasicPropertiesProvider _propertiesProvider;
        private readonly RabbitMqConfiguration _config;
        private readonly ILogger _logger;
        private readonly object _publishLock = new object();
        private readonly object _topologyLock = new object();

        public Publisher(IChannelFactory channelFactory, ITopologyProvider topologyProvider, IMessageSerializer serializer, IPublishAcknowledger acknowledger, 
            IBasicPropertiesProvider propertiesProvider, RabbitMqConfiguration config, ILogger<Publisher> logger)
        {
            _logger = logger;
            _channelFactory = channelFactory;
            _topologyProvider = topologyProvider;
            _serializer = serializer;
            _acknowledger = acknowledger;
            _propertiesProvider = propertiesProvider;
            _config = config;
        }

        public Task PublishAsync<TMessage>(TMessage message, PublishConfiguration config)
        {
            var props = _propertiesProvider.GetProperties<TMessage>(config.PropertyModifier);

            Task exchangeTask;
            lock (_topologyLock)
            {
                exchangeTask = _topologyProvider.DeclareExchangeAsync(config.Exchange);
            }
            var channelTask = _channelFactory.GetChannelAsync();

            return Task
                .WhenAll(exchangeTask, channelTask)
                .ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        throw t.Exception;
                    }

                    channelTask.Result.BasicReturn += config.BasicReturn;

                    lock (_publishLock)
                    {
                        var ackTask = _acknowledger.GetAckTask(channelTask.Result);
                        channelTask.Result.BasicPublish(
                            exchange: config.Exchange.ExchangeName,
                            routingKey: config.RoutingKey,
                            basicProperties: props,
                            body: _serializer.Serialize(message),
                            mandatory: (config.BasicReturn != null)
                        );
                        return ackTask
                        .ContinueWith(a => {
                            channelTask.Result.BasicReturn -= config.BasicReturn;
                        });
                    }
                })
                .Unwrap();
        }
    }
}
