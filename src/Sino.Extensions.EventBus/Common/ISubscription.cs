using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sino.Extensions.EventBus.Common
{
    public interface ISubscription : IDisposable
    {
        string QueueName { get; }
        string ConsumerTag { get; }
        bool Active { get; }
    }
}
