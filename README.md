﻿## 西纳普斯 - synapse (C# Version)

### 此为系统核心交互组件,包含了事件和RPC系统

#### 包地址
> https://www.nuget.org/packages/Rpc.Synpase.Icarus

#### 可以使用Nuget安装
> Install-Package Rpc.Synapse.Icarus

#### Demo 程序
> https://github.com/synapse-rpc/icarus-test

#### 使用前奏:
1. 需要一个RabbitMQ服务器

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
所有的Callback均需要继承BaseCallback类
注意: 不支持#和*通配符
```C#
public class BaseCallback
{
    public virtual Dictionary<string, string> RegAlias()
    {
        return new Dictionary<string, string>() { };
    }
}
```
RPC回调方法类型:
```C#
// data 为json反序列化后的对象
// ea 是mq接收到的原始数据
public JObject tb(JObject data, BasicDeliverEventArgs ea)
{
    var ret = new Dictionary<string, object>();
    ret.Add("suceess", "I 收到了");
    ret.Add("m", data.msg);
    ret.Add("number", 5233);
    return ret;
}
```

事件回调方法类型:
```C#
// data 为json反序列化后的对象
// ea 是mq接收到的原始数据
// 返回true系统将会应答消息,返回false系统将重新将消息放入队列
public bool tb(JObject data, BasicDeliverEventArgs ea)
{
    return true;
}
```
#### 日志说明:
LoggerServer实现了全局日志功能,回调需要继承 BaseLogger
```C#
public class BaseLogger
{
    //记录所有日志
    public virtual void All(JObject data, BasicDeliverEventArgs ea)
    {
    }

    //记录事件日志
    public virtual void Event(JObject data, BasicDeliverEventArgs ea)
    {
    }

    //记录请求日志
    public virtual void Request(JObject data, BasicDeliverEventArgs ea)
    {
    }

    //记录响应日志
    public virtual void Response(JObject data, BasicDeliverEventArgs ea)
    {
    }
}
```

#### 客户端方法说明:
1. 发送事件
> Synapse.SendEvent(string eventName, JObject param)

2. RPC请求
> Synapse.SendRpc(string server, string method, JObject param)

3. 控制台日志
> Synapse.Log(string desc, string type = "Info")

日志级别: LogWarn,LogError,LogInfo,LogDebug

#### 参数说明:

```C#
public string MqHost;               //MQ主机
public string MqPort = "5672";      //MQ端口
public string MqUser;               //MQ用户
public string MqPass;               //MQ密码
public string MqVHost = "/";        //MQ虚拟机名称,默认为/
public string SysName;              //系统名称(都处于同一个系统下才能通讯)
public string AppName;              //应用名(当前应用的名字,不能于其他应用重复)
public string AppId;                //应用ID(支持分布式,不输入会每次启动自动随机生成)
public int RpcTimeout = 3;          //RPC请求超时时间(只针对客户端有效)
public int EventProcessNum = 20;    //事件服务并发量
public int RpcProcessNum = 20;      //RPC服务并发量
public bool DisableEventClient;     //禁用事件客户端
public bool DisableRpcClient;       //禁用RPC客户端
public bool Debug;                  //调试
public BaseCallback RpcCallback;    //RPC处理类(不指定默认禁用)
public BaseCallback EventCallback;  //Event处理类(不指定默认禁用)
public BaseLogger LoggerCallback;   //日志处理类(不指定默认禁用)
```
