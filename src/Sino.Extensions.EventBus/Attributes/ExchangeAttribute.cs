using Sino.Extensions.EventBus.Configuration;
using System;

namespace Sino.Extensions.EventBus.Attributes
{
    /// <summary>
    /// 交换器注解属性
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ExchangeAttribute : Attribute
    {
        internal bool? NullableDurability;
        internal bool? NullableAutoDelete;

        public ExchangeAttribute(string name)
        {
            Name = name;
        }

        /// <summary>
        /// 交换器名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 交换器类型
        /// </summary>
        public ExchangeType Type { get; set; }

        /// <summary>
        /// 交换器是否持久化，需要Queue也为持久化同时消息发送时DeliveryMode为2才可用（该特性会极大的降低RabbitMq性能）
        /// </summary>
        public bool Durable { set { NullableDurability = value; } }

        /// <summary>
        /// 是否在所有队列结束时自动删除交换器
        /// </summary>
        public bool AutoDelete { set { NullableAutoDelete = value; } }
    }
}
