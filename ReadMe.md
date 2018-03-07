事件总线
----
由于公司的项目均采用[.Net Core](http://dot.net)运行于基于Ubuntu 16.04的Docker之上，所以之前的.Net Framework下的方案均不可用，而因为[Masstransit](http://masstransit-project.com/)并未提供基于[.Net Core](http://dot.net)
的SDK，所以在结合诸多开源项目以及我们自己的实际需求之后通过改写从而诞生了该库。  

[![Build status](https://ci.appveyor.com/api/projects/status/x5j1d91cqlqtg2lt/branch/master?svg=true)](https://ci.appveyor.com/project/vip56/sino-extensions-eventbus/branch/master)
[![NuGet](https://img.shields.io/nuget/v/Nuget.Core.svg?style=plastic)](https://www.nuget.org/packages/Sino.Extensions.EventBus)   

# 快速使用  
### 1. 配置
打开`appsettings.json`并增加如下配置信息：
```
  "EventBus": {
    "Username": "guest",
    "Password": "guest",
    "VirtualHost": "/",
    "Port": 5672,
    "Hostnames": [ "localhost" ],
    "PublishConfirmTimeout": "00:00:01",
    "RecoveryInterval": "00:00:10",
    "PersistentDeliveryMode": true,
    "AutoCloseConnection": true,
    "AutomaticRecovery": true,
    "TopologyRecovery": true,
    "Exchange": {
      "Durable": true,
      "AutoDelete": true,
      "Type": "Topic"
    },
    "Queue": {
      "AutoDelete": true,
      "Durable": true,
      "Exclusive": true
    }
  }
```
以上配置说明如下所示：  
- `Username`：用户名  
- `Password`：密码  
- `VirtualHost`：虚拟路径  
- `Port`：端口  
- `Hostnames`:主机地址列表  
- `PublishConfirmTimeout`：等待发布被确认的超时时间  
- `RecoveryInterval`：自动重试间隔  
- `PersistentDeliveryMode`：持久化属性（消息是基于内存还是硬盘存储）如果对性能的需求高于消息的稳定传递则可设置为False  
- `AutoCloseConnection`：是否在所有通道关闭后自动关闭连接  
- `AutomaticRecovery`：是否启用自动恢复 (重连, 通道重开, 修复QoS)  
- `TopologyRecovery`：是否启用topology恢复 (重新声明交换器和队列, 修复绑定和消费者)  
- `Exchange Durable`：交换器是否持久化，需要Queue也为持久化同时消息发送时DeliveryMode为2才可用（该特性将会降低RabbitMQ性能）  
- `Exchange AutoDelete`：是否在所有队列结束时自动删除交换器  
- `Exchange Type`：交换器类型  
- `Queue AutoDelete`：队列中不存在任何消费者时候是否自动删除  
- `Queue Durable`：持久化  
- `Queue Exclusive`：专用队列  

### 2. 事件定义  
事件建议单独一个类库进行定义，这样有利于将其打包成nuget方便后期其他项目直接引用，同时在该类库项目中应该定义一个公共的基类事件，如`BaseEvent`，其中可以定义一部分通用的时间属性比如发送时间，因为所有的事件必须继承自`IAsyncNotification`接口，所以这样也方便管理，比如下面这个事件基类：
```
    public abstract class BaseEvent : IAsyncNotification
    {
        public BaseEvent()
        {
            Time = DateTime.Now;
        }

        public DateTime Time { get; set; }
    }
```

其他的业务事件建议采用`动词+名词+Event`的方式进行命名，这样可以规范整体系统的事件，方便看出其事件的用意。    

### 3. 客服端（发送方）
因为接收和发送都集中在这个框架中，所以对应的配置有一定的差距，下面就以客户端（发送方）为例来演示如何初始化，这里的初始化都以ASP.NET Core为准，首先打开`Startup`文件，在`ConfigureServices`中增加以下内容：
```
services.AddEventBus(Configuration.GetSection("EventBus"));
```

在我们的代码中直接可以通过引入`IEventBus`接口即可使用，比如下面的代码就在`Controller`中发送了一个事件：
```
        public OrderController(IEventBus eventBus)
        {
            _eventBus = eventBus;
        }

        [HttpGet("Add")]
        public IActionResult Add()
        {
            _eventBus.PublishAsync(new AddOrderEvent()
            {
                Id = 1,
                Title = "测试",
                Count = 1,
                UserId = 2
            });
            return Ok();
        }
```  

### 4. 服务端（接收方）
接收方因为存在自动IOC的部分以及手动指定的部分，对应的配置也多一些，同样是打开`Startup`文件，在`ConfigureServices`中增加以下代码：
```
services.AddEventBus(Configuration.GetSection("EventBus"), typeof(Startup));
```
其中第二个多出来的参数是指定Event处理程序所在程序集中的任意一个对象的类型即可，因为内部需要扫描这个程序集。完成上面的初始化后我们所有的处理程序都会注册到IOC中，但是这个时候这些处理程序并没有可用，因为考虑到一定的性能问题，所以最终必须通过另一个配置才能启动对某一个Event的监听处理，比如我们需要处理`AddOrderEvent`那么就需要在`Configure`中增加如下代码：
```
app.AddHandler<AddOrderEvent>();
```  

### 5. 事件处理程序
所有的事件处理程序，如果事件没有采用注解属性的方式规定Exchange、Queue和Routing Key的必须引用Event定义的程序集，否则会出现无法收到的问题，之后我们可以通过实现`IAsyncNotificationHandler<>`泛型接口来处理指定的Event，当然由于内部使用了自带的IOC所以实现该接口的来，可以使用正常项目的其他已经配置到IOC中的服务，比如下面这个代码就是处理`AddOrderEvent`的处理程序：
```
    public class AddOrderEventHandler : IAsyncNotificationHandler<AddOrderEvent>
    {
        public Task Handle(AddOrderEvent notification)
        {
            return Task.CompletedTask;
        }
    }
```  

### 注解属性
提供了有以下三种注解属性`ExchangeAttribute`、`QueueAttribute`和`RoutingAttribute`，这些注解属性可以定义在对应的`Event`类上即可，其中可以设置大部分的配置参数。

### 自动命名规则
- 监听交换器的`队列命名`由`[事件类名]_[宿主项目]`这种结构构成，其中宿主项目在Ubuntu 16.04上的Docker中包含了其宿主程序的完整路径
- `RoutingKey`则由事件类名代表
- `交换器`则由事件所属的命名空间决定  

# 高级用法
----
### 自定义客户端参数
对于需要自定义一些额外客户端参数请求RabbitMQ服务器可以实现接口`IClientPropertyProvider`或继承`ClientPropertyProvider`中的`GetClientProperties`方法，然后利用IOC进行注入即可。其默认的客户端参数如下：
```
var props = new Dictionary<string, object>
{
    { "product", "EventBus" },
    { "version", typeof(EventBus).GetTypeInfo().Assembly.GetName().Version.ToString() },
    { "platform", "corefx" }
};
return props;
```  

### 自定义序列化方式
默认是采用JSON的方式进行序列化，如果用户需要改用其他方式进行序列化可以通过实现`IMessageSerializer`接口并通过IOC注入即可，其中默认的`Newtonsoft.Json`的配置如下：
```
_serializer = new JsonSerializer
{
    TypeNameAssemblyFormat = FormatterAssemblyStyle.Simple,
    Formatting = Formatting.None,
    CheckAdditionalContent = true,
    ContractResolver = new CamelCasePropertyNamesContractResolver(),
    ObjectCreationHandling = ObjectCreationHandling.Auto,
    DefaultValueHandling = DefaultValueHandling.Ignore,
    ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
    MissingMemberHandling = MissingMemberHandling.Ignore,
    PreserveReferencesHandling = PreserveReferencesHandling.Objects,
    NullValueHandling = NullValueHandling.Ignore
};
```  

### 自定义Event附加信息
需要需要在每个Event中增加附加的数据，用户可以自己实现接口`IBasicPropertiesProvider`并通过IOC注入即可，默认是包含如下额外参数：  
- `sent`：发送时的UTC时间
- `message_type`：事件的完整命名和类名  

### 自动扩缩容
如果实际的使用过程中存在高低峰的情况则可以通过启用自动扩缩容来达到在需要进行扩容的时候即时将通道数增加上去，并且在低谷期的时候缩容达到节省资源的作用。  
默认配置下是不会启用自动扩缩容的，如果用户需要使用可以通过将类`ChannelFactoryConfiguration`注入到IOC中即可，其中对应每个参数的说明如下所示：  
- `EnableScaleUp`：是否允许自动扩容
- `EnableScaleDown`：是否允许自动缩容
- `ScaleInterval`：自动扩缩容的扫描间隔时间
- `GracefulCloseInterval`：平滑关闭通道的间隔时间
- `MaxChannelCount`：最大的可创建的通道个数
- `InitialChannelCount`：初始创建的通道个数
- `WorkThreshold`：指定每个通道的消息达到该贬值才会扩容  

### 基于ElasticSearch日志记录
因为基础组件并不是业务程序，所以日志记录需要独立于常规的日志记录，为了记录基础组件的工作并保证能够持续的跟进，这里利用基于[NLog](http://nlog-project.org/)的[ElasticSearch](https://github.com/ReactiveMarkets/NLog.Targets.ElasticSearch)的扩展来支持，首先在原本的`nlog.config`中增加对应的目标配置：
```
    <extensions>
		<add assembly="NLog.Targets.ElasticSearch"/>
    </extensions>
    <target name="elastic" xsi:type="BufferingWrapper" flushTimeout="5000">
      <target xsi:type="ElasticSearch"
              name="elastic"
              uri="http://127.0.0.1:9200"
              index="eventbus"
              documentType="log"
              includeAllProperties="false"
              layout="${longdate} [${level:uppercase=true}] ${callsite:className=true:methodName=true:skipFrames=1} ${message} ${exception:format=toString,Data} @${callsite:fileName=true:includeSourcePath=true}" />
    </target>
```
其中`BufferingWrapper`是用来起到缓冲作用的，而其中的子节点就是重点的部分了，对应的配置如下所示：
- `uri`：ElasticSearch的地址
- `index`：索引的名称
- `documentType`：文档类型
- `includeAllProperties`：是否包含所有属性  

下面我们还需要过滤日志，只将我们的基础组件的日志输出，而我们基础组件基本都是以`Sino.Extensions`开头的，所以配置起来很容易：
```
<logger name="Sino.Extensions.*" minlevel="Debug" writeTo="console,elastic" />
```  
这样我们的基础组件就可以独立的记录了。   

注意，这个类库因为是基于1.1.*版本的库编写，为了能够兼容我们当前的项目版本，所以进行了降级并上传到了我们自己的私有库，请勿直接安装公网上的库。
```
Install-Package NLog.Targets.ElasticSearch
```   
## 注意事项
- 在`Exchange AutoDelete`为`True`的情况下，如果发送事件频繁，由于RabbitMQ本身设计的原因可能会导致创建交换器等出现TimeOut异常，建议将此配置改为`False`从而启用程序本身的缓存功能，避免每次发送事件都检测交换器。

## 更新记录
- 17.7.6 针对Handler处理时抛出的异常进行了记录，非异常类型的任务失败不进行记录。  
- 17.9.11 将项目升级到VS2017
- 17.9.19 补充文档
- 17.9.20 增加ElasticSearch日志记录
- 18.3.7 支持asp.net core 2.0 by y-z-f