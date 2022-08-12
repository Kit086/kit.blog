---
title: 什么是异步？异步就是多线程吗？异步就是 async、await 吗？
slug: what-is-asynchronous-is-it-multi-threading-or-async-await
create_time: 2022-07-13 00:05:00
last_updated: 2022-08-12 18:24:00
description: 本篇博客介绍了什么是异步，异步与多线程的区别，不使用 await 调用异步方法，不使用 async 修饰的异步方法，同时等待多个 Task 完成等初学者可能感兴趣的异步编程知识。
tags:
  - CSharp
---



[TOC]

## 1. 什么是异步

异步编程就像去餐厅吃饭，服务员把菜单给客人，然后继续去服务其他客人了，客人点餐后再叫服务员。此时来服务这桌客人的未必是之前给这桌客人发菜单的服务员，而极有可能是此时正空闲的其它服务员。服务员再按照菜单让厨房做菜。

异步编程的**缺点**：同步编程就像是服务员一直等这桌客人点完餐，立即收走菜单去让厨房做菜，这个响应速度是很快的。而异步编程的情况下，客人点完餐后呼叫服务员时，未必有空闲的服务员，可能服务员会稍等一会才能过来。即使有空闲的服务员，也未必刚好在这桌客人身边，所以也需要时间移动到客人身边。

**优点**：整个餐厅原有的服务员人数能够同时接待更多桌客人。

## 2. 异步就是多线程吗？

不是，异步并不等于多线程。但**多线程是异步模式的一项典型用途。**

**1. 异步方法中的代码不会自动在新线程中执行；**

**2. 但可以把代码放到新线程中执行。**

### 在同一个线程中执行异步方法

```c#
public class Program
{
   public static async Task Main()
   {
       System.Console.WriteLine("1.Main: " + Thread.CurrentThread.ManagedThreadId);
       int methodResult = await MethodAsync();
       System.Console.WriteLine("2.Main: " + Thread.CurrentThread.ManagedThreadId);
    }

   private static async Task<int> MethodAsync()
   {
        System.Console.WriteLine("MethodAsync: " + Thread.CurrentThread.ManagedThreadId);
        int result = 1;
        for (int i = 0; i < 100; i++)
        {
            result += i;
        }
        return result;
    }
}

// 输出结果：
// 1.Main: 1
// MethodAsync: 1
// 2.Main: 1
```

可以看到 Main 方法和异步方法 MethodAsync 的线程 Id 都为 1。

### 使用 `Task.Run` 或 `Task.Factory.StartNew` 让代码在新线程中执行

```c#
// 调整一下 MethodAsync 的代码：
private static async Task<int> MethodAsync()
{
    System.Console.WriteLine("MethodAsync: " + Thread.CurrentThread.ManagedThreadId);
    return await Task.Run(() =>
    {
        System.Console.WriteLine("MethodAsync_Task.Run: " + Thread.CurrentThread.ManagedThreadId);
        int result = 1;
        for (int i = 0; i < 100; i++)
        {
            result += i;
        }
        return result;
    });
}

// 输出结果：
// 1.Main: 1
// MethodAsync: 1
// MethodAsync_Task.Run: 4
// 2.Main: 4
```

执行完 `Task.Run` 后，线程 Id 和后面 MainAsync 中的线程 Id 都变成 4，说明线程切换成功。

`Task.Run` 封装了 `Task.StartNew`，如果你想用更复杂参数来更加精细化控制，可以使用 `Task.StartNew`。

常规的 async/await 用法这里不再介绍，这两个关键字在 C# 5 引入后，使得异步编程非常简单了。这里简单提几个可能有初学者不知道的知识。

## 3. 不使用 await 调用异步方法会发生什么

在我看过的 dotnet 新手入门视频教程和博客中，所有的异步方法调用时都会加一个 await。我在初入 dotnet 世界的那几个月一直把它当作铁律执行。学而不思则罔，幸运的是应试教育并没有腐蚀掉我所有的思考能力，抑或是电子游戏帮我守住了大脑中最后的的独立思考阵地。我开始问自己：如果我不 await 呢？

