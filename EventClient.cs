using System.Text;
using System;
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
            var router = mSynapse.AppName + "." + eventName;
            var props = mChannel.CreateBasicProperties();
            props.AppId = mSynapse.AppId;
            props.MessageId = Synapse.RandomString();
            props.ReplyTo = mSynapse.AppName;
            props.Type = "event";
            mChannel.BasicPublish(Synapse.EventExchangeName, router, props, Encoding.UTF8.GetBytes(paramJson));
            if (mSynapse.Debug)
            {
                Synapse.Log(string.Format("Event Publish: {0} {1}", router, paramJson), Synapse.LogDebug);
            }
        }
    }
}
