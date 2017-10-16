using System.Threading.Tasks;

namespace Sino.Extensions.EventBus.Consumer
{
    /// <summary>
    /// 处理异步通知的消费接口
    /// </summary>
    /// <typeparam name="TNotification">通知类型</typeparam>
    public interface IAsyncNotificationHandler<in TNotification>
        where TNotification : IAsyncNotification
    {
        /// <summary>
        /// 处理异步通知
        /// </summary>
        Task Handle(TNotification notification);
    }
}