```c#
public static void Main()
{
    System.Console.WriteLine("Start");
    System.Console.WriteLine("1.Main: " + Thread.CurrentThread.ManagedThreadId);
    Sleep10sAsync();
    System.Console.WriteLine("Stop");
    System.Console.WriteLine("2.Main: " + Thread.CurrentThread.ManagedThreadId);
}
    
private static async Task Sleep10sAsync()
{
    System.Console.WriteLine("1.Sleep10s: " + Thread.CurrentThread.ManagedThreadId);
    System.Console.WriteLine("Sleep");
    await Task.Delay(10000); // 睡 10s
    System.Console.WriteLine("2.Sleep10s: " + Thread.CurrentThread.ManagedThreadId);
    System.Console.WriteLine("Wake up");
}

// 输出结果：
// Start
// 1.Main: 1
// 1.Sleep10s: 1
// Sleep
// Stop
// 2.Main: 1
```

这段程序瞬间执行完成。`Sleep10sAsync` 方法是一个异步方法，在 Main 中并没有 await 它。

通过打印出的线程 Id，可以知道，在从 Main 执行到 Sleep10sAsync 中的 await 语句之前，一直是线程 1，最后程序结束还是线程 1，并没有等待 10s。

`Sleep10sAsync()` 方法中的以下两行代码的运行结果也没有打印到控制台：

```c#
System.Console.WriteLine("2.Sleep10s: " + Thread.CurrentThread.ManagedThreadId);
System.Console.WriteLine("Wake up");
```

现在还看不出什么，我们将 Main 方法改造一下，用 await 来调用 `Sleep10sAsync()` 方法：

```c#
public static async Task Main()
{
    System.Console.WriteLine("Start");
    System.Console.WriteLine("1.Main: " + Thread.CurrentThread.ManagedThreadId);
    await Sleep10sAsync();
    System.Console.WriteLine("Stop");
    System.Console.WriteLine("2.Main: " + Thread.CurrentThread.ManagedThreadId);
}

private static async Task Sleep10sAsync()
{
    System.Console.WriteLine("1.Sleep10s: " + Thread.CurrentThread.ManagedThreadId);
    System.Console.WriteLine("Sleep");
    await Task.Delay(10000); // 睡 10s
    System.Console.WriteLine("2.Sleep10s: " + Thread.CurrentThread.ManagedThreadId);
    System.Console.WriteLine("Wake up");
}

// 输出结果：
// Start
// 1.Main: 1
// 1.Sleep10s: 1
// Sleep
// 2.Sleep10s: 5
// Wake up
// Stop
// 2.Main: 5
```

这段程序耗时超过 10s 才执行完成。

结合这两次的执行结果可以得出结论：从 Main 开始一直执行到 `Sleep10sAsync()` 的 await 语句 `await Task.Delay(10000);` 之前，都是使用线程 1，`Sleep10sAsync()` 中的 await 操作告诉线程 1 可以先去做别的事了。等 10 秒后，线程 5 来执行后面的代码，最终也是在线程 5 上返回了 `Sleep10sAsync()` 方法，然后继续执行 Main 方法后半部分。

我们可以判断 `Task.Delay` 方法中有某段代码开启了新线程 5，与 await 这个关键字没什么关系。即：**await 并不会开启新线程。**

异步调用前的线程会在异步等待时放回线程池，异步等待结束后，会从线程池取一个空闲的线程，来运行异步等待调用结束后的后续代码。

上面这句话又引入了“异步等待”这种模糊的概念，我实在不知道怎么讲清楚，这是一个复杂的过程，或许只有看源码才能彻底理解，我们目前仅从逻辑上理解即可。

### 证明 await 并不会开启新线程

前面（2. 异步就是多线程吗？）的代码在 `Task.Run` 前面使用了 await 关键字，不能作为有力的 “await 并不会开启新线程” 的证据。我们稍微修改一下代码：

