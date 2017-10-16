using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using Sino.Extensions.EventBus.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Sino.Extensions.EventBus.Channel
{
    /// <summary>
    /// 频道工厂实现
    /// </summary>
    public class ChannelFactory : IChannelFactory
    {
        private readonly ConcurrentQueue<TaskCompletionSource<IModel>> _requestQueue;
        private readonly ILogger _logger;
        internal readonly ChannelFactoryConfiguration _channelConfig;
        private readonly IConnectionFactory _connectionFactory;
        private readonly RabbitMqConfiguration _config;
        internal readonly LinkedList<IModel> _channels;
        private LinkedListNode<IModel> _current;
        private readonly object _channelLock = new object();
        private readonly object _processLock = new object();
        private Timer _scaleTimer;
        private IConnection _connection;
        private bool _processingRequests;

        public ChannelFactory(IConnectionFactory connectionFactory, RabbitMqConfiguration config, ChannelFactoryConfiguration channelConfig,
            ILogger<ChannelFactory> logger)
        {
            _logger = logger;
            _connectionFactory = connectionFactory;
            _config = config;
            _channelConfig = channelConfig;
            _requestQueue = new ConcurrentQueue<TaskCompletionSource<IModel>>();
            _channels = new LinkedList<IModel>();

            ConnectToBroker();
            Initialize();
        }

        /// <summary>
        /// 连接到代理
        /// </summary>
        protected virtual void ConnectToBroker()
        {
            try
            {
                _connection = _connectionFactory.CreateConnection(_config.Hostnames);
                SetupConnectionRecovery(_connection);
            }
            catch (BrokerUnreachableException e)
            {
                _logger.LogError(1, e, "无法连接到目标");
                throw e.InnerException;
            }
        }

        protected virtual void SetupConnectionRecovery(IConnection connection = null)
        {
            connection = connection ?? _connection;
            var recoverable = connection as IRecoverable;
            if (recoverable == null)
            {
                _logger.LogInformation("连接不带有自动恢复，出现任何未捕获的异常都会导致连接无用。");
                return;
            }
            _logger.LogDebug("初始化带有恢复的连接。");
            recoverable.Recovery += (sender, args) =>
            {
                _logger.LogInformation("连接已经恢复， 通道开始工作。");
                EnsureRequestsAreHandled();
            };
        }

        internal virtual void Initialize()
        {
            _logger.LogDebug($"初始化{_channelConfig.InitialChannelCount}个通道");
            for (var i = 0; i < _channelConfig.InitialChannelCount; i++)
            {
                if (i > _channelConfig.MaxChannelCount)
                {
                    _logger.LogDebug($"尝试创建{i}个通道失败，最大{_channelConfig.MaxChannelCount}");
                    continue;
                }
                CreateAndWireupAsync().Wait();
            }
            _current = _channels.First;

            if (_channelConfig.EnableScaleDown || _channelConfig.EnableScaleUp)
            {
                _logger.LogInformation($"自动扩缩容启用，并且间隔扫描时间为{_channelConfig.ScaleInterval}。");
                _scaleTimer = new Timer(state =>
                {
                    AdjustChannelCount(_channels.Count, _requestQueue.Count);
                }, null, _channelConfig.ScaleInterval, _channelConfig.ScaleInterval);
            }
            else
            {
                _logger.LogInformation("自动扩缩容关闭。");
            }
        }

        internal virtual void AdjustChannelCount(int channelCount, int requestCount)
        {
            if (channelCount == 0)
            {
                _logger.LogWarning("当前可用通道为0，自动扩缩容跳过。");
                return;
            }

            var workPerChannel = requestCount / channelCount;
            var canCreateChannel = channelCount < _channelConfig.MaxChannelCount;
            var canCloseChannel = channelCount > 1;
            _logger.LogDebug($"Begining channel scaling.\n  Channel count: {channelCount}\n  Work per channel: {workPerChannel}");

            if (_channelConfig.EnableScaleUp && canCreateChannel && workPerChannel > _channelConfig.WorkThreshold)
            {
                CreateAndWireupAsync();
                return;
            }
            if (_channelConfig.EnableScaleDown && canCloseChannel && requestCount == 0)
            {
                var toClose = _channels.Last.Value;
                _logger.LogInformation($"Channel '{toClose.ChannelNumber}' will be closed in {_channelConfig.GracefulCloseInterval}.");
                _channels.Remove(toClose);

                Timer graceful = null;
                graceful = new Timer(state =>
                {
                    graceful?.Dispose();
                    toClose.Dispose();
                }, null, _channelConfig.GracefulCloseInterval, new TimeSpan(-1));
            }
        }

        public void Dispose()
        {
            foreach (var channel in _channels)
            {
                channel?.Dispose();
            }
            _connection?.Dispose();
            _scaleTimer?.Dispose();
        }

        public IModel GetChannel()
        {
            return GetChannelAsync().Result;
        }

        public IModel CreateChannel(IConnection connection = null)
        {
            return CreateChannelAsync(connection).Result;
        }

        public Task<IModel> GetChannelAsync()
        {
            var tcs = new TaskCompletionSource<IModel>();
            _requestQueue.Enqueue(tcs);
            if (_connection.IsOpen)
            {
                EnsureRequestsAreHandled();
            }
            else
            {
                var recoverable = _connection as IRecoverable;
                if (recoverable == null)
                {
                    throw new InvalidOperationException("无法恢复通道，连接到代理的连接已经关闭且无法恢复。");
                }
            }
            return tcs.Task;
        }

        private void EnsureRequestsAreHandled()
        {
            if (_processingRequests)
            {
                return;
            }
            if (!_channels.Any() && _channelConfig.InitialChannelCount > 0)
            {
                _logger.LogInformation("当前没有可用的通道。");
                return;
            }
            lock (_processLock)
            {
                if (_processingRequests)
                {
                    return;
                }
                _processingRequests = true;
                _logger.LogDebug("开始处理GetChannel请求。");
            }

            TaskCompletionSource<IModel> channelTcs;
            while (_requestQueue.TryDequeue(out channelTcs))
            {
                lock (_channelLock)
                {
                    if (_current == null && _channelConfig.InitialChannelCount == 0)
                    {
                        CreateAndWireupAsync().Wait();
                        _current = _channels.First;
                    }
                    _current = _current.Next ?? _channels.First;

                    if (_current.Value.IsOpen)
                    {
                        channelTcs.TrySetResult(_current.Value);
                        continue;
                    }

                    _logger.LogInformation($"Channel '{_current.Value.ChannelNumber}' is closed. Removing it from pool.");
                    _channels.Remove(_current);

                    if (_current.Value.CloseReason.Initiator == ShutdownInitiator.Application)
                    {
                        _logger.LogInformation($"Channel '{_current.Value.ChannelNumber}' is closed by application. Disposing channel.");
                        _current.Value.Dispose();
                        if (!_channels.Any())
                        {
                            var newChannelTask = CreateAndWireupAsync();
                            newChannelTask.Wait();
                            _current = _channels.Last;
                            channelTcs.TrySetResult(_current.Value);
                            continue;
                        }
                    }
                }

                var openChannel = _channels.FirstOrDefault(c => c.IsOpen);
                if (openChannel != null)
                {
                    _logger.LogInformation($"Using channel '{openChannel.ChannelNumber}', which is open.");
                    channelTcs.TrySetResult(openChannel);
                    continue;
                }
                var isRecoverable = _channels.Any(c => c is IRecoverable);
                if (!isRecoverable)
                {
                    _processingRequests = false;
                    throw new InvalidOperationException("Unable to retreive channel. All existing channels are closed and none of them are recoverable.");
                }

                _logger.LogInformation("Unable to find an open channel. Requeue TaskCompletionSource for future process and abort execution.");
                _requestQueue.Enqueue(channelTcs);
                _processingRequests = false;
                return;
            }
            _processingRequests = false;
            _logger.LogDebug("'GetChannel' has been processed.");
        }

        public Task<IModel> CreateChannelAsync(IConnection connection = null)
        {
            return connection != null
                ? Task.FromResult(connection.CreateModel())
                : GetConnectionAsync().ContinueWith(tConnection => tConnection.Result.CreateModel());
        }

        internal virtual Task<IModel> CreateAndWireupAsync()
        {
            return GetConnectionAsync()
                .ContinueWith(tConnection =>
                {
                    var channel = tConnection.Result.CreateModel();
                    _logger.LogInformation($"Channel '{channel.ChannelNumber}' has been created.");
                    var recoverable = channel as IRecoverable;
                    if (recoverable != null)
                    {
                        recoverable.Recovery += (sender, args) =>
                        {
                            if (!_channels.Contains(channel))
                            {
                                _logger.LogInformation($"Channel '{_current.Value.ChannelNumber}' is recovered. Adding it to pool.");
                                _channels.AddLast(channel);
                            }
                        };
                    }
                    _channels.AddLast(new LinkedListNode<IModel>(channel));
                    return channel;
                });
        }

        private Task<IConnection> GetConnectionAsync()
        {
            if (_connection == null)
            {
                _logger.LogDebug($"Creating a new connection for {_config.Hostnames.Count} hosts.");
                _connection = _connectionFactory.CreateConnection(_config.Hostnames);
            }
            if (_connection.IsOpen)
            {
                _logger.LogDebug("Existing connection is open and will be used.");
                return Task.FromResult(_connection);
            }
            _logger.LogInformation("The existing connection is not open.");

            if (_connection.CloseReason.Initiator == ShutdownInitiator.Application)
            {
                _logger.LogInformation("Connection is closed with Application as initiator. It will not be recovered.");
                _connection.Dispose();
                throw new Exception("Application shutdown is initiated by the Application. A new connection will not be created.");
            }

            var recoverable = _connection as IRecoverable;
            if (recoverable == null)
            {
                _logger.LogInformation("Connection is not recoverable, trying to create a new connection.");
                _connection.Dispose();
                throw new Exception("The non recoverable connection is closed. A channel can not be obtained.");
            }

            _logger.LogDebug("Connection is recoverable. Waiting for 'Recovery' event to be triggered. ");
            var recoverTcs = new TaskCompletionSource<IConnection>();

            EventHandler<EventArgs> completeTask = null;
            completeTask = (sender, args) =>
            {
                _logger.LogDebug("Connection has been recovered!");
                recoverTcs.TrySetResult(recoverable as IConnection);
                recoverable.Recovery -= completeTask;
            };

            recoverable.Recovery += completeTask;
            return recoverTcs.Task;
        }
    }
}
