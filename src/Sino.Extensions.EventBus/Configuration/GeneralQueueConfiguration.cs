namespace Sino.Extensions.EventBus.Configuration
{
    public class GeneralQueueConfiguration
    {
        /// <summary>
        /// 队列中不存在任何消费者时候是否自动删除
        /// </summary>
        public bool AutoDelete { get; set; }

        /// <summary>
        /// 持久化
        /// </summary>
        public bool Durable { get; set; }

        /// <summary>
        /// 专用队列
        /// </summary>
        public bool Exclusive { get; set; }
    }
}
