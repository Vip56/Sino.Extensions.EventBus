using Sino.Extensions.EventBus.Channel;
using Sino.Extensions.EventBus.Common;
using Sino.Extensions.EventBus.Configuration;
using Sino.Extensions.EventBus.Consumer;
using Sino.Extensions.EventBus.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RabbitMQ.Client;
using Microsoft.Extensions.Logging;

namespace Sino.Extensions.EventBus.Operations
{
    public class Subscriber : ISubscriber
    {
        private readonly IChannelFactory _channelFactory;
        private readonly IConsumerFactory _consumerFactory;
        private readonly ITopologyProvider _topologyProvider;
        private readonly IMessageSerializer _serializer;
        private readonly RabbitMqConfiguration _config;
        private readonly List<ISubscription> _subscriptions;
        private readonly ILogger _logger;

        public Subscriber(IChannelFactory channelFactory, IConsumerFactory consumerFactory, ITopologyProvider topologyProvider,
            IMessageSerializer serializer, RabbitMqConfiguration config, ILogger<Subscriber> logger)
        {
            _logger = logger;
            _channelFactory = channelFactory;
            _consumerFactory = consumerFactory;
            _topologyProvider = topologyProvider;
            _serializer = serializer;
            _config = config;
            _subscriptions = new List<ISubscription>();
        }

        public ISubscription SubscribeAsync<T>(Func<T, Task> subscribeMethod, SubscriptionConfiguration config)
        {
            var routingKey = config.RoutingKey;

            var topologyTask = _topologyProvider.BindQueueAsync(config.Queue, config.Exchange, routingKey);
            var channelTask = _channelFactory.CreateChannelAsync();

            var subscriberTask = Task
                .WhenAll(topologyTask, channelTask)
                .ContinueWith(t =>
                {
                    if (topologyTask.IsFaulted)
                    {
                        throw topologyTask.Exception ?? new Exception("Topology Task Faulted");
                    }
                    var consumer = _consumerFactory.CreateConsumer(config, channelTask.Result);
                    consumer.OnMessageAsync = async (o, args) =>
                    {
                        var body = _serializer.Deserialize<T>(args.Body);
                        await subscribeMethod(body);
                    };
                    consumer.Model.BasicConsume(config.Queue.FullQueueName, config.NoAck, consumer);
                    _logger.LogDebug($"Setting up a consumer on channel '{channelTask.Result.ChannelNumber}' for queue {config.Queue.QueueName} with NoAck set to {config.NoAck}.");
                    return new Subscription(consumer, config.Queue.FullQueueName);
                });
            Task.WaitAll(subscriberTask);
            _subscriptions.Add(subscriberTask.Result);
            return subscriberTask.Result;
        }

        public async Task ShutdownAsync(TimeSpan? graceful = null)
        {
            _logger.LogDebug("Shutting down Subscriber.");
            foreach (var subscription in _subscriptions.Where(s => s.Active))
            {
                subscription.Dispose();
            }
            graceful = graceful ?? _config.GracefulShutdown;
            await Task.Delay(graceful.Value);
            (_consumerFactory as IDisposable)?.Dispose();
            (_channelFactory as IDisposable)?.Dispose();
            (_topologyProvider as IDisposable)?.Dispose();
        }
    }
}
