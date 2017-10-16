using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace Sino.Extensions.EventBus.Common
{
    /// <summary>
    /// 有Ack实现
    /// </summary>
    public class PublishAcknowledger : IPublishAcknowledger
    {
        private readonly TimeSpan _publishTimeout;
        private readonly ILogger _logger;
        private readonly ConcurrentDictionary<string, TaskCompletionSource<ulong>> _deliveredAckDictionary;
        private readonly ConcurrentDictionary<string, Timer> _ackTimers;
        private ulong _currentDeliveryTags;

        public PublishAcknowledger(TimeSpan publishTimeout, ILogger<PublishAcknowledger> logger)
        {
            _logger = logger;
            _publishTimeout = publishTimeout;
            _deliveredAckDictionary = new ConcurrentDictionary<string, TaskCompletionSource<ulong>>();
            _ackTimers = new ConcurrentDictionary<string, Timer>();
        }

        public Task GetAckTask(IModel channel)
        {
            if (channel.NextPublishSeqNo == 0UL)
            {
                _currentDeliveryTags = 0;
                _logger.LogInformation($"Setting 'Publish Acknowledge' for channel '{channel.ChannelNumber}'");
                channel.ConfirmSelect();
                channel.BasicAcks += (sender, args) =>
                {
                    var model = sender as IModel;
                    _logger.LogInformation($"Recieved ack for {args.DeliveryTag}/{model.ChannelNumber} with multiple set to '{args.Multiple}'");
                    if (args.Multiple)
                    {
                        for (var i = args.DeliveryTag; i > _currentDeliveryTags; i--)
                        {
                            CompleteConfirm(model, i, true);
                        }
                        _currentDeliveryTags = args.DeliveryTag;
                    }
                    else
                    {
                        _currentDeliveryTags = args.DeliveryTag > _currentDeliveryTags ? args.DeliveryTag : _currentDeliveryTags;
                        CompleteConfirm(model, args.DeliveryTag);
                    }
                };
            }

            var key = CreatePublishKey(channel, channel.NextPublishSeqNo);
            var tcs = new TaskCompletionSource<ulong>();
            if (!_deliveredAckDictionary.TryAdd(key, tcs))
            {
                _logger.LogWarning($"Unable to add delivery tag {key} to ack list.");
            }
            _ackTimers.TryAdd(key, new Timer(state =>
            {
                _logger.LogWarning($"Ack for {key} has timed out.");
                TryDisposeTimer(key);

                TaskCompletionSource<ulong> ackTcs;
                if (!_deliveredAckDictionary.TryGetValue(key, out ackTcs))
                {
                    _logger.LogWarning($"Unable to get TaskCompletionSource for {key}");
                    return;
                }
                ackTcs.TrySetException(new InvalidOperationException($"The broker did not send a publish acknowledgement for message {key} within {_publishTimeout.ToString("g")}."));
            }, channel, _publishTimeout, new TimeSpan(-1)));
            return tcs.Task;
        }

        private void CompleteConfirm(IModel channel, ulong tag, bool multiple = false)
        {
            var key = CreatePublishKey(channel, tag);
            TryDisposeTimer(key);
            TaskCompletionSource<ulong> tcs;
            if (!_deliveredAckDictionary.TryRemove(key, out tcs))
            {
                if (!multiple)
                {
                    _logger.LogWarning($"Unable to remove task completion source for Publish Confirm on '{key}'.");
                }
            }
            else
            {
                if (tcs.TrySetResult(tag))
                {
                    _logger.LogDebug($"Successfully confirmed publish {key}");
                }
                else
                {
                    if (tcs.Task.IsFaulted)
                    {
                        _logger.LogDebug($"Unable to set result for '{key}'. Task has been faulted.");
                    }
                    else if (!multiple)
                    {
                        _logger.LogWarning($"Unable to set result for Publish Confirm on key '{key}'.");
                    }
                }
            }
        }

        private static string CreatePublishKey(IModel channel, ulong nextTag)
        {
            return $"{nextTag}/{channel.ChannelNumber}";
        }

        private void TryDisposeTimer(string key)
        {
            Timer ackTimer;
            if (_ackTimers.TryRemove(key, out ackTimer))
            {
                _logger.LogDebug($"Disposed ack timer for {key}");
                ackTimer.Dispose();
            }
        }
    }
}
