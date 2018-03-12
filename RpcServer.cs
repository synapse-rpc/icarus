using System.Text;
using System.Collections.Generic;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Newtonsoft.Json;
using System;
using System.Reflection;
namespace Icarus
{
    public class RpcServer
    {
        private Synapse mSynapse;
        private IModel mChannel;
        private string mQueueName;
        private string mRouter;
        private Dictionary<string, string> mAlias;

        public RpcServer(Synapse synapse)
        {
            mSynapse = synapse;
            mChannel = mSynapse.CreateChannel(mSynapse.RpcProcessNum, "RpcServer");
            mQueueName = string.Format("{0}_{1}_server", mSynapse.SysName, mSynapse.AppName);
            mRouter = string.Format("server.{0}", mSynapse.AppName);
            mAlias = mSynapse.RpcCallback.RegAlias();
        }

        private void mCheckAndCreateQueue()
        {
            mChannel.QueueDeclare(mQueueName, true, false, true, null);
            var eventKeys = mAlias.Keys;
            mChannel.QueueBind(mQueueName, mSynapse.SysName, mRouter, null);
        }

        public void Run()
        {
            mCheckAndCreateQueue();
            EventingBasicConsumer consumer = new EventingBasicConsumer(mChannel);
            consumer.Received += (ch, ea) =>
            {
                if (mSynapse.Debug)
                {
                    Synapse.Log(string.Format("RPC Receive: ({2}){0}->{1}@{4} {3}", ea.BasicProperties.ReplyTo, ea.BasicProperties.Type, ea.BasicProperties.MessageId, Encoding.UTF8.GetString(ea.Body), mSynapse.AppName), Synapse.LogDebug);
                }
                var res = new Dictionary<string, object>() { { "rpc_error", "method not found" } };
                if (mAlias.ContainsKey(ea.BasicProperties.Type))
                {
                    var mt = mSynapse.RpcCallback.GetType().GetMethod(mAlias[ea.BasicProperties.Type]);
                    if (mt != null)
                    {
                        var paramObj = JsonConvert.DeserializeObject<dynamic>(Encoding.UTF8.GetString(ea.Body));
                        res = (Dictionary<string, object>)mt.Invoke(mSynapse.RpcCallback, new object[] { paramObj });
                    }
                }
                var reply = string.Format("client.{0}.{1}", ea.BasicProperties.ReplyTo, ea.BasicProperties.AppId);
                var props = mChannel.CreateBasicProperties();
                props.AppId = mSynapse.AppId;
                props.CorrelationId = ea.BasicProperties.MessageId;
                props.MessageId = Synapse.RandomString();
                props.ReplyTo = mSynapse.AppName;
                props.Type = ea.BasicProperties.Type;
                var returnJson = JsonConvert.SerializeObject(res);
                mChannel.BasicPublish(mSynapse.SysName, reply, false, props, Encoding.UTF8.GetBytes(returnJson));
                if (mSynapse.Debug)
                {
                    Synapse.Log(string.Format("Rpc Return: ({0}){1}@{2}->{3} {4}", ea.BasicProperties.MessageId, ea.BasicProperties.Type, mSynapse.AppName, ea.BasicProperties.ReplyTo, returnJson), Synapse.LogDebug);
                }
            };
            mChannel.BasicConsume(mQueueName, true, consumer);
        }
    }
}
