using Sino.Extensions.EventBus.Configuration;
using System;
using System.Threading.Tasks;

namespace Sino.Extensions.EventBus.Common
{
    public interface IEventBus
    {
        Task PublishAsync<T>(T message = default(T), Action<IPublishConfigurationBuilder> configuration = null);
        ISubscription SubscribeAsync<T>(Func<T, Task> subscribeMethod, Action<ISubscriptionConfigurationBuilder> configuration = null);
    }
}
