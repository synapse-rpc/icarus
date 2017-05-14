using System;
using RabbitMQ.Client;
namespace Icarus
{
    public class RpcClient:Common
    {
		private string queue_name = SysName + "_rpc_cli_" + AppName + "_" + AppId;
		private string router = "rpc.cli." + AppName + "." + AppId;
        protected void rpcCallbackQueue(){
			var queue = channel.QueueDeclare(queue_name, true, true, true, null);
            channel.QueueBind(queue.QueueName,SysName,router,null);
        }

        protected void rpcCallbackQueueListen(){
            //    channel.BasicConsume(queue_name);
            return;
        }
    }
}