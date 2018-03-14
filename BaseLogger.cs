using RabbitMQ.Client.Events;
using Newtonsoft.Json.Linq;
namespace Icarus
{
    public class BaseLogger
    {
        //记录所有日志
        public virtual void All(JObject data, BasicDeliverEventArgs ea)
        {
        }

        //记录事件日志
        public virtual void Event(JObject data, BasicDeliverEventArgs ea)
        {
        }

        //记录请求日志
        public virtual void Request(JObject data, BasicDeliverEventArgs ea)
        {
        }

        //记录响应日志
        public virtual void Response(JObject data, BasicDeliverEventArgs ea)
        {
        }

    }
}
