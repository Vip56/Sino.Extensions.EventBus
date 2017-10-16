using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sino.Extensions.EventBus.Common
{
    /// <summary>
    /// 无Ack实现
    /// </summary>
    public class NoAckAcknowledger : IPublishAcknowledger
    {
        public static Task Completed = Task.FromResult(0ul);

        public Task GetAckTask(IModel result)
        {
            return Completed;
        }
    }
}
