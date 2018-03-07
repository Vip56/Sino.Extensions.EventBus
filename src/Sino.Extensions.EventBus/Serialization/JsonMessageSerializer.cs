using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using RabbitMQ.Client.Events;
using Sino.Extensions.EventBus.Common;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters;
using System.Text;

namespace Sino.Extensions.EventBus.Serialization
{
    /// <summary>
    /// json序列化方式
    /// </summary>
    public class JsonMessageSerializer : IMessageSerializer
    {
        private readonly JsonSerializer _serializer;

        public JsonMessageSerializer()
        {
            _serializer = new JsonSerializer
            {
                TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
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
        }

        public byte[] Serialize<T>(T obj)
        {
            if (obj == null)
            {
                return Encoding.UTF8.GetBytes(string.Empty);
            }
            string msgStr;
            using (var sw = new StringWriter())
            {
                _serializer.Serialize(sw, obj);
                msgStr = sw.GetStringBuilder().ToString();
            }
            var msgBytes = Encoding.UTF8.GetBytes(msgStr);
            return msgBytes;
        }

        public object Deserialize(BasicDeliverEventArgs args)
        {
            object typeBytes;
            if (args.BasicProperties.Headers.TryGetValue(PropertyHeaders.MessageType, out typeBytes))
            {
                var typeName = Encoding.UTF8.GetString(typeBytes as byte[] ?? new byte[0]);
                var type = Type.GetType(typeName, false);
                return Deserialize(args.Body, type);
            }
            else
            {
                var typeName = args.BasicProperties.Type;
                var type = Type.GetType(typeName, false);
                return Deserialize(args.Body, type);
            }
        }

        public T Deserialize<T>(byte[] bytes)
        {
            var obj = (T)Deserialize(bytes, typeof(T));
            return obj;
        }

        public object Deserialize(byte[] bytes, Type messageType)
        {
            object obj;
            var msgStr = Encoding.UTF8.GetString(bytes);
            using (var jsonReader = new JsonTextReader(new StringReader(msgStr)))
            {
                obj = _serializer.Deserialize(jsonReader, messageType);
            }
            return obj;
        }
    }
}
