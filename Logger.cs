using System.Collections.Generic;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Newtonsoft.Json;
using System.Text;
namespace Icarus
{
    public class Logger
    {
        private Synapse mSynapse;
        private IModel mChannel;
        private string mQueueName;
        private Dictionary<string, string> mAlias;

        public Logger(Synapse synapse)
        {
            mSynapse = synapse;
            mChannel = mSynapse.CreateChannel(mSynapse.LoggerProcessNum, "Logger");
            mQueueName = string.Format("{0}_logger_{1}", mSynapse.SysName, mSynapse.AppName);
            mAlias = mSynapse.LoggerCallback.RegAlias();
        }

        private void mCheckAndCreateQueue()
        {
            mChannel.QueueDeclare(mQueueName, true, false, true, null);
            var eventKeys = mAlias.Keys;
            foreach (string item in eventKeys)
            {
                mChannel.QueueBind(mQueueName, mSynapse.SysName, item, null);
            }
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
                if (mAlias.ContainsKey(ea.RoutingKey))
                {
                    var mt = mSynapse.LoggerCallback.GetType().GetMethod(mAlias[ea.RoutingKey]);
                    if (mt == null)
                    {
                        Synapse.Log("Logger Callback not abailable.", Synapse.LogError);
                        mChannel.BasicNack(ea.DeliveryTag, false, false);
                    }
                    else
                    {
                        var paramObj = JsonConvert.DeserializeObject<dynamic>(Encoding.UTF8.GetString(ea.Body));
                        mt.Invoke(mSynapse.LoggerCallback, new object[] { ea });
                    }
                }
            };
            mChannel.BasicConsume(mQueueName, true, consumer);
        }
    }
}
