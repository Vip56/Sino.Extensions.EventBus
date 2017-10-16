using System;

namespace Sino.Extensions.EventBus.Configuration
{
    /// <summary>
    /// 自动扩缩容配置，默认不开启
    /// </summary>
    public class ChannelFactoryConfiguration
    {
        /// <summary>
        /// 是否允许自动扩容
        /// </summary>
        public bool EnableScaleUp { get; set; }

        /// <summary>
        /// 是否允许自动缩容
        /// </summary>
        public bool EnableScaleDown { get; set; }

        /// <summary>
        /// 自动扩缩容的扫描间隔时间
        /// </summary>
        public TimeSpan ScaleInterval { get; set; }

        /// <summary>
        /// 平滑关闭通道的间隔时间
        /// </summary>
        public TimeSpan GracefulCloseInterval { get; set; }

        /// <summary>
        /// 最大的可创建的通道个数
        /// </summary>
        public int MaxChannelCount { get; set; }

        /// <summary>
        /// 初始创建的通道个数
        /// </summary>
        public int InitialChannelCount { get; set; }

        /// <summary>
        /// 指定每个通道的消息达到该贬值才会扩容
        /// </summary>
        public int WorkThreshold { get; set; }

        public static ChannelFactoryConfiguration Default => new ChannelFactoryConfiguration
        {
            InitialChannelCount = 0,
            MaxChannelCount = 1,
            GracefulCloseInterval = TimeSpan.FromMinutes(30),
            WorkThreshold = 20000,
            ScaleInterval = TimeSpan.FromSeconds(10),
            EnableScaleUp = false,
            EnableScaleDown = false
        };
    }
}
