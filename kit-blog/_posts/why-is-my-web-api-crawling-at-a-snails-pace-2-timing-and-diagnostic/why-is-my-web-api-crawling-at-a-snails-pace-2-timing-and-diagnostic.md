---
title: 为什么我的接口，慢得跟蜗牛一样啊？- 2. Serilog 记录计时和诊断日志
slug: why-is-my-web-api-crawling-at-a-snails-pace-2-timing-and-diagnostic
create_time: 2023-03-12 00:50:00
last_updated: 2023-03-12 00:50:00
description: 这篇文章介绍了 Serilog 如何轻松记录时间的度量结果，如何记录 HttpRequest 日志和诊断（diagnostic）日志。您将了解如何为您的应用配置完善的日志、计时、请求追踪和诊断信息了，让您的公司提供的软件服务的水平得到跃升。
tags:
  - dotnet
  - performance
---

[TOC]

## 1. Serilog 记录计时日志

本文源代码地址：[https://github.com/Kit086/kit.demos/tree/main/WhyIsMyWebApiCrawlingAtASnailsPace/02-TimingAndDiagnostic](https://github.com/Kit086/kit.demos/tree/main/WhyIsMyWebApiCrawlingAtASnailsPace/02-TimingAndDiagnostic)

这一节内容是紧接着上一节的，您必须首先完成 Serilog 的配置。上一节的地址：[为什么我的接口，慢得跟蜗牛一样啊 - 1. 使用 Serilog 结构化日志（https://blog.kitlau.dev/posts/why-is-my-web-api-crawling-at-a-snails-pace-1-structured-logging-with-serilog/）](https://blog.kitlau.dev/posts/why-is-my-web-api-crawling-at-a-snails-pace-1-structured-logging-with-serilog/)

上一节源代码地址：[https://github.com/Kit086/kit.demos/tree/main/WhyIsMyWebApiCrawlingAtASnailsPace/01-StructuredLoggingWithSerilog](https://github.com/Kit086/kit.demos/tree/main/WhyIsMyWebApiCrawlingAtASnailsPace/01-StructuredLoggingWithSerilog)

在度量软件产品性能，以及进行性能优化时，不成熟的技术团队可能主要靠看代码来猜。实际上为项目添加计时日志非常容易。我们今天介绍最简单的方式。

首先在项目中添加 `SerilogTimings` 这个 NuGet 包。

改造一下 WeatherForecast 接口：

```csharp
[HttpGet(Name = "GetWeatherForecast")]
public async Task<IEnumerable<WeatherForecast>> Get()
{
    using (Operation.Time("Do some DBQuery"))
    {
        await DBQuery();
    }

    using (Operation.Time("Do some IOTask"))
    {
        await IOTask();
    }

    return Enumerable.Range(1, 5).Select(index => new WeatherForecast
    {
        Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
        TemperatureC = Random.Shared.Next(-20, 55),
        Summary = Summaries[Random.Shared.Next(Summaries.Length)]
    })
    .ToArray();
}

private async Task DBQuery()
{
    await Task.Delay(100);
}

private async Task IOTask()
{
    await Task.Delay(1000);
}
```

添加了两个方法，分别模拟耗时 100ms 的数据库请求和耗时 1000ms 的 IO 任务。使用这种方式：

```csharp
using (Operation.Time("Do some DBQuery"))
{
    await DBQuery();
}
```

即可记录下任务的开销。调一下接口，打开我们记录的结构化日志看一下：

```json
{
    "@t": "2023-03-11T15:57:34.6754516Z",
    "@mt": "Do some DBQuery {Outcome} in {Elapsed:0.0} ms", // DBQuery 日志消息
    "@r": [
        "115.0" // 记录下 DBQuery 的耗时
    ],
    "Outcome": "completed",
    "Elapsed": 114.9648, // DbQuery 共耗时 114.9648ms
    "OperationId": "5909386f-7d79-44ab-84a8-8350396569a2",
    "ActionId": "1fe3505e-9d76-4b46-9602-264b43344f9d",
    "ActionName": "TimingAndDiagnostic.Controllers.WeatherForecastController.Get (TimingAndDiagnostic)",
    "RequestId": "0HMP295835I51:00000005",
    "RequestPath": "/WeatherForecast",
    "ConnectionId": "0HMP295835I51"
}
{
    "@t": "2023-03-11T15:57:35.6993135Z",
    "@mt": "Do some IOTask {Outcome} in {Elapsed:0.0} ms", // IOTask 日志消息
    "@r": [
        "1010.0" // 记录下 IOTask 的耗时
    ],
    "Outcome": "completed",
    "Elapsed": 1010.0164, // DbQuery 共耗时 1010.0164ms
    "OperationId": "a95432fb-1d38-4edd-9851-c0130a210da2",
    "ActionId": "1fe3505e-9d76-4b46-9602-264b43344f9d",
    "ActionName": "TimingAndDiagnostic.Controllers.WeatherForecastController.Get (TimingAndDiagnostic)",
    "RequestId": "0HMP295835I51:00000005",
    "RequestPath": "/WeatherForecast",
    "ConnectionId": "0HMP295835I51"
}
```

这两条日志的信息就是把 Outcome 和 Elapsed 参数拼到 @mt 中的字符串。与控制台输出的未结构化的日志相同：

```
[23:57:25 INF] Hello ASP.NET Core!
[23:57:34 INF] Do some DBQuery completed in 115.0 ms
[23:57:35 INF] Do some IOTask completed in 1010.0 ms
```

结构化日志中清晰地记录了 RequestPath 和 ActionName 等信息，我们可以轻松分析出是 IOTask 耗时太长了。

你可能现在还看不到这样做的价值，但是想象一下你是一个团队中的骨干工程师，你的职责不只是写正确的代码，你需要保证这个 App 运行得很好，让客户满意。你可能有几百种这种 SQL 查询，你需要统计分析它们都消耗了多少时间。他们可能只需要几十毫秒，但如果有一个用了超级长的时间，你至少能知道这个 Web API 为什么这么慢，知道问题出在了哪里。这就相当于你的 App 内置了一个 X 光器。

这很难吗？只需要在 program.cs 中的几行代码，appsettings.json 中的几行配置，就搞定了。但是很多自认为技术水平一流的公司和团队为什么做不到呢？

## 2. Serilog 记录 HttpRequest 日志

如果我这是一个需要记录所有用户的操作日志的管理后台，如何记录每一条 HttpRequest 日志呢？

来到 `Program.cs` 中：

```csharp
// ......
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging(); // <= 添加 Serilog 的 Http Request 日志中间件

app.UseHttpsRedirection();
// ......
```

看一下控制台日志：

```
[00:23:41 INF] Hello ASP.NET Core!
[00:23:54 INF] Do some DBQuery completed in 108.2 ms
[00:23:55 INF] Do some IOTask completed in 1007.0 ms
[00:23:55 INF] HTTP GET /WeatherForecast responded 200 in 1172.1709 ms
```

最后一条就是记录的 Http 请求的日志。看一下记录的详细的结构化日志：

```json
{
    "@t": "2023-03-11T16:23:54.8698327Z",
    "@mt": "Do some DBQuery {Outcome} in {Elapsed:0.0} ms",
    "@r": [
        "108.2"
    ],
    "Outcome": "completed",
    "Elapsed": 108.1661,
    "OperationId": "d3bb84ff-8288-43d7-8f96-6f56e183b9d9",
    "ActionId": "b2abe43d-e0a3-46be-89f2-bcc2a1bfdf5d",
    "ActionName": "TimingAndDiagnostic.Controllers.WeatherForecastController.Get (TimingAndDiagnostic)",
    "RequestId": "0HMP29JTE4HNC:00000005",
    "RequestPath": "/WeatherForecast",
    "ConnectionId": "0HMP29JTE4HNC"
}
{
    "@t": "2023-03-11T16:23:55.8879069Z",
    "@mt": "Do some IOTask {Outcome} in {Elapsed:0.0} ms",
    "@r": [
        "1007.0"
    ],
    "Outcome": "completed",
    "Elapsed": 1006.9783,
    "OperationId": "2fb3ec11-43d0-4ac9-ba49-04b194fca8da",
    "ActionId": "b2abe43d-e0a3-46be-89f2-bcc2a1bfdf5d",
    "ActionName": "TimingAndDiagnostic.Controllers.WeatherForecastController.Get (TimingAndDiagnostic)",
    "RequestId": "0HMP29JTE4HNC:00000005",
    "RequestPath": "/WeatherForecast",
    "ConnectionId": "0HMP29JTE4HNC"
}
{
    "@t": "2023-03-11T16:23:55.9164744Z",
    "@mt": "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms",
    "@r": [
        "1172.1709"
    ],
    "RequestMethod": "GET",
    "RequestPath": "/WeatherForecast",
    "StatusCode": 200,
    "Elapsed": 1172.1709,
    "SourceContext": "Serilog.AspNetCore.RequestLoggingMiddleware",
    "RequestId": "0HMP29JTE4HNC:00000005",
    "ConnectionId": "0HMP29JTE4HNC"
}
```

最后一条是 Http 请求日志。通过 RequestId，可以将这次 Http Request 中做的事串联起来进行分析。ASP.NET Core 每一条请求都有一个独一无二的 RequestId，可以根据它来过滤请求中发生了什么事。

将来在日志分析工具中，如果我们想分析 WeatherForecast 接口为什么慢，可以：

- 先根据 RequestPath 查出这个接口的所有日志；
- 分析 Http Request 的日志，它的 Elapsed 值记录了整个请求消耗的时间；
- 找一条慢的，再根据 RequestId 查询出这次请求的所有日志；
- 逐个查看耗时，直到找到很慢的操作
- 定位到代码的对应位置进行分析优化

## 3. 如何自定义 HttpRequest 日志的消息模板

修改一下中间件管道部分的代码即可：

```csharp
app.UseSerilogRequestLogging(config => 
    config.MessageTemplate = "Http {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms");
```

还可以通过这种方式进行一些其它的配置，我这里就不一一演示了。

## 4. Serilog 记录诊断（diagnostic）日志

接着前面的场景，我想记录下每条请求都是哪个用户发出的，该怎么办？因为有时候你根本关心的不是 HTTP Status code 等数据，你关心的是用户做了什么。如果某个用户遇到了问题，你希望快速发现这个用户在过去 30 分钟内做了什么，而不是让 QA 去复现这个问题。所以你希望日志里记下这条请求的用户的 userId 或 username 等信息。如果能轻松用它筛选日志，在处理线上问题时会又快又精准，大大节省人力和时间。

下面我们实现一下：改写 WeatherForecastController 的代码：

```csharp
public class WeatherForecastController : ControllerBase
{
    // ......

    private readonly ILogger<WeatherForecastController> _logger;
    private readonly IDiagnosticContext _diagnosticContext;

    public WeatherForecastController(ILogger<WeatherForecastController> logger, 
        IDiagnosticContext diagnosticContext) // 构造函数注入 IDiagnosticContext
    {
        _logger = logger;
        _diagnosticContext = diagnosticContext;
    }

    [HttpGet(Name = "GetWeatherForecast")]
    public async Task<IEnumerable<WeatherForecast>> Get()
    {
        // 使用 _diagnosticContext 设置一些元数据
        _diagnosticContext.Set("Username", "kitlau");
        _diagnosticContext.Set("UserId", 10086);

        // ......
    }
}
```

我们先是从构造函数中注入了 `IDiagnosticContext`，然后在接口中使用注入的 `_diagnosticContext` 设置了一些元数据。在生产环境中，你可以从用户的 Token 中获取到 `UserId` 或 `Username` 等值。在这里为了简单，我直接写死了。

看一下 Http Request 日志的效果：

```json
{
    "@t": "2023-03-11T16:38:36.5254965Z",
    "@mt": "Http {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms",
    "@r": [
        "1162.6959"
    ],
    "Username": "kitlau",
    "UserId": 10086,
    "RequestMethod": "GET",
    "RequestPath": "/WeatherForecast",
    "StatusCode": 200,
    "Elapsed": 1162.6959,
    "SourceContext": "Serilog.AspNetCore.RequestLoggingMiddleware",
    "RequestId": "0HMP29S25AO3J:00000005",
    "ConnectionId": "0HMP29S25AO3J"
}
```

多了 `UserId` 和 `Username` 两个字段。如果需要别的信息，也可以用这种方式添加。

## 总结

这篇文章介绍了 Serilog 如何轻松记录时间的度量结果，如何记录 HttpRequest 日志和诊断（diagnostic）日志。虽然篇幅很长，但大部分都是冗长的日志信息，实际配置起来非常容易。

看完这两篇文章，您应该已经可以为您的应用配置完善的日志、计时、请求追踪和诊断信息了。这已经能让您的公司提供的软件服务的水平提升好几个层级了。如果说哪一点还不够完善的话，我们目前还是只有记录在文件里的日志。下一篇文章我将会介绍如何收集多个实例的日志，聚合到一起，进行优雅而高效的日志分析诊断等操作。

本文源代码地址：[https://github.com/Kit086/kit.demos/tree/main/WhyIsMyWebApiCrawlingAtASnailsPace/02-TimingAndDiagnostic](https://github.com/Kit086/kit.demos/tree/main/WhyIsMyWebApiCrawlingAtASnailsPace/02-TimingAndDiagnostic)