using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sino.Extensions.EventBus.Configuration
{
    public interface IQueueConfigurationBuilder
    {
        IQueueConfigurationBuilder WithName(string queueName);
        IQueueConfigurationBuilder WithAutoDelete(bool autoDelete = true);
        IQueueConfigurationBuilder WithDurability(bool durable = true);
        IQueueConfigurationBuilder WithExclusivity(bool exclusive = true);
        IQueueConfigurationBuilder WithArgument(string key, object value);
        IQueueConfigurationBuilder AssumeInitialized(bool asumption = true);
    }
}
