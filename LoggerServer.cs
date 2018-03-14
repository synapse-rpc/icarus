using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace Rpc.Synapse.Icarus
{
    public class LoggerServer
    {
        private Synapse mSynapse;
        private IModel mChannel;
        private string mQueueName;

        public LoggerServer(Synapse synapse)
        {
            mSynapse = synapse;
            mChannel = mSynapse.CreateChannel(mSynapse.LoggerProcessNum, "Logger");
            mQueueName = string.Format("{0}_{1}_logger", mSynapse.SysName, mSynapse.AppName);
        }

        private void mCheckAndCreateQueue()
        {
            mChannel.QueueDeclare(mQueueName, true, false, true, null);
            mChannel.QueueBind(mQueueName, mSynapse.SysName, "#", null);
        }

        public void Run()
        {
            mCheckAndCreateQueue();
            EventingBasicConsumer consumer = new EventingBasicConsumer(mChannel);
            consumer.Received += (ch, ea) =>
            {
                if (mSynapse.Debug)
                {
                    Synapse.Log(string.Format("Logger Receive: {0} {1}", ea.RoutingKey, Encoding.UTF8.GetString(ea.Body)), Synapse.LogDebug);
                }
                var type = ea.RoutingKey.Split('.')[0];
                var paramObj = JsonConvert.DeserializeObject<JObject>(Encoding.UTF8.GetString(ea.Body));
                switch (type)
                {
                    case "event":
                        mSynapse.LoggerCallback.Event(paramObj, ea);
                        break;
                    case "server":
                        mSynapse.LoggerCallback.Request(paramObj, ea);
                        break;
                    case "client":
                        mSynapse.LoggerCallback.Response(paramObj, ea);
                        break;
                }
                mSynapse.LoggerCallback.All(paramObj, ea);
            };
            mChannel.BasicConsume(mQueueName, true, consumer);
        }
    }
}
