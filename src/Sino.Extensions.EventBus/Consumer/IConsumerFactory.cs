using RabbitMQ.Client;
using Sino.Extensions.EventBus.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sino.Extensions.EventBus.Consumer
{
    public interface IConsumerFactory
    {
        IRawConsumer CreateConsumer(IConsumerConfiguration cfg, IModel channel);
    }
}
