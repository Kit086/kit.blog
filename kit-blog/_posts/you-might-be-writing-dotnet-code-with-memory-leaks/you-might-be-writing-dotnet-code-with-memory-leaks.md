---
title: 你可能正在写内存泄漏的 .NET 代码！
slug: you-might-be-writing-dotnet-code-with-memory-leaks
create_time: 2023-10-14 01:07:00
last_updated: 2023-10-14 01:07:00
description: 本文讨论了常见的 .NET 内存泄漏的代码写法，以提醒自己在编写代码过程中警惕内存泄漏。
tags:
  - dotnet
---

[TOC]

## 0. 为什么会有这篇文章

现在是 2023 年 10 月 14 日凌晨 1 点 07 分，星期六，连上 7 天班终于放假，刚从外面嗨完回来，已经过了打盹的时间了，脑袋清醒得像个西瓜🤯，就像被 refresh 了一样。虽然知道熬夜不对，但暂时睡不着，就来水一篇博客。

最近要参与构建一个对运行时的稳定性要求较高的项目，而影响运行时稳定的重要因素就是内存泄漏。即使是经验十足的 .NET 高手，依然很难避免。我也经常搞砸，自己 review 自己的代码时才发现问题。

如果有开发者因为代码写法的小原因造成生产事故，甚至面临丢饭碗或者被“祭天”的处境，实在是可惜。

|![图 1](assets/2023-10-14-02-09-29.png)|
|:--:|
|**图 1**|

所以今天我列举一下我暂时能想起来的和能从网上查找到的内存泄漏场景与代码例子，以提醒自己。即使掌握了今天我要介绍的这几种场景，你依然可能犯同样的错误，因为我就常常犯。但知道总比不知道好。

## 1. 什么是内存泄漏？

.NET 中的内存泄漏一般是指应用程序不再需要的对象仍然被其他对象引用，从而阻止垃圾收集器（GC）回收它们的内存。这可能导致性能降低和内存溢出异常。

