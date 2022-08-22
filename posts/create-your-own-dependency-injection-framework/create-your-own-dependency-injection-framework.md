---
title: 还在背依赖注入的概念？不如自己写一个依赖注入框架
slug: create-your-own-dependency-injection-framework
create_time: 2022-08-22 10:00:00
last_updated: 2022-08-22 20:02:00
description: 本文介绍了 dotnet 依赖注入是如何实现的，并手写了一个依赖注入框架 KDI。
tags:
  - CSharp
---

[TOC]

## 0. 为什么会有这篇博客

准备出去找个厂了，老在家躺着不是办法。

我是从来不背面试题的，我觉得现在网上流传的 dotnet 面试题，就像应试教育的考试题一样可笑。例如：

1. ASP.NET Core 的主要特性有哪些？
2. 什么是依赖注入？
3. 什么是 dotnet core 的 startup class？

如果问我 “ASP.NET Core 的主要特性有哪些？”，首先我要先理解你的“特性”是什么意思，是 dotnet 里那几种 Attribute？还是 features，还是在应试教育中背历史和背政治那个笼统的“特性”概念？xx 促进了 xx，推动了 xx，奠定了 xx 的基础？

如果问我“什么是依赖注入？”，我就回答：依赖注入就是注入依赖。有毛病吗？没毛病。正确吗？正确。是提问者想要的答案吗？不是。他都不会提问，让人怎么回答？除非提问者是个刚开始学编程的新手，根本不知道什么是依赖注入，我可以理解他提出这个问题。

提这些问题的面试官就像在 QQ 群和微信讨论组中的一些朋友，既无法准确描述问题，又没有给出上下文、目标、障碍、尝试、推测、日志。让我想起我的朋友 Anduin 曾经说过的话：

|![图 1 - 如何提问 - Anduin](2022-08-22-12-50-29.png)|
|:--:|
|<b>图 1 - 如何提问 - Anduin</b>|

好了，扯这么多，大家权当听个玩笑，下面进入正题，我会跟你一起自己写一个依赖注入框架。

## 1. 代码地址

