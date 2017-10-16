using Sino.Extensions.EventBus.Configuration;
using System;

namespace Sino.Extensions.EventBus.Common
{
    /// <summary>
    /// 提供Event配置信息
    /// </summary>
    public interface IConfigurationEvaluator
    {
        /// <summary>
        /// 根据类型获取订阅配置信息
        /// </summary>
        /// <typeparam name="TMessage">事件类型</typeparam>
        /// <param name="configuration">自定义配置信息</param>
        SubscriptionConfiguration GetConfiguration<TMessage>(Action<ISubscriptionConfigurationBuilder> configuration = null);

        /// <summary>
        /// 根据类型获取发送配置信息
        /// </summary>
        /// <typeparam name="TMessage">事件类型</typeparam>
        /// <param name="configuration">自定义配置信息</param>
        PublishConfiguration GetConfiguration<TMessage>(Action<IPublishConfigurationBuilder> configuration);

        /// <summary>
        /// 根据类型获取订阅配置信息
        /// </summary>
        /// <param name="messageType">类型信息</param>
        /// <param name="configuration">自定义配置信息</param>
        SubscriptionConfiguration GetConfiguration(Type messageType, Action<ISubscriptionConfigurationBuilder> configuration = null);

        /// <summary>
        /// 根据类型获取发送配置信息
        /// </summary>
        /// <param name="messageType">类型信息</param>
        /// <param name="configuration">自定义配置信息</param>
        PublishConfiguration GetConfiguration(Type messageType, Action<IPublishConfigurationBuilder> configuration);
    }
}
