using RabbitMQ.Client;
namespace Icarus
{
    public class Common
    {
        public static string MqHost, MqPort, MqUser, MqPass, SysName, AppName, AppId;
        public static int RpcTimeout, ProcessNum;
        public static bool DisableEventClient, DisableRpcClient, Debug;

        protected static IConnection conn;
		protected static IModel channel;
    }
}
