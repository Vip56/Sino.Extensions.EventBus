using RabbitMQ.Client;
using System;
using System.Threading.Tasks;

namespace Sino.Extensions.EventBus.Channel
{
    /// <summary>
    /// 频道工厂接口
    /// </summary>
    public interface IChannelFactory : IDisposable
    {
        /// <summary>
        /// 创建频道
        /// </summary>
        IModel CreateChannel(IConnection connection = null);

        /// <summary>
        /// 获取当前有效的频道
        /// </summary>
        Task<IModel> GetChannelAsync();

        /// <summary>
        /// 创建频道
        /// </summary>
        Task<IModel> CreateChannelAsync(IConnection connection = null);
    }
}
