using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Collections.Generic;
using Newtonsoft.Json;
using System;
using System.Reflection;
using Newtonsoft.Json.Linq;

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
            mQueueName = string.Format("{0}_{1}_event", mSynapse.SysName, mSynapse.AppName);
            mAlias = mSynapse.EventCallback.RegAlias();
        }

        private void mCheckAndCreateQueue()
        {
            mChannel.QueueDeclare(mQueueName, true, false, true, null);
            var eventKeys = mAlias.Keys;
            foreach (string item in eventKeys)
            {
                if (!item.Contains("*") && !item.Contains("#"))
                {
                    mChannel.QueueBind(mQueueName, mSynapse.SysName, string.Format("event.{0}", item), null);
                }
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
                    Synapse.Log(string.Format("Event Receive: {0}@{1} {2}", ea.BasicProperties.Type, ea.BasicProperties.ReplyTo, Encoding.UTF8.GetString(ea.Body)), Synapse.LogDebug);
                }
                var key = ea.RoutingKey.Replace("event.", string.Empty);
                if (mAlias.ContainsKey(key))
                {
                    var mt = mSynapse.EventCallback.GetType().GetMethod(mAlias[key]);
                    if (mt == null)
                    {
                        Synapse.Log("Event Callback not abailable.", Synapse.LogError);
                        mChannel.BasicNack(ea.DeliveryTag, false, false);
                    }
                    else
                    {
                        var paramObj = JsonConvert.DeserializeObject<JObject>(Encoding.UTF8.GetString(ea.Body));
                        var res = (bool)mt.Invoke(mSynapse.EventCallback, new object[] { paramObj, ea });
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