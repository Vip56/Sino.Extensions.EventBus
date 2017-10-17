using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;

namespace Sino.Extensions.EventBus.Configuration
{
    public interface IPublishConfigurationBuilder
    {
        IPublishConfigurationBuilder WithExchange(Action<IExchangeConfigurationBuilder> exchange);
        IPublishConfigurationBuilder WithRoutingKey(string routingKey);
        IPublishConfigurationBuilder WithProperties(Action<IBasicProperties> properties);
        IPublishConfigurationBuilder WithMandatoryDelivery(EventHandler<BasicReturnEventArgs> basicReturn);
    }
}
