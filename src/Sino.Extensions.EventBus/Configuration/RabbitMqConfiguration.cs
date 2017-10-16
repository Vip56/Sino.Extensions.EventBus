using RabbitMQ.Client;
using System;
using System.Collections.Generic;

namespace Sino.Extensions.EventBus.Configuration
{
    public class RabbitMqConfiguration
    {
        /// <summary>
        /// 等待发布被确认的超时时间，默认为1分钟
        /// 更多请参考： https://www.rabbitmq.com/confirms.html
        /// </summary>
        public TimeSpan PublishConfirmTimeout { get; set; }

        /// <summary>
        /// 在服务关闭前等待消息处理程序处理消息的超时时间
        /// </summary>
        public TimeSpan GracefulShutdown { get; set; }

        /// <summary>
        /// 是否启用自动恢复 (重连, 通道重开, 修复QoS)
        /// 默认启用
        /// </summary>
        public bool AutomaticRecovery { get; set; }

        /// <summary>
        /// 是否启用topology恢复 (重新声明交换器和队列, 修复绑定和消费者)
        /// 默认启用
        /// </summary>
        public bool TopologyRecovery { get; set; }

        /// <summary>
        /// 交换器配置
        /// </summary>
        public GeneralExchangeConfiguration Exchange { get; set; }

        /// <summary>
        /// 队列配置
        /// </summary>
        public GeneralQueueConfiguration Queue { get; set; }

        /// <summary>
        /// 持久化属性（消息是基于内存还是硬盘存储）
        /// 如果对性能的需求高于消息的稳定传递则可设置为False
        /// </summary>
        public bool PersistentDeliveryMode { get; set; }

        /// <summary>
        /// 是否在所有通道关闭后自动关闭连接
        /// </summary>
        public bool AutoCloseConnection { get; set; }

        /// <summary>
        /// SSL配置
        /// </summary>
        public SslOption Ssl { get; set; }

        /// <summary>
        /// 虚拟主机路径
        /// </summary>
        public string VirtualHost { get; set; }

        /// <summary>
        /// 用户名
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// 密码
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// 端口
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// 主机地址
        /// </summary>
        public List<string> Hostnames { get; set; }

        /// <summary>
        /// 自动重试间隔
        /// 默认10s
        /// </summary>
        public TimeSpan RecoveryInterval { get; set; }

        public RabbitMqConfiguration()
        {
			PublishConfirmTimeout = TimeSpan.FromSeconds(1);
			PersistentDeliveryMode = true;
			AutoCloseConnection = true;
			AutomaticRecovery = true;
			TopologyRecovery = true;
			RecoveryInterval = TimeSpan.FromSeconds(10);
			GracefulShutdown = TimeSpan.FromSeconds(10);
            Ssl = new SslOption { Enabled = false };
			Hostnames = new List<string>();
			Exchange = new GeneralExchangeConfiguration
			{
				AutoDelete = false,
				Durable = true,
				Type = ExchangeType.Topic
			};
			Queue = new GeneralQueueConfiguration
			{
				Exclusive = false,
				AutoDelete = false,
				Durable = true
			};
		    VirtualHost = "/";
		    Username = "guest";
		    Password = "guest";
		    Port = 5672;
        }
    }
}
