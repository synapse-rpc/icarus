using System.Text;
using System.Collections.Generic;
using RabbitMQ.Client;
using Newtonsoft.Json;

namespace Icarus
{
    public class EventClient
    {
        private Synapse mSynapse;
        private IModel mChannel;

        public EventClient(Synapse synapse)
        {
            mSynapse = synapse;
            mChannel = mSynapse.CreateChannel(0, "EventClient");
        }

        public void Send(string eventName, Dictionary<string, object> param)
        {
            var paramJson = JsonConvert.SerializeObject(param);
            var router = string.Format("event.{0}.{1}", mSynapse.AppName, eventName);
            var props = mChannel.CreateBasicProperties();
            props.AppId = mSynapse.AppId;
            props.MessageId = Synapse.RandomString();
            props.ReplyTo = mSynapse.AppName;
            props.Type = eventName;
            mChannel.BasicPublish(mSynapse.SysName, router, props, Encoding.UTF8.GetBytes(paramJson));
            if (mSynapse.Debug)
            {
                Synapse.Log(string.Format("Event Publish: {0}@{1} {2}", eventName, mSynapse.AppName, paramJson), Synapse.LogDebug);
            }
        }
    }
}
