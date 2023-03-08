---
title: .NET 性能技巧：为什么你应该避免使用终结器 Finalizer？
slug: dotnet-performance-tips-finalizer
create_time: 2023-3-8 22:00:00
last_updated: 2023-3-8 22:00:00
description: 本文介绍了什么是终结器，使用终结器对性能的影响，并做了基准测试（Benchmark）来对比，且给出了优化方案。
tags:
  - dotnet
  - performance
---

[TOC]

## 0. 为什么会有这篇文章？

性能一直是 Java 和 dotnet 绕不开的话题。两者的生态都十分稳定，被很多企业选择来构建承受高并发请求的服务。初中级工程师想要编写高性能代码，去钻研 GC 这些底层的东西会事倍功半，而掌握一些性能技巧反而事半功倍。我打算水几篇博客，分享几个我的性能优化 tips。

今天分享的主题与终结器（Finalizer）相关，因为昨天在与朋友🍉哥聊天时提到了终结器，给了我文章灵感的来源。

## 1. 为什么要避免使用终结器？

首先简单介绍一下什么是垃圾收集，什么是终结器：

- 垃圾回收器是.NET 运行时的一个组件，它负责自动管理托管对象的内存分配和释放。垃圾回收器会定期检查应用程序中不再使用的对象，并回收它们占用的内存，以便为新的对象分配空间。
- 终结器是在对象被垃圾回收器回收之前执行的一段代码，它可以用来释放非托管资源，如文件句柄、数据库连接等。但是，终结器也会延迟对象的回收，增加内存占用和垃圾回收的时间。

终结器有点类似 C++ 的析构函数，dotnet 中的终结器开销非常大，原因如下：

- 任何带有终结器的类都会被垃圾回收器自动提升到新一代。这意味着它们不能在第 0 代进行垃圾收集，而第 0 代是最快的一代。无论如何，对象都会至少多存活一个生命周期。
- 当垃圾回收器发现一个对象有终结器时，它不能立即回收该对象，而是要把该对象放入一个终结队列（Finalizer Queue）中，等待一个专门的终结线程来执行终结器。这样，该对象就会多存活一个垃圾回收周期，占用更多的内存。这可能会导致一些终结器长时间运行或抛出异常的问题。
- 终结线程执行终结器的过程也会消耗一定的时间和资源，影响垃圾回收器的效率。如果终结器中的代码量过多，或者访问了其他对象，或者抛出了异常，或者没有及时调用 `Dispose` 方法，都会加剧这种影响。

如果您对垃圾回收和第 0 代、第 1 代这种术语尚且不太了解，请看我翻译的这篇文章：[一幅漫画解释 .NET 垃圾收集（GC）原理：https://blog.kitlau.dev/posts/cartoon-dotnet-garbage-collection/](https://blog.kitlau.dev/posts/cartoon-dotnet-garbage-collection/)

## 2. 性能基准测试对比

我们用 Benchmark 来测试一下终结器的性能。创建一个 dotnet 7 的 Console 程序，准备两个被测试的类，一个实现了终结器，一个没有：

```csharp
public class Person
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public DateTime BirthDate { get; set; }
}

public class PersonWithFinalizer
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public DateTime BirthDate { get; set; }

    ~PersonWithFinalizer()
    {
        // Do something
    }
}
```

然后设计一个基准测试：

```csharp
public class Test
{
    [Params(1_000, 10_000, 100_000)]
    public int _n;

    private static Person _person;
    private static PersonWithFinalizer _personWithFinalizer;

    [Benchmark]
    public void PersonTest()
    {
        for (int i = 0; i < _n; i++)
        {
            _person = new Person();
        }
    }

    [Benchmark]
    public void PersonWithFinalizerTest()
    {
        for (int i = 0; i < _n; i++)
        {
            _personWithFinalizer = new PersonWithFinalizer();
        }
    }
}
```

分别测试创建和释放一千、一万和十万个对象的性能。

运行以下命令在 Release 模式下执行基准测试：

```
dotnet run -c Release
```

结果：

```
|                  Method |     _n |          Mean |       Error |      StdDev |
|------------------------ |------- |--------------:|------------:|------------:|
|              PersonTest |   1000 |      5.886 us |   0.1125 us |   0.2141 us |
| PersonWithFinalizerTest |   1000 |    111.027 us |   2.1886 us |   4.7112 us |
|              PersonTest |  10000 |     58.469 us |   1.1306 us |   1.1611 us |
| PersonWithFinalizerTest |  10000 |  1,194.646 us |  32.3590 us |  93.3631 us |
|              PersonTest | 100000 |    717.316 us |  31.1411 us |  89.8491 us |
| PersonWithFinalizerTest | 100000 | 11,921.305 us | 269.4794 us | 786.0840 us |
```

差距可以说是巨大了，创建和释放一千个没有实现终结器的 PersonTest 对象，只消耗平均 5.886 微秒，但实现终结器的 PersonWithFinalizerTest 对象要平均 111.027 微秒。开销是没有终结器的 18.86 倍。一万条和十万条的数据我就不再扯了，上面的表格里都有，您可以自己领略一下。

我是在我的小笔记本上测试的，我的 CPU 是 AMD 的 Ryzen 5 5600U，有网友甚至测出过几百倍的差距。建议您在部署有终结器的代码时，先在您生产环境的服务器上跑一下基准测试，看一下测试的结果您是否可以接受。

## 3. 优化措施

可以让 PersonWithFinalizer 类实现 `IDisposable` 接口的 `Dispose` 方法来替代终结器：

```csharp
public class Person : IDisposable
{
    private bool _disposed = false;

    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public DateTime BirthDate { get; set; }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // 释放托管资源
            }

            // 释放非托管资源

            _disposed = true;
        }
    }
}
```

## 总结

在使用 .NET 时，应尽量避免使用终结器（Finalizer）的原因有以下几个：

1. 不可控性：终结器的执行时间是不可控的，而且不同的垃圾收集器实现会有不同的终结器执行策略，这可能会导致程序行为不稳定或不可预测。

2. 性能问题：终结器的执行需要垃圾收集器进行两次扫描，一次是标记阶段，一次是清理阶段，这会导致额外的性能开销。而且，如果有多个对象需要被终结，它们的终结器会被串行执行，可能会导致长时间的停顿。

3. 内存泄漏：终结器可能会导致内存泄漏。因为对象的终结器只有在垃圾收集器扫描到对象并判断其不再被引用时才会执行，所以如果对象被引用但没有被垃圾收集器扫描到，它的终结器就不会执行，这可能会导致资源没有正确释放，从而导致内存泄漏。

4. 可替代性：终结器的功能可以使用更可控和更可预测的方式替代。比如使用 IDisposable 接口来释放资源，或者使用最新的异步资源释放 API（IAsyncDisposable）。

因此，为了确保程序的可靠性和性能，建议尽量避免使用终结器，在资源释放方面选择更可控和更可预测的方式。