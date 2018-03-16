using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using Scrutor;
using Sino.Extensions.EventBus;
using Sino.Extensions.EventBus.Channel;
using Sino.Extensions.EventBus.Common;
using Sino.Extensions.EventBus.Configuration;
using Sino.Extensions.EventBus.Consumer;
using Sino.Extensions.EventBus.Operations;
using Sino.Extensions.EventBus.Serialization;
using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Builder;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class EventBusServiceCollectionExtensions
    {
        /// <summary>
        /// 添加EventBus功能，通过IEventBus接口使用
        /// </summary>
        /// <param name="section">配置文件</param>
        /// <param name="types">自动识别类</param>
        public static IServiceCollection AddEventBus(this IServiceCollection services, IConfigurationSection section, params Type[] types)
        {
            var mainCfg = new RabbitMqConfiguration();
            section.Bind(mainCfg);
            return AddEventBus(services, mainCfg, types);
        }

        public static IServiceCollection AddEventBus(this IServiceCollection services, RabbitMqConfiguration config, params Type[] types)
        {
            services.AddSingleton(config);

            //自动扫描Handler并注册
            services.Scan(scan => scan.FromAssembliesOf(types)
                .AddClasses(classes => classes.AssignableTo(typeof(IAsyncNotificationHandler<>)))
                .AsImplementedInterfaces()
                .WithTransientLifetime());

            services
                .AddSingleton<IConnectionFactory, ConnectionFactory>(provider =>
                {
                    var cfg = provider.GetService<RabbitMqConfiguration>();
                    return new ConnectionFactory
                    {
                        VirtualHost = cfg.VirtualHost,
                        UserName = cfg.Username,
                        Password = cfg.Password,
                        Port = cfg.Port,
                        HostName = cfg.Hostnames.FirstOrDefault() ?? string.Empty,
                        AutomaticRecoveryEnabled = cfg.AutomaticRecovery,
                        TopologyRecoveryEnabled = cfg.TopologyRecovery,
                        NetworkRecoveryInterval = cfg.RecoveryInterval,
                        ClientProperties = provider.GetService<IClientPropertyProvider>().GetClientProperties(cfg),
                        Ssl = cfg.Ssl
                    };
                })
                .AddSingleton<IClientPropertyProvider, ClientPropertyProvider>()
                .AddSingleton<IMessageSerializer, JsonMessageSerializer>()
                .AddSingleton<IBasicPropertiesProvider, BasicPropertiesProvider>()
                .AddSingleton<IChannelFactory, ChannelFactory>()
                .AddSingleton(c => ChannelFactoryConfiguration.Default)
                .AddSingleton<ITopologyProvider, TopologyProvider>()
                .AddTransient<IConfigurationEvaluator, ConfigurationEvaluator>()
                .AddTransient<IConsumerFactory, EventingBasicConsumerFactory>()
                .AddSingleton<ISubscriber, Subscriber>()
                .AddTransient<IPublishAcknowledger, PublishAcknowledger>(
                    p => new PublishAcknowledger(p.GetService<RabbitMqConfiguration>().PublishConfirmTimeout,p.GetService<ILogger<PublishAcknowledger>>())
                )
                .AddSingleton<INamingConventions, NamingConventions>()
                .AddTransient<IPublisher, Publisher>()
                .AddSingleton<IEventBus>(provider =>
                {
                    return ActivatorUtilities.CreateInstance<EventBus>(provider);
                });

            return services;
        }

        public static IApplicationBuilder AddHandler<TEvent>(this IApplicationBuilder app, Action<ISubscriptionConfigurationBuilder> configuration = null)
            where TEvent : IAsyncNotification
        {
            var eventBus = app.ApplicationServices.GetService<IEventBus>();

            eventBus.SubscribeAsync<TEvent>(async x =>
            {
                var handlers = app.ApplicationServices.GetServices<IAsyncNotificationHandler<TEvent>>();
                foreach (var handler in handlers)
                {
                    await handler.Handle(x);
                }
            }, configuration);

            return app;
        }
    }
}
