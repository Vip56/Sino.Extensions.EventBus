using System;

namespace Sino.Extensions.EventBus.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class QueueAttribute : Attribute
    {
        internal bool? NullableDurability;
        internal bool? NullableExclusitivy;
        internal bool? NullableAutoDelete;

        public QueueAttribute(string name)
        {
            Name = name;
        }

        /// <summary>
        /// 队列名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 持久化
        /// </summary>
        public bool Durable
        {
            get { return NullableDurability.GetValueOrDefault(); }
            set { NullableDurability = value; }
        }

        /// <summary>
        /// 专用队列
        /// </summary>
        public bool Exclusive
        {
            get { return NullableExclusitivy.GetValueOrDefault(); }
            set { NullableExclusitivy = value; }
        }

        /// <summary>
        /// 队列中不存在任何消费者时候是否自动删除
        /// </summary>
        public bool AutoDelete
        {
            get { return NullableAutoDelete.GetValueOrDefault(); }
            set { NullableAutoDelete = value; }
        }
        public int MessageTtl { get; set; }
        public byte MaxPriority { get; set; }
        public string DeadLeterExchange { get; set; }
        public string Mode { get; set; }
    }
}
