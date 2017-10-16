using System;

namespace Sino.Extensions.EventBus.Common
{
    /// <summary>
    /// 提供默认情况下Event的Exchange、RoutingKey和QueueName参数
    /// </summary>
    public interface INamingConventions
    {
        Func<Type, string> ExchangeNamingConvention { get; set; }
        Func<Type, string> QueueNamingConvention { get; set; }
        Func<Type, string> SubscriberQueueSuffix { get; set; }
    }
}
