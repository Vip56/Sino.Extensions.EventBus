using Sino.Extensions.EventBus.Configuration;
using System.Collections.Generic;

namespace Sino.Extensions.EventBus.Common
{
    /// <summary>
    /// 为连接到RabbitMQ提供自定义客户端参数接口
    /// </summary>
    public interface IClientPropertyProvider
    {
        IDictionary<string, object> GetClientProperties(RabbitMqConfiguration cfg = null);
    }
}
