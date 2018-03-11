using System;
using System.Text;
using System.Collections.Generic;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Newtonsoft.Json;
namespace Icarus
{
    public class RpcClient
    {
        private Synapse mSynapse;
        private IModel mChannel;
        private string mQueueName;
        private string mRouter;
        private Dictionary<string, byte[]> mResponseCache;

        public RpcClient(Synapse synapse)
        {
            mSynapse = synapse;
            mChannel = mSynapse.CreateChannel(0, "RpcClient");
            mQueueName = string.Format("{0}_client_{1}_{2}", mSynapse.SysName, mSynapse.AppName, mSynapse.AppId);
            mRouter = string.Format("client.{0}.{1}", mSynapse.AppName, mSynapse.AppId);
            mResponseCache = new Dictionary<string, byte[]>();
        }

        private void mCheckAndCreateQueue()
        {
            mChannel.QueueDeclare(mQueueName, true, false, true, null);
            mChannel.QueueBind(mQueueName, mSynapse.SysName, mRouter, null);
        }

        public void Run()
        {
            mCheckAndCreateQueue();
            EventingBasicConsumer consumer = new EventingBasicConsumer(mChannel);
            consumer.Received += (ch, ea) =>
            {
                mResponseCache.Add(ea.BasicProperties.CorrelationId, ea.Body);
                mChannel.BasicAck(ea.DeliveryTag, false);
                if (mSynapse.Debug)
                {
                    Synapse.Log(string.Format("RPC Response: ({0}){1}@{2}->{3} {4}", ea.BasicProperties.CorrelationId, ea.BasicProperties.Type, ea.BasicProperties.ReplyTo, mSynapse.AppName, Encoding.UTF8.GetString(ea.Body)), Synapse.LogDebug);
                }
            };
            mChannel.BasicConsume(mQueueName, false, consumer);
        }

        public object Send(string app, string action, Dictionary<string, object> param)
        {
            var paramJson = JsonConvert.SerializeObject(param);
            var router = string.Format("server.{0}", app);
            dynamic response;
            var props = mChannel.CreateBasicProperties();
            props.AppId = mSynapse.AppId;
            props.MessageId = Synapse.RandomString();
            props.Type = action;
            props.ReplyTo = mSynapse.AppName;
            mChannel.BasicPublish(mSynapse.SysName, router, props, Encoding.UTF8.GetBytes(paramJson));
            if (mSynapse.Debug)
            {
                Synapse.Log(string.Format("RPC Request: ({0}){4}->{1}@{2} {3}", props.MessageId, action, app, paramJson, mSynapse.AppName), Synapse.LogDebug);
            }
            var ts = Synapse.GetTimeStamp();
            while (true)
            {
                if (Synapse.GetTimeStamp() - ts > mSynapse.RpcTimeout)
                {
                    response = new Dictionary<string, object>() { { "rpc_error", "Timeout" } };
                    break;
                }
                if (mResponseCache.ContainsKey(props.MessageId))
                {
                    response = JsonConvert.DeserializeObject<dynamic>(Encoding.UTF8.GetString(mResponseCache[props.MessageId]));
                    mResponseCache.Remove(props.MessageId);
                    break;
                }
                else
                {
                    continue;
                }
            }
            return response;
        }
    }
}