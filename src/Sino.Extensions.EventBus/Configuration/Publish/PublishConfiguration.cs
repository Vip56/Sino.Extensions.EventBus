using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;

namespace Sino.Extensions.EventBus.Configuration
{
    public class PublishConfiguration
    {
        public ExchangeConfiguration Exchange { get; set; }
        public string RoutingKey { get; set; }
        public Action<IBasicProperties> PropertyModifier { get; set; }
        public EventHandler<BasicReturnEventArgs> BasicReturn { get; set; }
    }
}