> 如果您暂时还不了解什么是 GC，可以看一下我翻译的这幅漫画：[一幅漫画解释 .NET 垃圾收集（GC）原理：https://cat.aiursoft.cn/post/2023/7/18/cartoon-dotnet-garbage-collection](https://cat.aiursoft.cn/post/2023/7/18/cartoon-dotnet-garbage-collection)

应用程序对象占用的内存，一般是被应用程序在堆上开辟的内存空间，被称为“托管内存（managed memory）”。.NET 世界中还有一种“非托管内存（unmanaged memory）”，我在跑内存分析的时候，经常看到非托管内存占用居高不下的情况，但很少被开发者留意。我们常常用到实现了 `IDisposable` 接口的 `Dispose()` 方法的类，比较常见的是操作与网络、文件等有关的类。如果使用了这些类而忘记 `Dispose` 掉它们，就会导致非托管内存泄漏。

下面我们开始介绍。

## 2. 匿名方法捕获类成员导致内存泄漏

匿名方法捕获类的成员会创建一个强引用，使得类的实例无法被垃圾回收器回收。下面是示例代码：

```csharp
public class MyClass
{
    private int _id;

    public MyClass(int id)
    {
        _id = id;
    }

    public void DoSomething(JobQueue jobQueue)
    {
        // 这里的匿名函数捕获了_id成员
        jobQueue.AddJob(() => Logger.Log("Job {0} done", _id));
    }
}

public class JobQueue
{
    private List<Action> _jobs = new List<Action>();

    public void AddJob(Action job)
    {
        _jobs.Add(job);
    }

    public void ExecuteJobs()
    {
        foreach (var job in _jobs)
        {
            job();
        }
        _jobs.Clear();
    }
}
```

在这段代码中，MyClass 类的实例在调用 DoSomething 方法时，向 JobQueue 类的实例添加了一个匿名函数作为任务。这个匿名函数捕获了 _id 成员，因此在 JobQueue 类的实例存在并引用这个任务委托时，它也会引用 MyClass 类的实例。这就导致了内存泄漏，因为即使 MyClass 类的实例不再被其他对象引用，它也不会被垃圾回收器回收。

要避免这种内存泄漏，可以使用一个局部变量来替代 _id 成员，例如：

``` csharp
public void DoSomething(JobQueue jobQueue)
{
    // 使用一个局部变量来替代_id成员
    int id = _id;
    jobQueue.AddJob(() => Logger.Log("Job {0} done", id));
}
```

这样就不会创建一个强引用，而是一个弱引用，当 MyClass 类的实例不再被其他对象引用时，它就可以被垃圾回收器回收。如果你想了解更多关于匿名函数捕获类的成员导致内存泄漏的情况和解决方案，你可以参考这篇文章。

## 3. 事件（event）的生命周期导致内存泄漏

事件（event）也是导致内存泄漏的惯犯，但匿名方法出现之后，事件的使用频率下降了。但还是有必要说一下。如果一个类订阅另一个生命周期较长的类的事件，它会创建一个强引用，即使不再需要订阅者，也会保持订阅者的活跃状态。

```csharp
public class Publisher
{
    public event EventHandler SomeEvent;

    public void RaiseEvent()
    {
        SomeEvent?.Invoke(this, EventArgs.Empty);
    }
}

public class Subscriber
{
    public Subscriber(Publisher publisher)
    {
        publisher.SomeEvent += OnSomeEvent;
    }

    private void OnSomeEvent(object sender, EventArgs e)
    {
        // Do something
    }
}
```

在这段代码中，每个创建的 Subscriber 实例都会订阅由传递给其构造函数的 Publisher 实例的 SomeEvent 事件。如果 Publisher 实例的生命周期比 Subscriber 实例长，例如它是一个静态或单例对象，那么 Subscriber 实例将不会被垃圾回收，除非它显式地取消订阅事件。为了避免这种内存泄漏，Subscriber 类应在不再需要时取消订阅事件，或者使用不会创建对订阅者的强引用的“弱事件处理程序”。示例代码如下：

```csharp
public class Subscriber
{
    private WeakReference weakPublisher;

    public Subscriber(Publisher publisher)
    {
        weakPublisher = new WeakReference(publisher);
        publisher.SomeEvent += OnSomeEvent;
    }

    private void OnSomeEvent(object sender, EventArgs e)
    {
        // Do something
        var publisher = weakPublisher.Target as Publisher;
        if (publisher == null)
        {
            // Publisher 已被垃圾收集，因此取消订阅该事件
            ((Publisher)sender).SomeEvent -= OnSomeEvent; // 这是你需要添加的代码
        }
    }
}
```

这个东西好奇怪，可能有经验丰富的开发者用过，反正我是查资料查到这里才知道的 `WeakReference` 这个东西。我没有尝试过上面这段代码。请尽量写自己了解的，能掌控的代码。我甚至不记得我在后端应用中用过 C# 的 event 关键字，可能客户端用的多一些？

## 4. 使用静态变量导致内存泄漏

有时候我们常常会在了解一些新东西和骚操作之后就忍不住去用它，可惜还没了解全面，导致出问题。例如静态变量、静态事件等。

静态变量是一种 GC 不收集的变量，这种东西也叫 GC Root。静态变量要在内存中长久地保存。这一般不会造成太大的内存开销。但 GC Root 引用的对象也不会被 GC 收集，这就出问题了。

但如果你使用了集合类型的静态变量，例如：

```csharp
public class MyClass
{
    static List<MyClass> _instances = new List<MyClass>();

    public MyClass()
    {
        _instances.Add(this);
    }
}
```

那每次 `new MyClass` 时，静态变量 `_instances` 都会增大一点，而且只要应用不停就不释放，慢慢膨胀。

静态事件就更不必说了。

## 5. 使用内存缓存导致内存泄漏

这种情况跟上种情况差不多，而且很容易发生。很多人喜欢在对象（特别是单例生命周期的对象）中添加一个 `List<T>` 或者 `Dictionary<int, T>` 这种集合属性，来缓存一些东西。如果使用的是写死的且 **只读** 的集合类型，那么没什么问题。但如果你的集合类型可以往里面添加项，那就麻烦大了。很少会有人手写代码和算法来从里面删除不再使用的对象。只加不减，这个集合类型的属性就会越膨胀越大。如果生命周期足够长，最终会耗尽内存。

> 对集合类型的选择，我前面曾经写过一篇文章，感兴趣请移步：[谁是你的菜？IEnumerable、IQueryable 和 ICollection 选择指南：https://cat.aiursoft.cn/post/2023/3/10/a-guide-to-ienumerable-iqueryable-and-icollection](https://cat.aiursoft.cn/post/2023/3/10/a-guide-to-ienumerable-iqueryable-and-icollection)

这里 **只读** 的集合类型并不是指用 readonly 关键字修饰的集合类型属性，而是 `IReadOnlyList<T>` 和 `IReadOnlyDictionary<int, T>` 这种只读的接口定义的属性。

建议遇到需要这样使用内存缓存的场景，改用更专业的 `IMemoryCache`。.NET 的 MemoryCache 会在一定情况下释放其中的内存，包括：

- 缓存项的过期时间到达，或者缓存项被主动移除；
- 缓存项的优先级设置为 CacheItemPriority.Low 或 CacheItemPriority.NeverRemove，当内存压力过高时，缓存项可能被回收；
- 缓存项关联了一个 IChangeToken，当该 IChangeToken 发生变化时，缓存项会被移除；
- 缓存对象被 Dispose，或者创建一个新的 MemoryCache 对象。

如果有必要，可以引入 Redis 或 MemCached 等来实现缓存，但前提是真的有必要引入。如果内存缓存能解决问题，引入外置的缓存又没有太大的增益，那就要好好取舍了。

## 6. 不 `Dispose` 导致的非托管内存泄漏

这个最容易解决了，留意操作与网络、文件等有关的类，以及各种与流（Stream）相关的类。如果它们实现了 IDisposable 或者有 `Dispose` 方法，则用 `using` 语句来把需要 `Dispose` 的对象括起来。

我曾经遇到过只依赖 GC 来处理这种情况的开发者，GC 确实能处理，因为这种类一般都有终结器（Finalizer），它长这个样子：

```csharp
~MyClassFinalizer()
{
    // ......
}
```

它一般会调用 `Dispose()` 方法。

但终结器有巨大的性能开销，详情请移步我的这篇博客：[.NET 性能技巧：为什么你应该避免使用终结器 Finalizer？：https://cat.aiursoft.cn/post/2023/3/12/net-performance-tips-why-you-should-avoid-using-finalizers](https://cat.aiursoft.cn/post/2023/3/12/net-performance-tips-why-you-should-avoid-using-finalizers)

## 7. 错误使用 unsafe 或其它直接操作内存的方法

.NET 使用 unsafe 可以分配非托管内存。后面又出了 Span、Marshal 等更“靠近”内存的东西。不是说不能用它们，而是在用的时候要清楚自己在做什么。它们的用法我估计只有真的要用时才会去学，这里当然也不讲。

## 总结

深夜乱写，写的过程中脑子已经不太清醒了，难免会语无伦次，有错误和疏漏，请见谅，请留言指出，十分感谢。祝愿大家都能拜托恼人的内存泄漏🎉🎉