```c#
public static async Task Main()
{
    Console.WriteLine("1.Main: " + Thread.CurrentThread.ManagedThreadId);
    
    int methodResult = await MethodAsync();
    
    Console.WriteLine("2.Main: " + Thread.CurrentThread.ManagedThreadId);
}

private static async Task<int> MethodAsync()
{
    Console.WriteLine("1. MethodAsync: " + Thread.CurrentThread.ManagedThreadId);
    
    Task<int> t = Task.Run(() =>
    {
        Console.WriteLine("MethodAsync_Task.Run: " + Thread.CurrentThread.ManagedThreadId);
        
        int result = 1;
        
        for (int i = 0; i < 100; i++)
        {
            result += i;
        }
        
        Console.WriteLine("result is " + result);
        
        return result;
    });
    
    Thread.Sleep(1000); // 等 1s，防止下面的 Console.WriteLine 比 Task.Run 中的 Console.WriteLine 提前执行。此处不使用 await Task.Delay，防止引入新的 await 造成混淆
    
    Console.WriteLine("2. MethodAsync: " + Thread.CurrentThread.ManagedThreadId);
    
    return await t;
}

// 输出结果：
// 1.Main: 1
// 1. MethodAsync: 1
// MethodAsync_Task.Run: 6
// result is 4951
// 2. MethodAsync: 1
// 2.Main: 1
```

该例子并没有 `await Task.Run`，但在 `Task.Run` 中的 `Console.WriteLine("MethodAsync_Task.Run: " + Thread.CurrentThread.ManagedThreadId);` 执行时打印的结果依旧可以发现线程切换到了 6。能够证明**新线程并不是 await 开启的，而是 `Task.Run` 开启的。**

既然开启新线程的不是 await 关键字，说明不需要 await 关键字也可以调用异步方法，那么 await 关键字的作用是什么？

## 4. await 的作用是什么？

我目前了解的 await 的作用是：

1. 在当前操作（被 await 的这行代码）完成前，后续的代码暂不执行，但 await 不会阻塞当前线程；
2. 执行到 await 表达式时，代码会检查该表达式是否已经得到执行结果，如果还没有，他就会创建一个续延（continuation），当 await 的操作结束后，再执行这个续延。续延会执行方法的剩余内容（await 表达式之后的内容）；
3. await 运算符会执行拆封操作，将 `Task<T>` 变为 `T` 类型的对象。

await 还有很多要注意的点，特别是续延这个概念，我们可以后面另外开一篇文章说一说。

## 5. 先调用异步方法，后 await

既然知道不靠 await 来开启新线程，我们就可以先调用异步方法（大部分情况下是处理需要很长时间的网络请求和数据 I/O），然后去做别的事，等最后 await 异步方法返回的 Task，来保证异步方法执行完成。如果有返回值，await 还可以拆封，将 `Task<T>` 类型的返回值拆封出 `T` 类型的对象。

```c#
public static async Task Main()
{
    Console.WriteLine("Start");
    Console.WriteLine("1.Main: " + Thread.CurrentThread.ManagedThreadId);
    
    Task<int> t = Sleep10sAndReturn100Async();
    
    Console.WriteLine("Stop");
    
    int result = await t;

    Console.WriteLine(result);
    
    Console.WriteLine("2.Main: " + Thread.CurrentThread.ManagedThreadId);
}

private static async Task<int> Sleep10sAndReturn100Async()
{
    Console.WriteLine("1.Sleep10s: " + Thread.CurrentThread.ManagedThreadId);
    Console.WriteLine("Sleep");
    
    await Task.Delay(10000); // 睡 10s
    
    Console.WriteLine("2.Sleep10s: " + Thread.CurrentThread.ManagedThreadId);
    Console.WriteLine("Wake up");
    
    return 100;
}

// 输出结果：
// Start
// 1.Main: 1
// 1.Sleep10s: 1
// Sleep
// Stop
// 2.Sleep10s: 4
// Wake up
// 100
// 2.Main: 4
```

在初学 dotnet 时，我错误地认为一定要 await 一个异步方法，才会调用这个异步方法。通过这个例子，我们：

