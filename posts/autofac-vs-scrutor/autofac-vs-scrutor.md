---
title: 你真的需要 Autofac 吗？Scrutor：更轻量的容器伴侣
slug: autofac-vs-scrutor
create_time: 2023-3-7 23:00:00
last_updated: 2023-3-7 23:00:00
description: 这篇文章介绍了什么是 ServiceLocator 反模式，以及如何使用轻量的依赖注入扩展库 Scrutor 来取代 Autofac，并详细介绍了几种使用 Scrutor 的姿势。
tags:
  - dotnet
---

[TOC]

## 0. 为什么会有这篇文章？

这篇文章也是我对上一份工作中遇到的典型问题的思考。工作中的项目中使用了 Autofac 作为 DI 容器，仅有的作用是批量扫描 Assembly 来注册 Assembly 中的所有服务到容器中。而且广泛使用了 ServiceLocator（服务定位器）模式，现在这种模式被称为反模式，该项目使用 Autofac 的第二个目的也是使用它来实现 ServiceLocator 反模式。

> 什么是反模式？
>
> 反模式是一种常见的错误解决方案，它会导致更多的问题而不是解决问题。反模式通常是由于缺乏经验、知识或者时间而采用的，但是它们会带来负面的影响，如低效、复杂、不可维护或者不可扩展的代码或设计。
>
> 在过去的几十年里，已经有一些类和接口间的协作方法被识别和总结出来，它们是可重用的，并称之为“模式”。有些模式并没有多少益处，相反它们实际上还有很多副作用，这类模式被称为“反模式”。随着很多副作用被发现，一些模式会逐渐被抛弃并归类为反模式。反模式的识别和避免是提高软件质量和效率的重要步骤。

这篇文章主要探讨两个问题：

1. ServiceLocator 反模式的坏处，为什么我们应该尽量避免它；
2. 如果仅需要批量扫描 Assembly 进行注册，一定要用 Autofac 吗，有没有更“轻”的办法？

## 1. Autofac 与 ASP.NET Core 默认的 DI 容器

Autofac 和 ASP.NET Core 默认的 DI 容器有以下几个区别：

1. Autofac 提供了一些额外的功能，比如属性注入、泛型注册、模块化、拦截器等，而 ASP.NET Core 默认的 DI 容器比较简单，只支持构造函数注入和服务范围。
2. Autofac 可以控制控制器的生命周期，而 ASP.NET Core 默认的 DI 容器则由框架管理控制器的生命周期。
3. ASP.NET Core 默认 DI 容器只支持三种服务生命周期：单例（Singleton）、作用域（Scoped）和瞬时（Transient）。Autofac 支持多种服务生命周期，比如实例（Instance）、线程（Thread）、请求（Request）、单例（Singleton）、作用域（Scoped）等。
4. ASP.NET Core 默认 DI 容器的作用域生命周期是指在一个请求内，同一个服务只会被创建一次。Autofac 的作用域生命周期是指在一个容器内，同一个服务只会被创建一次。

