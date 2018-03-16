using Microsoft.Extensions.DependencyInjection;
using Sino.Extensions.EventBus.Configuration;
using Sino.Extensions.EventBus.Consumer;
using System;
using System.Threading.Tasks;
using Xunit;

namespace EventBusUnitTest
{
    public class SendEventTest
    {
        protected IServiceCollection Services { get; set; }

        public SendEventTest()
        {
            Services = new ServiceCollection();
        }

        [Fact]
        public void Test1()
        {
            Services.AddEventBus(new RabbitMqConfiguration(), typeof(SendEventTest));
        }
    }

    public abstract class BaseEvent : IAsyncNotification
    {
        public BaseEvent()
        {
            Time = DateTime.Now;
        }

        public DateTime Time { get; set; }
    }

    public class BaseEventHandler : IAsyncNotificationHandler<BaseEvent>
    {
        public Task Handle(BaseEvent notification)
        {
            return Task.CompletedTask;
        }
    }
}
