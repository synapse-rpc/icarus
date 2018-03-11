## 西纳普斯 - synapse (C# Version)

### 此为系统核心交互组件,包含了事件和RPC系统

#### 可以使用Nuget安装
> Install-Package Icarus

#### 使用前奏:
1. 需要一个RabbitMQ服务器
2. 在RabbitMQ中创建一个vHost

#### 使用方式:
```C#
    var app = new Synapse();
    app.MqHost = "xxxx";
    app.MqPort = "5672";
    app.MqUser = "guest";
    app.MqPass = "guest";
    app.SysName = "simcu";
    app.AppName = "dotNet";
    app.Debug = true;
    app.EventCallback = new TestEventServer();
    app.RpcCallback = new TestServer();
    app.Serve();
```

#### CallBack类说明:
callback类中需要有一个 public Dictionary<string, string> RegAlias() 方法,返回一个请求对应的字典,对于RPC来说为调用名和执行方法名的关联,对于EVENT来说,为监听事件和执行方法名的对应;
```C#
//两种回调必须有的注册方法
public Dictionary<string, string> RegAlias()
{
    return new Dictionary<string, string>(){
        {"dotNet.test","tb"}
    };
}

//事件回调方法
public bool tb(dynamic data)
{
    Console.WriteLine("我已经收到信息: {0}", data.msg);
    return true;
}

//RPC回调方法
public Dictionary<string, object> tb(dynamic data)
{
    var ret = new Dictionary<string, object>();
    ret.Add("suceess", "I 收到了");
    ret.Add("m", data.msg);
    ret.Add("number", 5233);
    return ret;
}
```
#### 客户端方法说明:
1. 发送事件
> Synapse.SendEvent(string eventName, Dictionary<string, object> param)

2. RPC请求
> Synapse.SendRpc(string server, string method, Dictionary<string, object> param)

3. 日志记录方法
> Synapse.Log(string desc, string type = "Info")

日志级别: LogWarn,LogError,LogInfo,LogDebug

#### 参数说明:

```C#
public string MqHost;               //MQ主机
public string MqPort;               //MQ端口
public string MqUser;               //MQ用户
public string MqPass;               //MQ密码
public string SysName;              //MQ vHost
public string AppName;              //应用名(当前应用的名字,不能于其他应用重复)
public string AppId;                //应用ID(支持分布式,不输入会每次启动自动随机生成)
public int RpcTimeout = 3;          //RPC请求超时时间(只针对客户端有效)
public int EventProcessNum = 20;    //事件服务并发量
public int RpcProcessNum = 20;      //RPC服务并发量
public bool DisableEventClient;     //禁用事件客户端
public bool DisableRpcClient;       //禁用RPC客户端
public bool Debug;                  //调试
public dynamic RpcCallback;         //RPC处理类
public dynamic EventCallback;       //Event处理类
```