代码地址：[https://github.com/Kit086/kit.demos/tree/main/DependencyInjection](https://github.com/Kit086/kit.demos/tree/main/DependencyInjection)，本文中代码不能保证没有拼写错误，建议 clone 源代码。

> 顺带一提，我用于学习 Clean Architecture 的代码已经开源：[https://github.com/Kit086/CleanArchitecture](https://github.com/Kit086/CleanArchitecture)。我并不是 Clean Architecutre 的拥趸，但是学习一下总没坏处，如果喜欢请给我一个 star，谢谢。

## 2. dotnet 的 DI 容器是如何实现的？

### 准备工作

我们尝试在控制台项目中应用一下 DI，首先用命令 `dotnet new console` 或者用 Visual Studio 创建一个控制台项目。

引入 `Microsoft.Extensions.DependencyInjection` 库。

创建一个 Service.cs：

```c#
public interface IService
{
    void DoIt();
}

public class Service : IService
{
    public void DoIt()
    {
        Console.WriteLine("Done!");
    }
}
```

有一个 IService 接口和 Service 实现类。

Program.cs：

```c#
var services = new ServiceCollection();

services.AddSingleton<IService, Service>();

var serviceProvider = services.BuildServiceProvider();

var myService = serviceProvider.GetRequiredService<IService>();

myService.DoIt();
```

输出结果：

```bash
Done!
```

- ServiceCollection 是保存已注册服务的一个集合，可以往这个集合中注册服务
- serviceProvider 可以理解为容器，使用 ServiceCollection 对象调用 `BuildServiceProvider()` 方法构建出这个容器
- 从 serviceProvider 中可以获取注册的服务，使用 `GetService()`，`GetRequiredService()` 等方法

### ServiceCollection 是什么

首先看一下 ServiceCollection 的源码：

```c#
public class ServiceCollection : IServiceCollection
{
    private readonly List<ServiceDescriptor> _descriptors = new List<ServiceDescriptor>();
    
    public int Count => _descriptors.Count;

    // ......
}
```

它实现了 IServiceCollection，它有一个属性是 `List<ServiceDescriptor>`。继续看一下 IServiceCollection：

```c#
public interface IServiceCollection : IList<ServiceDescriptor>
{
}
```

它实现了 `IList<ServiceDescriptor>`，这下我们懂了，ServiceCollection 本质上是一个 ServiceDescriptor 的集合。

### ServiceDescriptor 是什么

看一下源码：

```c#
public class ServiceDescriptor
{
    public ServiceDescriptor(
        Type serviceType,
        Type implementationType,
        ServiceLifetime lifetime)
        : this(serviceType, lifetime)
    {
        ImplementationType = implementationType;
    }

    public ServiceLifetime Lifetime { get; }
    public Type ServiceType { get; }
    public Type? ImplementationType { get; }
    public object? ImplementationInstance { get; }
    public Func<IServiceProvider, object>? ImplementationFactory { get; }

    public static ServiceDescriptor Transient<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService
    {
    }
    public static ServiceDescriptor Scoped<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService
    {
    }
    public static ServiceDescriptor Singleton<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService
    {
    }
}
```

为了方便浏览，大部分源码我都删掉了，感兴趣的可以去 Github 上的 dotnet 源码仓库看一下，实际代码比这复杂得多。

看一下它的构造函数，他接受的三个参数显然分别是：服务类型、实现类型、生命周期。

看到这里就都串起来了，ServiceCollection 实现了 `IList<ServiceDescriptor>`，我们可以自己 new 一个 ServiceDescriptor 直接 Add 进去。

我们尝试一下：

```c#
//using ......
using Microsoft.Extensions.DependencyInjection.Extensions; // 不要忘了 using 它

var services = new ServiceCollection();

var serviceDescriptor = new ServiceDescriptor(
    typeof(IService), 
    typeof(Service), 
    ServiceLifetime.Singleton);

services.Add(serviceDescriptor);

var serviceProvider = services.BuildServiceProvider();

var myService = serviceProvider.GetRequiredService<IService>();

myService.DoIt();
```

输出结果：

```bash
Done!
```

现在我们可以设计一下我们自己的 DI 框架了，我们只需要实现 ServiceCollection 和 ServiceDescriptor，以及 `BuildServiceProvider()` 和 `GetService()` 等方法即可。

## 3. 设计我们自己的 DI 框架

创建一个新的 classlib 项目，我把它命名为 KDI。然后创建一个控制台项目 KDI.Console，用来消费我们的 DI 框架，它需要添加对 KDI 项目的引用。

在 KDI.Console 中新建一个 Service 和接口 IService：

```c#
public interface IService
{
    void DoIt();
}

public class Service : IService
{
    public void DoIt()
    {
        System.Console.WriteLine("Done!");
    }
}
```

Program.cs 的最终代码差不多是这样：

```c#
var kServices = new KServiceCollection();

kServices.AddSingleton<IService, Service>();

var kServiceProvider = kServices.BuildKServiceProvider();

var service = kServiceProvider.GetKService<IService>();

service.DoIt();
```

其中的方法我们尚未实现，下面开始实现。

## 4. 实现

### 定义 KServiceDescriptor

首先我们在 KDI 项目中自定义一个 ServiceDescriptor，就叫 KServiceDescriptor：

```c#
public class KServiceDescriptor
{
    public Type ServiceType { get; set; } = default!;
    
    public Type? ImplementType { get; set; }

    public LifeTime LifeTime { get; set; }
}

public enum LifeTime
{
    Transient,
    Singleton
}
```

我知道应该是 Implementation 而不是 Implement，但是为了简单一点，我写成了 ImplementType。后面所有 Implementation 我都写为了 Implement。

同时我们自定义了 LifeTime 这个枚举，有 Singleton 和 Transient 两种生命周期。Scoped 属实是麻烦，不实现它也不影响我们理解 DI 容器工作的原理。

### 定义 KServiceProvider

在 KDI 项目中自定义一个 ServiceProvider，就叫 KServiceProvider：

```c#
public class KServiceProvider
{
    internal KServiceProvider(KServiceCollection collection)
    {
    }
    
    public T? GetKService<T>()
    {
        return (T?)GetKService(typeof(T));
    }

    private object? GetKService(Type type)
    {
        // TODO:
        return null;
    }
}
```

让它的构造函数接受一个 KServiceCollection 的参数，用于 build 这个 collection。

ServiceProvider 还有 `GetService()` 的功能，这里我们自定义一组 `GetKService()` 方法，先不实现。

构造函数是 internal 的，因为我们不希望用户自己 new KServiceProvider。

### 定义 KServiceCollection

在 KDI 项目中自定义一个 ServiceCollection，就叫 KServiceCollection：

```c#
public class KServiceCollection : List<KServiceDescriptor>
{
    public KServiceProvider BuildKServiceProvider()
    {
        return new KServiceProvider(this);
    }

    public KServiceCollection AddSingleton<TService, TImplement>()
    {
        var descriptor = AddDescriptor<TService, TImplement>(LifeTime.Singleton);
        Add(descriptor);
        return this;
    }

    public KServiceCollection AddTransient<TService, TImplement>()
    {
        var descriptor = AddDescriptor<TService, TImplement>(LifeTime.Transient);
        Add(descriptor);
        return this;
    }
    
    private static KServiceDescriptor AddDescriptor<TService, TImplement>(LifeTime lifeTime)
    {
        var descriptor = new KServiceDescriptor
        {
            ServiceType = typeof(TService),
            ImplementType = typeof(TImplement),
            LifeTime = lifeTime
        };
        return descriptor;
    }
}
```

它继承了 `List<KServiceDescriptor>`，所以它有一个继承过来的 `Add()` 方法。我们实现 `AddSingleton()` 和 `AddTransient()` 方法时，new 了一个 KServiceDescriptor（这步操作通过 private 的 `AddDescriptor()` 方法实现），用 `Add()` 方法 Add 到它本身中。

`BuildKServiceProvider()` 方法返回一个以该 KServiceCollection 为构造函数参数的 KServiceProvider 对象。

差不多已经完成三分之一了，实际上 KDI 项目已经可以编译通过了，接下来我们完善 KServiceProvider。

## 5. 完善 KServiceProvider

### 使用字典存储服务的创建方式

首先要考虑的是 KServiceProvider 如何按照 Service 注册进来的生命周期来处理服务实例的创建。我们使用两个 Dictionary 分别存储不同生命周期的服务的创建的方式。

```c#
private readonly Dictionary<Type, Func<object>> _transientDict = new();
private readonly Dictionary<Type, Lazy<object>> _singletonDict = new();
```

存储 Transient 服务的字典的值的类型是 `Func<object>`，很容易理解，因为 Transient 每次请求都给一个新实例，跟 new 一个新对象一样。所以我们每次 GetService 时都可以通过执行这个 Func 委托来 new 一个新实例。

Singleton 的字典值为 `Lazy<object>` 。为什么是 Lazy？既然 Singleton 是全场都只用一个实例，为什么不直接用 object 来存呢？

假设我们用 object 来存。初始化 KServiceProvider 时，会从传入的 KServiceCollection 中读用户注册进去的所有服务，假设我有服务 A B C 都注册为了 Singleton，A 服务依赖了 B 和 C 服务。我在初始化 KServiceProvider 时需要遍历 KServiceCollection，遍历中我们先读到 A，此时我们要 new 一个 A 的对象，存到 Singleton 的字典中，我们就需要从容器里拿 B 和 C，肯定不能直接 new，因为 B 和 C 也是 Singleton 的，直接 new 的话，相当于 A 的参数中的 B 和 C 跟其它地方的 B 和 C 不是同一个实例了，B 和 C 就不是真正的 Singleton 了。因为此时先遍历到了 A，还没有遍历到 B 和 C，所以无法从容器中拿到 B 和 C，就凉了。Lazy 加载在这里就用上了。不使用 Lazy 的话，你就只能必须按顺序注册服务了。这也是我第一次在 C# 中用 Lazy，很激动。

既然提到了我们注册服务时要取服务依赖的服务，我们就写一个方法，读当前服务构造函数参数，来获取它依赖的其它服务：

```c#
private object?[] GetDependencies(KServiceDescriptor descriptor)
{
    return descriptor.ImplementType
        .GetConstructors().First()
        .GetParameters()
        .Select(pi => GetKService(pi.ParameterType))
        .ToArray();
}
```

### 完善 `GetService()`

已经有了两个字典，就可以完善 `GetService()` 方法了，从字典中取对应 type 的实例：

```c#
private object? GetKService(Type type)
{
    if (_singletonDict.TryGetValue(type, out var lazyObj))
    {
        return lazyObj.Value;
    }
    if(_transientDict.TryGetValue(type, out var func))
    {
        return func.Invoke();
    }
    throw new Exception("You have not registered the service.");
}
```

### 完善 KServiceProvider 构造函数

```c#
internal KServiceProvider(KServiceCollection collection)
{
    foreach (var descriptor in collection)
    {
        if (descriptor.LifeTime is LifeTime.Transient)
        {
            _transientDict.Add(descriptor.ServiceType,
                () => Activator.CreateInstance(
                    descriptor.ImplementType,
                    GetDependencies(descriptor)));
        }
        else if (descriptor.LifeTime is LifeTime.Singleton)
        {
            _singletonDict.Add(descriptor.ServiceType,
                new Lazy<object>(() =>
                    Activator.CreateInstance(
                        descriptor.ImplementType, 
                        GetDependencies(descriptor))));
        }
    }
}
```

只有两个条件，我就不用 switch-case 了。构造函数中遍历传入的 KServiceCollection，构造键值对，添加到字典中。字典的值使用 Activator 创建。

对于 Transient，字典的值传入一个返回新实例的 Func 委托，在 GetService 时就可以直接 Invoke 这个 Func，拿到新实例。

对于 Singleton，字典的值传入一个 `Lazy<object>`，用户使用 KServiceProvider 获取 Singleton 服务时，说明 KServiceProvider 已经创建好了，此时 `Lazy<object>` 的 `Value` 也可以拿到了，用户可以顺利拿到 Singleton 的服务。

不知不觉，我们的 DI 框架好像已经开发完了。放一下 KServiceProvider 的完整代码，防止读者被我绕晕了：

KServiceProvider:

```c#
public class KServiceProvider
{
    private readonly Dictionary<Type, Func<object>> _transientDict = new();
    private readonly Dictionary<Type, Lazy<object>> _singletonDict = new();

    internal KServiceProvider(KServiceCollection collection)
    {
        foreach (var descriptor in collection)
        {
            if (descriptor.LifeTime is LifeTime.Transient)
            {
                _transientDict.Add(descriptor.ServiceType,
                    () => Activator.CreateInstance(
                        descriptor.ImplementType,
                        GetDependencies(descriptor)));
            }
            else if (descriptor.LifeTime is LifeTime.Singleton)
            {
                _singletonDict.Add(descriptor.ServiceType,
                    new Lazy<object>(() =>
                        Activator.CreateInstance(
                            descriptor.ImplementType, 
                            GetDependencies(descriptor))));
            }
        }
    }

    public T? GetKService<T>()
    {
        return (T?)GetKService(typeof(T));
    }

    private object? GetKService(Type type)
    {
        if (_singletonDict.TryGetValue(type, out var lazyObj))
        {
            return lazyObj.Value;
        }
        if(_transientDict.TryGetValue(type, out var func))
        {
            return func.Invoke();
        }
        throw new Exception("You have not registered the service.");
    }

    private object?[] GetDependencies(KServiceDescriptor descriptor)
    {
        return descriptor.ImplementType
            .GetConstructors().First()
            .GetParameters()
            .Select(pi => GetKService(pi.ParameterType))
            .ToArray();
    }
}
```

## 6. 试验 KDI 框架

现在我们可以运行 KDI.Console 项目了。运行结果：

```bash
Done!
```

KDI 可以正常运行。下面我们用 Guid 来测试一下两种不同生命周期。

在 KDI.Console 项目中创建一组新服务：

```c#
public interface IGuidServiceSingleton
{
    Guid Guid { get; }
}

public class GuidServiceSingleton : IGuidServiceSingleton
{
    public Guid Guid { get; } = Guid.NewGuid();
}

public interface IGuidServiceTransient
{
    Guid Guid { get; }
}

public class GuidServiceTransient : IGuidServiceTransient
{
    public Guid Guid { get; } = Guid.NewGuid();
}
```

这两对接口和实现只有名字不同，一个结尾是 Singleton，一个结尾是 Transient，用于区分生命周期。

重写一下 KDI.Console 的 Program.cs：

```c#
var kServices = new KServiceCollection();

kServices.AddSingleton<IGuidServiceSingleton, GuidServiceSingleton>();
kServices.AddTransient<IGuidServiceTransient, GuidServiceTransient>();

var kServiceProvider = kServices.BuildKServiceProvider();

var singleton1 = kServiceProvider.GetKService<IGuidServiceSingleton>();
var singleton2 = kServiceProvider.GetKService<IGuidServiceSingleton>();

var transient1 = kServiceProvider.GetKService<IGuidServiceTransient>();
var transient2 = kServiceProvider.GetKService<IGuidServiceTransient>();

Console.WriteLine($"singleton1: {singleton1.Guid}");
Console.WriteLine($"singleton2: {singleton2.Guid}");
Console.WriteLine($"transient1: {transient1.Guid}");
Console.WriteLine($"transient2: {transient2.Guid}");
```

运行结果：

```bash
singleton1: 0786e2a4-4da4-4851-94cb-8af86334734a
singleton2: 0786e2a4-4da4-4851-94cb-8af86334734a
transient1: b8a5e7d0-8d29-46ef-8972-a5c7043a96dc
transient2: 54d2174c-19a8-465d-8978-412e3dd2926d
```

没问题了。

实际上 KDI 还有很多 bug，比如 null 值检查的问题，也没有约束 `AddSingleton<TService, TImplement>` 等方法的 TImplement 类型必须实现 TService 接口，也没有实现 Remove 功能。完善这些篇幅会很长，剩下的内容都不复杂，就由您自己实现吧。

## 最后我想说的话，请务必看一下

最后我还是想说，学习编程，万万不能用应试教育时期的学习方式，否则只会事倍功半。实际上学什么都不应该用应试教育的方式。我已经在我的前几篇博客中多次讽刺过应试教育了，因为它真的毁人不倦。

我在网络上和现实中都遇到过一些使用应试教育方式学习编程的人。我不止一次听到有人说：“控制台应用中 scoped 与 singleton 一样。”。真的是这样吗？我们举例说明：

我们创建一个控制台项目，引入 `Microsoft.Extensions.DependencyInjection` 这个 Nuget 库。

创建 GuidService.cs：

```c#
public class GuidServiceScoped
{
    public Guid Id { get; } = Guid.NewGuid();
}

public class GuidServiceSingleton
{
    public Guid Id { get; } = Guid.NewGuid();
}
```

这两个 GuidService 只有类名不同，用于区分生命周期。

Program.cs:

```c#
var services = new ServiceCollection();

services.AddSingleton<GuidServiceSingleton>();
services.AddScoped<GuidServiceScoped>();

var sp = services.BuildServiceProvider();

var singletonGuidService1 = sp.GetRequiredService<GuidServiceSingleton>();
Console.WriteLine($"Singleton 1: {singletonGuidService1.Id}");

var singletonGuidService2 = sp.GetRequiredService<GuidServiceSingleton>();
Console.WriteLine($"Singleton 2: {singletonGuidService2.Id}");

var scopedGuidService1 = sp.GetRequiredService<GuidServiceScoped>();
Console.WriteLine($"Scoped 1: {scopedGuidService1.Id}");

var scopedGuidService2 = sp.GetRequiredService<GuidServiceScoped>();
Console.WriteLine($"Scoped 2: {scopedGuidService2.Id}");
```

输出结果：

```bash
Singleton 1: c269f115-5467-4d03-89ac-7e2b6b6523b9
Singleton 2: c269f115-5467-4d03-89ac-7e2b6b6523b9
Scoped 1: c7e4fd47-37fd-470e-a3d6-3e8006282e01
Scoped 2: c7e4fd47-37fd-470e-a3d6-3e8006282e01
```

我们用 Singleton 的生命周期注册了 GuidServiceSingleton，用 Scoped 的声明周期注册了 GuidServiceScoped。分别对两种生命周期注册的服务分别获取了两次，并打印了生成的 Id。看结果，似乎 Scoped 与 Singleton 生命周期的结果是相同的。我们继续看：

改写一下 Program.cs:

```c#
var services = new ServiceCollection();

services.AddSingleton<GuidServiceSingleton>();
services.AddScoped<GuidServiceScoped>();

var sp = services.BuildServiceProvider();

using (var serviceScope = sp.CreateScope())
{
    var singletonGuidService1 = serviceScope.ServiceProvider.GetRequiredService<GuidServiceSingleton>();
    Console.WriteLine($"Singleton 1: {singletonGuidService1.Id}");
}

using (var serviceScope = sp.CreateScope())
{
    var singletonGuidService2 = serviceScope.ServiceProvider.GetRequiredService<GuidServiceSingleton>();
    Console.WriteLine($"Singleton 2: {singletonGuidService2.Id}");
}

using (var serviceScope = sp.CreateScope())
{
    var scopedGuidService1 = serviceScope.ServiceProvider.GetRequiredService<GuidServiceScoped>();
    Console.WriteLine($"Scoped 1: {scopedGuidService1.Id}");
}

using (var serviceScope = sp.CreateScope())
{
    var scopedGuidService2 = serviceScope.ServiceProvider.GetRequiredService<GuidServiceScoped>();
    Console.WriteLine($"Scoped 2: {scopedGuidService2.Id}");
}
```

输出结果：

```bash
Singleton 1: 457083b6-ed86-4ea0-b851-5ecb53ad060d
Singleton 2: 457083b6-ed86-4ea0-b851-5ecb53ad060d
Scoped 1: f91b942f-935f-4a90-bc26-d0f7e61981bd
Scoped 2: 6403a669-cc94-4008-afad-bb482402069c
```

Singleton 的两次结果依然是相同的，Scoped 的两次拿到的 Id 却不同了，说明 scopedGuidService1 和 scopedGuidService2 取到的是两个不同的实例。但我们这确实是在控制台应用中，我也没有引入任何和 ASP.NET Core 相关的库。“控制台应用中 scoped 与 singleton 一样。” 是吗？不是。

能说出这句话的人至少知道控制台项目中也可以用 DI 容器，如果用应试教育的方式学编程，想知道它估计都要多学很久。

觉得震惊吗？如果你觉得这不过如此，不如我再告诉你一件事：**即使注册为 Singleton 的服务也会取到不同的实例。** 感兴趣的话，在公众号给我留言或者给我发邮件，我考虑再写一篇演示一下。

上面我代码中这种 scope 的用法往往被视为对 ServiceLocator 模式（或者说是反模式）的违背，会有安全问题，生产代码要用另一种方式，但这不是本篇博客的重点，本篇博客已经太长了，有想要知道的朋友可以给我留言或者发邮件，我会考虑再写一篇博客来介绍。

现在很多人还是在用应试教育的方式学习编程，面试官也喜欢用应试教育考试的方式提问。用这种方式学习编程的人工作几年成为老手，成为面试官，继续用这种方式面试候选人，开发者的创造力被彻底扼杀，遗祸一代一代。