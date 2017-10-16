namespace Sino.Extensions.EventBus.Configuration
{
    /// <summary>
    /// 交换器配置
    /// </summary>
    public class GeneralExchangeConfiguration
    {
        /// <summary>
        /// 交换器是否持久化，需要Queue也为持久化同时消息发送时DeliveryMode为2才可用（该特性会极大的降低RabbitMq性能）
        /// </summary>
        public bool Durable { get; set; }

        /// <summary>
        /// 是否在所有队列结束时自动删除交换器
        /// </summary>
        public bool AutoDelete { get; set; }

        /// <summary>
        /// 交换器类型
        /// </summary>
        public ExchangeType Type { get; set; }
    }
}
