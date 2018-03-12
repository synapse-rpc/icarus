using RabbitMQ.Client.Events;
namespace Icarus
{
    public class BaseLogger
    {
        //记录所有日志
        public virtual void All(BasicDeliverEventArgs data)
        {
        }

        //记录事件日志
        public virtual void Event(BasicDeliverEventArgs data)
        {
        }

        //记录请求日志
        public virtual void Request(BasicDeliverEventArgs data)
        {
        }

        //记录响应日志
        public virtual void Response(BasicDeliverEventArgs data)
        {
        }

    }
}
