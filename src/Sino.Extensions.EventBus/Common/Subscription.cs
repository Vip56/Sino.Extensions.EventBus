using RabbitMQ.Client;
using Sino.Extensions.EventBus.Consumer;

namespace Sino.Extensions.EventBus.Common
{
    public class Subscription : ISubscription
    {
        public string QueueName { get; }
        public string ConsumerTag { get; }
        public bool Active { get; set; }

        private readonly IRawConsumer _consumer;

        public Subscription(IRawConsumer consumer, string queueName)
        {
            _consumer = consumer;
            var basicConsumer = consumer as DefaultBasicConsumer;
            if (basicConsumer == null)
            {
                return;
            }
            QueueName = queueName;
            ConsumerTag = basicConsumer.ConsumerTag;
        }

        public void Dispose()
        {
            Active = false;
            _consumer.Disconnect();
        }
    }
}
