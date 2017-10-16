using Sino.Extensions.EventBus.Configuration;
using System.Threading.Tasks;

namespace Sino.Extensions.EventBus.Operations
{
    /// <summary>
    /// Event发送提供器
    /// </summary>
    public interface IPublisher
    {
        Task PublishAsync<TMessage>(TMessage message, PublishConfiguration config);
    }
}