1. 把 `Sleep10sAndReturn100Async()` 方法的返回值直接赋值给一个 `Task<int> t` 对象；
2. 再 `Console.WriteLine("Sleep");`；
3. 再 await 这个 `Task<int> t` 对象。

观察输出的结果，重点观察 `Sleep` 和 `Stop` 的打印顺序是：
1. 先打印了 `Sleep`
2. 后打印了 `Stop`

即运行到 `Main()` 方法中的 `Task<int> t = Sleep10sAndReturn100Async();` 这行代码时，就开始了 `Sleep10sAndReturn100Async()` 方法的执行。

而 `Main()` 方法中 `Console.WriteLine("Stop");` 语句在 `int result = await t;` 语句之前，即打印 `Stop` 的语句在 await 语句之前。也就是还没有 `await t`，`Sleep10sAndReturn100Async()` 方法就已经开始执行了。

由此可以证明：当调用异步方法时，异步方法就开始执行。await 语句只是等待它执行完成后再继续做后面的事。

在调用异步方法和 await 这个异步方法返回的 Task 之间，你还可以去做别的事。比如你还可以再运行一个与前一个异步方法无关的另一个异步方法：

```c#
public static async Task Main()
{
    Console.WriteLine("Start");
    Console.WriteLine("1.Main: " + Thread.CurrentThread.ManagedThreadId);
    Task<int> t1 = Sleep10sAndReturn100Async();
    Task<int> t2 = Sleep5sAndReturn200Async();
    
    Console.WriteLine("Stop");
    int result1 = await t1;
    int result2 = await t2;
    
    Console.WriteLine(result1);
    Console.WriteLine(result2);
    Console.WriteLine("2.Main: " + Thread.CurrentThread.ManagedThreadId);
}

private static async Task<int> Sleep10sAndReturn100Async()
{
    Console.WriteLine("1.Sleep10sAndReturn100Async: " + Thread.CurrentThread.ManagedThreadId);
    Console.WriteLine("Sleep10sAndReturn100Async: Sleep");
    
    await Task.Delay(10000); // 睡 10s
    
    Console.WriteLine("2.Sleep10sAndReturn100Async: " + Thread.CurrentThread.ManagedThreadId);
    Console.WriteLine("Sleep10sAndReturn100Async: Wake up");
    
    return 100;
}

private static async Task<int> Sleep5sAndReturn200Async()
{
    Console.WriteLine("1.Sleep5sAndReturn200Async: " + Thread.CurrentThread.ManagedThreadId);
    Console.WriteLine("Sleep5sAndReturn200Async: Sleep");
    
    await Task.Delay(5000); // 睡5s
    
    Console.WriteLine("2.Sleep5sAndReturn200Async: " + Thread.CurrentThread.ManagedThreadId);
    Console.WriteLine("Sleep5sAndReturn200Async: Wake up");
    
    return 200;
}

// 输出结果：
// 1.Main: 1
// 1.Sleep10sAndReturn100Async: 1
// Sleep10sAndReturn100Async: Sleep
// 1.Sleep5sAndReturn200Async: 1
// Sleep5sAndReturn200Async: Sleep
// Stop
// 2.Sleep5sAndReturn200Async: 8
// Sleep5sAndReturn200Async: Wake up
// 2.Sleep10sAndReturn100Async: 8
// Sleep10sAndReturn100Async: Wake up
// 100
// 200
// 2.Main: 8
```

这个例子中先执行了 `Sleep10sAndReturn100Async()` ，但并未 await 它返回的 Task t1，而是接着执行了 `Sleep5sAndReturn200Async()` 。后面先后 await 了它们两个返回的 Task t1 和 t2。

观察运行结果可以发现 `Sleep10sAndReturn100Async()` 先开始执行，然后 `Sleep5sAndReturn200Async()` 开始执行，但是 `Sleep5sAndReturn200Async()` 却先结束，整个程序的运行时间也只有 10 秒钟多一点，而不是 15 秒钟多。说明这两个方法是并行运行的。

