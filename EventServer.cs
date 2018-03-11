using System;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Icarus
{
    public class EventServer
    {
        private Synapse mSynapse;
        private IModel mChannel;
        private string mQueueName;
        private Dictionary<string, string> mAlias;

        public EventServer(Synapse synapse)
        {
            mSynapse = synapse;
            mChannel = mSynapse.CreateChannel(mSynapse.EventProcessNum, "EventServer");
            mQueueName = "event_" + mSynapse.AppName;
            mAlias = mSynapse.EventCallback.RegAlias();
        }

        private void mCheckAndCreateQueue()
        {
            mChannel.QueueDeclare(mQueueName, false, false, true, null);
            var eventKeys = mAlias.Keys;
            foreach (string item in eventKeys)
            {
                mChannel.QueueBind(mQueueName, Synapse.EventExchangeName, item, null);
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
                    Synapse.Log(string.Format("Event Receive: {0} {1}", ea.RoutingKey, Encoding.UTF8.GetString(ea.Body)), Synapse.LogDebug);
                }
                if (mAlias.ContainsKey(ea.RoutingKey))
                {
                    var mt = mSynapse.EventCallback.GetType().GetMethod(mAlias[ea.RoutingKey]);
                    if (mt == null)
                    {
                        Synapse.Log("Event Callback not abailable.", Synapse.LogError);
                        mChannel.BasicNack(ea.DeliveryTag, false, false);
                    }
                    else
                    {
                        var paramObj = JsonConvert.DeserializeObject<dynamic>(Encoding.UTF8.GetString(ea.Body));
                        var res = mt.Invoke(mSynapse.EventCallback, new object[] { paramObj });
                        if (res)
                        {
                            mChannel.BasicAck(ea.DeliveryTag, false);
                        }
                        else
                        {
                            mChannel.BasicNack(ea.DeliveryTag, false, true);
                        }
                    }
                }
            };
            mChannel.BasicConsume(mQueueName, false, consumer);
        }
    }
}