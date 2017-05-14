using System;
using RabbitMQ.Client;
using System.Text;
using RabbitMQ.Client.Exceptions;

namespace Icarus
{
    public class Synapse: Common
    {
        //启动 Synapse 组件, 开始监听RPC请求和事件
		public void Serve(){
            if (AppName == null || SysName == null){
                Console.WriteLine("[Synapse Error] Must Set SysName and AppName system exit .");
                return;
            }else{
                Console.WriteLine("[Synapse Info] System Name: {0}", SysName);
                Console.WriteLine("[Synapse Info] App Name: {0}", AppName);
            }
            if(ProcessNum == 0){
                ProcessNum = 100;
            }
            Console.WriteLine("[Synapse Info] App MaxProcessNum: {0}",ProcessNum);
            if(AppId == null){
                AppId = _randomString(20);
            }
            Console.WriteLine("[Synapse Info] App ID: {0}",AppId);
            if(Debug){
                Console.WriteLine("[Synapse Warn] App Run Mode: Debug");
            }else{
                Console.WriteLine("[Synapse Info] App Run Mode: Production");
            }
            goto START;
        START:
            _createConnection();
            _createChannel();
            _checkAndCreateExchange();
        }

		//生成随机字符串
		private string _randomString(int codeCount)
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

        //连接RabbitMQ服务器
        private void _createConnection(){
            var factory = new ConnectionFactory();
            factory.HostName = MqHost;
            factory.Port = Convert.ToInt16(MqPort);
            factory.UserName = MqUser;
            factory.Password = MqPass;
            try
            {
                conn = factory.CreateConnection();
                Console.WriteLine("[Synapse Info] Rabbit MQ Connection Created.");
            }
            catch (BrokerUnreachableException e)
            {
                Console.WriteLine("[Synapse Error] Failed to connect to RabbitMQ.\n {0}",e);
            }

        }

        //创建 RabbitMQ 通道
        private void _createChannel() {
            try
            {
                channel = conn.CreateModel();
                channel.BasicQos(0, Convert.ToUInt16(ProcessNum), false);
                Console.WriteLine("[Synapse Info] Rabbit MQ Channel Created.");
            }catch(ConnectFailureException e){
                Console.WriteLine("[Synapse Error] Failed to open a channel.\n {0}", e);
            }
        }

        //创建 事件监听
        private void _checkAndCreateExchange(){
            try
            {
                channel.ExchangeDeclare(SysName, "topic", true, false, null);
                Console.WriteLine("[Synapse Info] RegistermEvent Exchange Successed.");
            }catch(ConnectFailureException e){
                Console.WriteLine("[Synapse Error] Failed to declare Event Exchange.\n {0}", e);
            }
        }
	}
}