这里有个大坑，默认容器的 Scoped 生命周期与 Autofac 的 Scoped 生命周期并不完全相同，这里有一篇文章，对比了两者的生命周期，如果你不太了解，请在使用 Autofac 前一定看一下：[https://devblogs.microsoft.com/cesardelatorre/comparing-asp-net-core-ioc-service-life-times-and-autofac-ioc-instance-scopes/](https://devblogs.microsoft.com/cesardelatorre/comparing-asp-net-core-ioc-service-life-times-and-autofac-ioc-instance-scopes/)

## 2. ServiceLocator 为什么是反模式

ServiceLocator 是一种常见的反模式，它指的是在应用程序中使用全局静态实例访问服务的方式。这种实现方式会导致代码难以测试和扩展，并且会使代码的依赖关系变得混乱。在 ASP.NET Core 应用程序中，ServiceLocator 反模式的典型例子是在控制器中使用 IServiceProvider 服务来获取其他服务，也有可能直接用一个静态的方法例如 `IContainer.Resolve<XXXService>()` 这种方法来获取服务。

以下是一个 ASP.NET Core 的示例代码：

```csharp
public class SampleController : ControllerBase
{
    private readonly IServiceProvider _serviceProvider;

    public SampleController(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IActionResult Index()
    {
        var myService = _serviceProvider.GetService<IMyService>();
        // Do something with myService
        return Ok();
    }
}
```

在这个例子中，控制器依赖于 IServiceProvider 服务，并且在方法中使用它来获取 IMyService 服务。这种做法是不推荐的，因为它使得控制器和 IMyService 服务之间存在紧密的耦合关系，使得控制器难以测试和难以重构。同时，使用 IServiceProvider 服务会增加代码的复杂性和降低代码的可读性。

为了避免使用 ServiceLocator 反模式，应该使用 ASP.NET Core 中的依赖注入容器。在 ASP.NET Core 中，默认的 DI 容器提供了一种简单而强大的方式来管理应用程序中的服务依赖关系。下面是使用 DI 容器的控制器示例代码：

```csharp
public class SampleController : ControllerBase
{
    private readonly IMyService _myService;

    public SampleController(IMyService myService)
    {
        _myService = myService;
    }

    public IActionResult Index()
    {
        // Do something with _myService
        return Ok();
    }
}
```

简单总结一下 Service Locator 反模式使用全局静态实例访问服务，这种做法导致了以下问题：

1. 难以测试：在使用 Service Locator 反模式的代码中，因为需要访问全局静态实例，导致在测试时无法直接注入实例，必须要使用模拟框架来模拟 Service Locator 实例，这增加了测试的复杂性和难度。
2. 降低了可扩展性：由于 Service Locator 反模式的实现方式，代码中的依赖关系变得模糊不清，使得代码的扩展变得困难，因为你无法很好地了解代码的依赖关系，也无法确定对于一个新的需求或场景，需要修改哪些部分的代码。

所以，既然我们不应该使用 Service Locator，而它又是 Autofac 被选用的重要原因之一，因为这个原因而选用 Autofac 的朋友就可以考虑是否还要使用它了。当然，如果您的软件不需要考虑可测试性和可扩展性，而是更重视使用 ServiceLocator 带来的方便性，请继续使用，不要仅仅因为不符合最佳实践而放弃特别合适的功能。

## 3. 使用 Scrutor 实现 Autofac 提供的功能

Scrutor 是 `Microsoft.Extensions.DependencyInjection` 的一个程序集（Assembly）扫描和装饰（Decoration）的扩展。既然只是个扩展，它是很轻量的。

实际上不使用 Scrutor 或 Autofac 也可以实现扫描和装饰功能，但使用 Scrutor 会更加方便，编写更少代码，使代码可读性更好。如果您不了解什么是扫描和装饰，我很快就会介绍到。

### 通过“扫描”或“匹配”来注册

Scrutor 没什么文档，需要看源码和测试用例来学习，这里我简单列几种注册方式，如果大家希望了解更多，请留言或私信，我会考虑单独开一篇文章来介绍我知道的所有 Scrutor 用法。

```csharp
namespace CommonLib.Services;

public interface ITransientService { }
public class TransientService : ITransientService { }

public interface IScopedService { }
public class ScopedService : IScopedService { }

public interface ISingletonService { }
public class SingletonService : ISingletonService { }
```

这是 `CommonLib` 程序集中的三对 Service 和 interface。我将使用 Scrutor 把它们注册到 DI 容器中。首先准备一下代码：

```csharp
builder.Services.Scan(selector =>
{
    selector.FromAssemblyOf<ITransientService>();
});

DisplayServices(builder.Services);

void DisplayServices(IServiceCollection serviceCollection)
{
    foreach (var descriptor in serviceCollection.Skip(serviceCollection.Count - 3).Take(3))
    {
        Console.WriteLine($"{descriptor.Lifetime.ToString()} - {descriptor.ServiceType.Name} - {descriptor.ImplementationType?.Name}");
    }
}
```

因为这三对服务是最后才注册到容器中的，我写了个 `DisplayServices` 方法，取最后注册进来的 3 个 Service 来输出一些基本信息。现在的输出：

```
Transient - TransientService - TransientService
Transient - ScopedService - ScopedService
Transient - SingletonService - SingletonService
```

看来使用 `selector.FromAssemblyOf<ITransientService>();` 会扫描 `ITransientService` 所在的程序集，把其中的所有 Service 以 Transient 生命周期注册到容器中。

#### 如何以 `<接口, 实现>` 的方式注册呢？

```csharp
selector.FromAssemblyOf<ITransientService>()
    .AddClasses()
    .AsMatchingInterface();

// 结果：
// Transient - ITransientService - TransientService
// Transient - IScopedService - ScopedService
// Transient - ISingletonService - SingletonService
```

这样就把该程序集的所有 class 都作为它 Matching 的接口的实现来注册到容器中了。这个 Matching 是根据名字匹配的，如果 `ITransientService` 和 `ITTTTTTTTTService` 这种就匹配不起来。如果想直接根据实现的接口来匹配，可以用 `AsImplementedInterfaces()` 替换 `AsMatchingInterface()`。

#### 如何限制只注册某些命名空间中的服务呢？

我们每个程序集中，可能有很多不需要注册到容器的东西，如果我只想注册 `CommonLib.Services` 命名空间中的服务，该怎么办？

```csharp
selector.FromAssemblyOf<ITransientService>()
    .AddClasses(filter => filter.InNamespaces("CommonLib.Services"))
    .AsMatchingInterface();
```

#### 如何修改默认的生命周期？

前面我们默认注册的生命周期都是 Transient，我如何改为 Singleton？

```csharp
selector.FromAssemblyOf<ITransientService>()
    .AddClasses(filter => filter.InNamespaces("CommonLib.Services"))
    .AsMatchingInterface()
    .WithSingletonLifetime();

// 结果：
// Singleton - ITransientService - TransientService
// Singleton - IScopedService - ScopedService
// Singleton - ISingletonService - SingletonService
```

#### 如何为每一对服务和接口都指定不同的生命周期？

可以用接口标记方法，首先准备几个生命周期标记接口：

```csharp
namespace CommonLib.LifeTimeMarker;

public interface ITransientMarker { }

public interface IScopedMarker { }

public interface ISingletonMarker { }
```

显然，我是想让实现了 `ITransientMarker` 接口的服务类都被注册为 Transient 生命周期，以此类推。

回到我们的服务类，让它们都实现对应的 Marker 接口：

```csharp
public class TransientService : ITransientService, ITransientMarker { }

public class ScopedService : IScopedService, IScopedMarker { }

public class SingletonService : ISingletonService, ISingletonMarker { }
```

修改注册代码：

```csharp
selector.FromAssemblyOf<ITransientService>()
    .AddClasses(filter => filter.InNamespaces("CommonLib.Services")
        .AssignableTo<ITransientMarker>())
    .AsMatchingInterface()
    .WithTransientLifetime();

selector.FromAssemblyOf<ITransientService>()
    .AddClasses(filter => filter.InNamespaces("CommonLib.Services")
        .AssignableTo<IScopedMarker>())
    .AsMatchingInterface()
    .WithScopedLifetime();

selector.FromAssemblyOf<ITransientService>()
    .AddClasses(filter => filter.InNamespaces("CommonLib.Services")
        .AssignableTo<ISingletonMarker>())
    .AsMatchingInterface()
    .WithSingletonLifetime();

// 结果：
// Transient - ITransientService - TransientService
// Scoped - IScopedService - ScopedService
// Singleton - ISingletonService - SingletonService
```

注意，这里就不能用 `AsImplementedInterfaces()` 替换 `AsMatchingInterface()` 了，否则会把 Marker 接口也注册进去。

#### Attribute 标记方式

用名称匹配是不是不太爽？没关系，我们可以用 Attribute 来标记，首先创建三个 Attribute：

```csharp
[AttributeUsage(AttributeTargets.Class)]
public class TransientAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Class)]
public class ScopedAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Class)]
public class SingletonAttribute : Attribute { }
```

回到服务，为各个 Service 加上对应的 Attribute：

```csharp
[Transient]
public class TransientService : ITransientService { }

[Scoped]
public class ScopedService : IScopedService { }

[Singleton]
public class SingletonService : ISingletonService { }
```

修改注册代码：

```csharp
selector.FromAssemblyOf<ITransientService>()
    .AddClasses(filter => filter.InNamespaces("CommonLib.Services")
        .WithAttribute<TransientAttribute>())
    .AsImplementedInterfaces()
    .WithTransientLifetime();

selector.FromAssemblyOf<ITransientService>()
    .AddClasses(filter => filter.InNamespaces("CommonLib.Services")
        .WithAttribute<ScopedAttribute>())
    .AsImplementedInterfaces()
    .WithScopedLifetime();

selector.FromAssemblyOf<ITransientService>()
    .AddClasses(filter => filter.InNamespaces("CommonLib.Services")
        .WithAttribute<SingletonAttribute>())
    .AsImplementedInterfaces()
    .WithSingletonLifetime();

// 结果：
// Transient - ITransientService - TransientService
// Scoped - IScopedService - ScopedService
// Singleton - ISingletonService - SingletonService
```

这种方式是非常鲜明的，所有人看到你这种代码写法，都知道你是要把某个 Service 以某种生命周期注册到容器中。与其直接扫描程序集，我还是喜欢这种标记出来的方式。代码做了什么都标记得明明白白，交接或有新人加入团队时，不需要再把这些规则口口相传。

还有很多高级玩法，如果有机会，我会再写一篇博客单独介绍。

### 装饰器模式

很多朋友选择 Autofac，是为了使用它的装饰器模式。如果您是因为这个原因而选择了它，那么恭喜您，Scrutor 是您取代 Autofac 的不二之选。注册一个装饰器就是这么简单：

```csharp
builder.Services.AddSingleton<IDecoratedService, Decorated>();

builder.Services.Decorate<IDecoratedService, Decorator>();
```

## 总结

这篇文章介绍了什么是 ServiceLocator 反模式，以及如何使用轻量的依赖注入扩展库 Scrutor 来取代 Autofac，并详细介绍了几种使用 Scrutor 的姿势。

如果您的项目只是为了扫描程序集来批量注册，或者为了实现装饰器模式而引入了 Autofac，那么您完全可以使用 Scrutor 来取代它。

您的项目也在广泛使用 ServiceLocator 反模式吗？您的项目真的有必要引入 Autofac 吗？