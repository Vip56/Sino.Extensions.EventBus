namespace Sino.Extensions.EventBus.Configuration
{
    public class ConsumerConfiguration : IConsumerConfiguration
    {
        public ExchangeConfiguration Exchange { get; set; }

        public bool NoAck { get; set; }

        public ushort PrefetchCount { get; set; }

        public QueueConfiguration Queue { get; set; }

        public string RoutingKey { get; set; }

        public ConsumerConfiguration()
        {
            Exchange = new ExchangeConfiguration();
            Queue = new QueueConfiguration();
            NoAck = true;
        }
    }
}
