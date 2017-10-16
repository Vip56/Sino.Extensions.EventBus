using Sino.Extensions.EventBus.Common;
using Sino.Extensions.EventBus.Operations;
using System;
using System.Threading.Tasks;
using Sino.Extensions.EventBus.Configuration;
using Microsoft.Extensions.Logging;

namespace Sino.Extensions.EventBus
{
    public class EventBus : IEventBus
    {
        private readonly IConfigurationEvaluator _configEval;
        private readonly IPublisher _publisher;
        private readonly ISubscriber _subscriber;
        private readonly ILogger _logger;

        public EventBus(IConfigurationEvaluator configEval, ISubscriber subscriber, IPublisher publisher, ILogger<EventBus> logger)
        {
            _logger = logger;
            _configEval = configEval;
            _publisher = publisher;
            _subscriber = subscriber;
            _logger.LogInformation("事件总线初始化完毕");
        }

        public Task PublishAsync<T>(T message = default(T), Action<IPublishConfigurationBuilder> configuration = null)
        {
            var config = _configEval.GetConfiguration<T>(configuration);
            _logger.LogDebug($"发送事件，事件名：'{typeof(T).Name}'，交换器名：'{config.Exchange.ExchangeName}'，路由值：'{config.RoutingKey}'");
            return _publisher.PublishAsync(message, config);
        }

        public ISubscription SubscribeAsync<T>(Func<T, Task> subscribeMethod, Action<ISubscriptionConfigurationBuilder> configuration = null)
        {
            var config = _configEval.GetConfiguration<T>(configuration);
            _logger.LogDebug($"订阅事件，事件名：'{typeof(T).Name}'，交换器名：'{config.Exchange.ExchangeName}'，路由值：'{config.RoutingKey}'");
            return _subscriber.SubscribeAsync(subscribeMethod, config);
        }
    }
}
