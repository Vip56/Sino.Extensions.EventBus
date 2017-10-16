using Sino.Extensions.EventBus.Common;
using Sino.Extensions.EventBus.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sino.Extensions.EventBus.Operations
{
    public interface ISubscriber
    {
        ISubscription SubscribeAsync<T>(Func<T, Task> subscribeMethod, SubscriptionConfiguration config);
    }
}
