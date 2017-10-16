using System;

namespace Sino.Extensions.EventBus.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class RoutingAttribute : Attribute
    {
        internal bool? NullableNoAck;

        public RoutingAttribute(string routingKey)
        {
            RoutingKey = routingKey;
        }

        public string RoutingKey { get; set; }
        public ushort PrefetchCount { get; set; }
        public bool NoAck { get { return NullableNoAck.GetValueOrDefault(); } set { NullableNoAck = value; } }
    }
}
