using System;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using System.Collections.Generic;

namespace Icarus
{
    public class Synapse
    {
        public string MqHost;
        public string MqPort = "5672";
        public string MqUser;
        public string MqPass;
        public string MqVHost = "/";
        public string SysName;
        public string AppName;
        public string AppId;
        public int RpcTimeout = 3;
        public int EventProcessNum = 20;
        public int RpcProcessNum = 20;
        public int LoggerProcessNum = 20;
        public bool DisableEventClient;
        public bool DisableRpcClient;
        public bool Debug;
        public BaseCallback RpcCallback;
        public BaseCallback EventCallback;
        public BaseLogger LoggerCallback;

        public static string LogInfo = "Info";
        public static string LogDebug = "Debug";
        public static string LogWarn = "Warn";
        public static string LogError = "Error";


        private IConnection mConn;

        private EventClient mEventClient;
        private RpcClient mRpcClient;

        //启动 Synapse 组件
        public void Serve()
        {
            //系统名称,应用名称
            if (AppName == null || SysName == null)
            {
                Log("Must Set SysName and AppName system exit .", LogError);
                return;
            }
            else
            {
                Log(string.Format("System Name: {0}", SysName));
                Log(string.Format("App Name: {0}", AppName));
            }

            //应用ID
            if (AppId == null)
            {
                AppId = RandomString();
            }
            Log(string.Format("App ID: {0}", AppId));

            //调试模式
            if (Debug)
            {
                Log("App Run Mode: Debug", LogDebug);
            }
            else
            {
                Log("App Run Mode: Production");
            }
            mCreateConnection();
            mCheckAndCreateExchange();

            //日志服务
            if (LoggerCallback == null)
            {
                Log("Logger Disabled: LoggerCallback not set", LogWarn);
            }
            else
            {
                new LoggerServer(this).Run();
            }

            //事件服务器
            if (EventCallback == null)
            {
                Log("Event Server Disabled: EventCallback not set", LogWarn);
            }
            else
            {
                new EventServer(this).Run();
                foreach (KeyValuePair<string, string> item in EventCallback.RegAlias())
                {
                    Log(string.Format("*EVT: {0} -> {1}", item.Key, item.Value));
                }
            }

            //RPC服务器
            if (RpcCallback == null)
            {
                Log("Rpc Server Disabled: RpcCallback not set", LogWarn);
            }
            else
            {
                new RpcServer(this).Run();
                foreach (KeyValuePair<string, string> item in RpcCallback.RegAlias())
                {
                    Log(string.Format("*RPC: {0} -> {1}", item.Key, item.Value));
                }
            }

            //事件客户端
            if (DisableEventClient)
            {
                Log("Event Client Disabled: DisableEventClient set true", LogWarn);
            }
            else
            {
                mEventClient = new EventClient(this);
                Log("Event Client Ready");
            }

            //RPC客户端
            if (DisableRpcClient)
            {
                Log("Rpc Client Disabled: DisableEventClient set true", LogWarn);
            }
            else
            {
                mRpcClient = new RpcClient(this);
                mRpcClient.Run();
                Log(string.Format("Rpc Client Timeout: {0}s", RpcTimeout));
                Log("Rpc Client Ready");
            }
        }

        //发送事件
        public void SendEvent(string eventName, Dictionary<string, object> param)
        {
            if (DisableEventClient)
            {
                Log("Event Client Disabled!", LogError);
            }
            else
            {
                mEventClient.Send(eventName, param);
            }
        }

        //发送RPC请求
        public dynamic SendRpc(string server, string method, Dictionary<string, object> param)
        {
            if (DisableRpcClient)
            {
                Log("Rpc Client Disabled!", LogError);
                return new Dictionary<string, object>() { { "rpc_error", "rpc client disabled" } }; ;
            }
            else
            {
                return mRpcClient.Send(server, method, param);
            }
        }


        //连接RabbitMQ服务器
        private void mCreateConnection()
        {
            var factory = new ConnectionFactory();
            factory.HostName = MqHost;
            factory.Port = Convert.ToInt16(MqPort);
            factory.VirtualHost = MqVHost;
            factory.UserName = MqUser;
            factory.Password = MqPass;
            try
            {
                mConn = factory.CreateConnection();
                Log("Rabbit MQ Connection Created.");
            }
            catch (BrokerUnreachableException e)
            {
                Log(string.Format("Failed to connect to RabbitMQ.\n {0}", e));
            }

        }

        //创建 RabbitMQ 通道
        public IModel CreateChannel(int processNum = 0, string desc = "unknow")
        {
            IModel channel = null;
            try
            {
                channel = mConn.CreateModel();
                Log(string.Format("{0} Channel Created", desc));
                if (processNum != 0)
                {
                    channel.BasicQos(0, Convert.ToUInt16(processNum), false);
                    Log(string.Format("{1} MaxProcessNum: {0}", RpcProcessNum, desc));
                }
            }
            catch (ConnectFailureException e)
            {
                Log(string.Format("Failed to open a channel.\n {0}", e), LogError);
            }
            return channel;
        }

        //创建交换机
        private void mCheckAndCreateExchange()
        {
            var channel = CreateChannel(0, "Exchange");
            try
            {
                channel.ExchangeDeclare(SysName, ExchangeType.Topic, true, true, null);
                Log("Register Exchange Successed.");
            }
            catch (ConnectFailureException e)
            {
                Log(string.Format("Failed to declare Exchange.\n {0}", e), LogError);
            }
            channel.Close();
            Log("Exchange Channel Closed");
        }

        //生成随机字符串
        public static string RandomString(int codeCount = 20)
        {
            int rep = 0;
            string str = string.Empty;
            long num2 = DateTime.Now.Ticks + rep;
            rep++;
            Random random = new Random(((int)(((ulong)num2) & 0xffffffffL)) | ((int)(num2 >> rep)));
            for (int i = 0; i < codeCount; i++)
            {
                char ch;
                int num = random.Next();
                if ((num % 2) == 0)
                {
                    ch = (char)(0x30 + ((ushort)(num % 10)));
                }
                else
                {
                    ch = (char)(0x41 + ((ushort)(num % 0x1a)));
                }
                str = str + ch.ToString();
            }
            return str;
        }

        //生成时间戳
        public static Int32 GetTimeStamp()
        {
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt32(ts.TotalSeconds);
        }

        //控制台日志
        public static void Log(string desc, string type = "Info")
        {
            string time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            Console.WriteLine("[{0}][Synapse {1}] {2}", time, type, desc);
        }
    }
}
