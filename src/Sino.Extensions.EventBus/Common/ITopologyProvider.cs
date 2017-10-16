using Sino.Extensions.EventBus.Configuration;
using System.Threading.Tasks;

namespace Sino.Extensions.EventBus.Common
{
    /// <summary>
    /// 提供Queue与Exchange进行绑定
    /// </summary>
    public interface ITopologyProvider
    {
        /// <summary>
        /// 声明交换器
        /// </summary>
        Task DeclareExchangeAsync(ExchangeConfiguration exchange);

        /// <summary>
        /// 声明队列
        /// </summary>
        Task DeclareQueueAsync(QueueConfiguration queue);

        /// <summary>
        /// 绑定队列
        /// </summary>
        Task BindQueueAsync(QueueConfiguration queue, ExchangeConfiguration exchange, string routingKey);

        /// <summary>
        /// 解绑队列
        /// </summary>
        Task UnbindQueueAsync(QueueConfiguration queue, ExchangeConfiguration exchange, string routingKey);

        /// <summary>
        /// 判断交换器是否已声明
        /// </summary>
        bool IsInitialized(ExchangeConfiguration exchange);

        /// <summary>
        /// 判断队列是否已声明
        /// </summary>
        bool IsInitialized(QueueConfiguration exchange);
    }
}
