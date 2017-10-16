using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sino.Extensions.EventBus.Consumer
{
    public interface IRawConsumer : IBasicConsumer
    {
        Func<object, BasicDeliverEventArgs, Task> OnMessageAsync { get; set; }
        List<ulong> AcknowledgedTags { get; }
        void Disconnect();
    }
}
