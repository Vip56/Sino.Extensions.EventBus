using RabbitMQ.Client;
using System;

namespace Sino.Extensions.EventBus.Common
{
    /// <summary>
    /// 提供Event发送所携带的附加信息接口
    /// </summary>
    public interface IBasicPropertiesProvider
    {
        IBasicProperties GetProperties<TMessage>(Action<IBasicProperties> custom = null);
    }
}