可以印证前面对 await 功能的描述：await 就是看一下异步方法返回的 `Task<T>` 是否完成，如果完成了，直接拆封，拿到 `T` 类型的返回值；如果该 `Task<T>` 尚未完成，就等待它完成再拆封拿返回值，再继续运行后续代码。如果 await 没有返回值的 `Task`，就只是单纯等待它完成。

## 6. 不使用 async 修饰的异步方法

前面提了（3. 不使用 await 调用异步方法），现在看一下不使用 async 修饰的异步方法：

```c#
public static async Task Main()
{
    System.Console.WriteLine("1.Main: " + Thread.CurrentThread.ManagedThreadId);
    int methodResult = await MethodAsync();
    Console.WriteLine(methodResult);
    await MethodWithoutReturnValueAsync();
    System.Console.WriteLine("2.Main: " + Thread.CurrentThread.ManagedThreadId);
}

private static Task<int> MethodAsync()
{
    System.Console.WriteLine("MethodAsync: " + Thread.CurrentThread.ManagedThreadId);
    
    int result = 1;
    for (int i = 0; i < 100; i++)
    {
        result += i;
    }
    return Task.FromResult(result);
}

private static Task MethodWithoutReturnValueAsync()
{
    System.Console.WriteLine("MethodWithoutReturnValueAsync: " + Thread.CurrentThread.ManagedThreadId);
    return Task.CompletedTask;
}

// 输出结果：
// 1.Main: 1
// MethodAsync: 1
// 4951
// MethodWithoutReturnValueAsync: 1
// 2.Main: 1
```

### `Task.FromResult()`

`MethodAsync()` 返回 `Task<int>` ，没有用 async 修饰这个方法，所以该方法体中也没有 await。return 时可以使用 `Task.FromResult(xxx)`。**`Task.FromResult()` 总是返回一个已经完成的 Task。**

### `Task.CompletedTask`

`MethodWithoutReturnValueAsync()` 方法是返回 `Task` 的方法，没有返回值，return `Task.CompletedTask` 即可。

## 7. `Task.WhenAll()` 同时等待多个 Task 完成

本来这不属于本篇博客的范围，但是前面（5. 先调用异步方法，后 await）中有示例代码同时运行过多个异步方法，我是分别 await 了这几个方法返回的 Task 来判断异步方法是否运行完成以及取值的。为了预防有初学者误会，这里讲一下可以直接使用 `Task.WhenAll()` 同时等待多个 Task 完成，不必一个一个 await。

我们改造一下（6. 不使用 async 修饰的异步方法）中的 `Main()` 方法：

```c#
public static async Task Main()
{
    System.Console.WriteLine("1.Main: " + Thread.CurrentThread.ManagedThreadId);
    
    Task<int> t1 = MethodAsync();
    Task t2 = MethodWithoutReturnValueAsync();
    
    await Task.WhenAll(t1, t2);
    
    Console.WriteLine(await t1);
    
    System.Console.WriteLine("2.Main: " + Thread.CurrentThread.ManagedThreadId);
}

// 输出结果：
// 1.Main: 1
// MethodAsync: 1
// MethodWithoutReturnValueAsync: 1
// 4951
// 2.Main: 1
```

这段代码与（6. 不使用 async 修饰的异步方法）的代码仅有 `Main()` 方法不同。这里使用了 `await Task.WhenAll(t1, t2, ......);` 来等待多个异步方法全部执行完成，再进行后续操作。

后续操作中可以直接使用 await 关键字来取出 Task 的返回值，不会再重新执行方法。

因为这里等待了两个方法都完成，才取了 `MethodAsync` 的返回值打印出来，所以这个例子的输出结果与（6. 不使用 async 修饰的异步方法）的输出结果顺序不同，不影响异步方法的执行。

**同时等待多个 Task 完成的前提条件是这几个 Task 互不相关，不互相依赖。**

## 总结

这篇文章实际上只讲了异步编程的一些表象，足够用来开发了，但如果不理解异步编程的原理，很难放心大胆地使用。后面有时间的话我会尝试把我理解的异步原理讲得尽量简洁明确一些。