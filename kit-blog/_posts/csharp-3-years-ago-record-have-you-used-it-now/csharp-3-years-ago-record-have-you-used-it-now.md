---
title: C# 3 年前的 record 你现在用上了吗？
slug: csharp-3-years-ago-record-have-you-used-it-now
create_time: 2023-04-01 14:00:00
last_updated: 2023-04-01 14:00:00
description: 本文介绍了 record 适合在什么场景下使用，record 是不是 class，record 与 class 和 struct 在应用和底层的区别，以及为什么 record 如此被冷落。重视代码质量，让 record 发挥作用！
tags:
  - dotnet
---

答：没用上，本文完结。

以上是愚人节玩笑😁

[TOC]

## 0. 为什么会有这篇文章

record 是 C# 9.0 的主打特性之一。距离它发布快 3 年了，却没怎么见过有人使用它。虽然我现在不写代码了，但是我还是抽空学习（问 GPT-4）了一下。记录一下，分析一下它在什么场景下使用，顺便分析一下我认为它为什么这么受冷落。

## 1. record 是什么

record 类型是一种不可变的引用类型，提供了值语义，便于开发者创建不可变的对象。

Record 类型主要包括以下特性：

- 不可变性：record 类型的对象在创建后无法更改其状态。这意味着它的属性和字段都是只读的。
- 值语义：record 类型提供了值语义，这意味着在比较两个 record 对象时，它们会根据它们的属性值进行比较，而不是根据引用。这使得它们在使用时更接近于原始数据类型，如 int 和 double。
- 简洁的语法：record 类型支持 init-only 属性，可以在对象初始化时设置属性值。此外，C# 也提供了简洁的语法来声明 record 类型，称为 positional record。
- 继承：record 类型可以继承自其他 record 类型，但必须保持不可变性。
- 解构：record 类型支持解构功能，可以将对象的属性值解构为单独的变量。

以下是一个简单的 record 类型示例：

```csharp
record Point(int X, int Y);
```

在这个例子中，我们定义了一个名为 `Point` 的 record 类型，它包含两个属性 X 和 Y。使用这种简洁的语法，编译器将自动生成相应的构造函数、属性访问器以及 `Equals`、`GetHashCode` 等方法。

要创建一个新的 `Point` 对象，可以使用以下语法：

```csharp
var point = new Point(3, 4);
```

因为 record 类型是不可变的，所以在创建对象后，我们不能更改其属性值：

```csharp
point.X = 5; // 这将导致编译错误
```

## 2. record 适合在什么场景下使用？

record 类型非常适合用于表示不可变的数据结构，特别是那些需要进行值比较而不是引用比较的场景。以下是一些典型的使用 record 的场景：

1. 数据传输对象（DTO）：在 ASP.NET Core 的 Web 后端开发中，数据传输对象（DTO）通常用于在不同的应用程序层之间传递数据。由于 DTO 主要负责携带数据，而不包含业务逻辑，因此使用 record 类型非常合适。它们可以简化对象创建和保证对象不可变性，同时提供了值语义，使得比较和传递变得简单。

```csharp
record UserDto(int Id, string Name, string Email);
```

2. 领域事件：在领域驱动设计（DDD）中，领域事件表示在业务领域中发生的重要事件。领域事件通常需要不可变性，以确保在传播过程中它们的状态不会被修改。record 类型非常适合用于表示领域事件。

```csharp
record OrderCreatedEvent(int OrderId, DateTime CreatedAt);
```

3. 值对象：值对象是另一个在 DDD 中经常出现的概念，它表示一个具有特定业务含义的简单对象，例如货币、地址或日期范围等。值对象应该是不可变的，并具有值语义，因此 record 类型非常适合用于实现值对象。

```csharp
record Money(decimal Amount, string Currency);
```

4. 函数式编程：由于 record 类型具有不可变性，它们非常适合用于函数式编程范式。在函数式编程中，函数是纯粹的，这意味着它们不会产生副作用，并且对于相同的输入，总是返回相同的输出。使用 record 类型可以帮助确保数据不可变性，降低错误发生的可能性。

总之，record 类型在需要保证数据不可变性、简化对象创建和传递、以及实现值语义时非常有用。这使得它们非常适合在 DTO、领域事件、值对象等场景中使用。

## 3. record 就是 class 吗？

我认为严格来说，record 就是 class。

下面这段代码是节选了一段这个 record `record Point(int X, int Y);` 的预编译后的结果，特别长，就大体过一遍就行，看看它是如何定义的，以及实现了哪些方法：

```csharp
internal class Point : IEquatable<Point>
{
    private readonly int <X>k__BackingField;

    private readonly int <Y>k__BackingField;

    protected virtual Type EqualityContract
    {
        get
        {
            return typeof(Point);
        }
    }

    public int X
    {
        get
        {
            return <X>k__BackingField;
        }
        init
        {
            <X>k__BackingField = value;
        }
    }

    public int Y
    {
        get
        {
            return <Y>k__BackingField;
        }
        init
        {
            <Y>k__BackingField = value;
        }
    }

    public Point(int X, int Y)
    {
        <X>k__BackingField = X;
        <Y>k__BackingField = Y;
        base..ctor();
    }

    public override string ToString()
    {
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append("Point");
        stringBuilder.Append(" { ");
        if (PrintMembers(stringBuilder))
        {
            stringBuilder.Append(' ');
        }
        stringBuilder.Append('}');
        return stringBuilder.ToString();
    }

    protected virtual bool PrintMembers(StringBuilder builder)
    {
        RuntimeHelpers.EnsureSufficientExecutionStack();
        builder.Append("X = ");
        builder.Append(X.ToString());
        builder.Append(", Y = ");
        builder.Append(Y.ToString());
        return true;
    }

    public static bool operator !=(Point left, Point right)
    {
        return !(left == right);
    }

    public static bool operator ==(Point left, Point right)
    {
        return (object)left == right || ((object)left != null && left.Equals(right));
    }

    public override int GetHashCode()
    {
        return (EqualityComparer<Type>.Default.GetHashCode(EqualityContract) * -1521134295 + EqualityComparer<int>.Default.GetHashCode(<X>k__BackingField)) * -1521134295 + EqualityComparer<int>.Default.GetHashCode(<Y>k__BackingField);
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as Point);
    }

    public virtual bool Equals(Point other)
    {
        return (object)this == other || ((object)other != null && EqualityContract == other.EqualityContract && EqualityComparer<int>.Default.Equals(<X>k__BackingField, other.<X>k__BackingField) && EqualityComparer<int>.Default.Equals(<Y>k__BackingField, other.<Y>k__BackingField));
    }

    public virtual Point <Clone>$()
    {
        return new Point(this);
    }

    protected Point(Point original)
    {
        <X>k__BackingField = original.<X>k__BackingField;
        <Y>k__BackingField = original.<Y>k__BackingField;
    }

    public void Deconstruct(out int X, out int Y)
    {
        X = this.X;
        Y = this.Y;
    }
}
```

可以看到，`record` 这个单词已经消失了，`Point` 的声明变成了 `internal class Point`，也就是 class。我觉得我们可以认为 record 就是 class，不过多了一些额外的功能。

## 4. record 与 class 和 struct 有什么区别？

record、class 和 struct 都是 C# 中用于定义自定义数据类型的关键字。他们之间的主要区别如下：

1. 不可变性：record 类型是不可变的，这意味着在创建 record 对象之后，无法更改其属性值。而 class 和 struct 对象的属性值在创建后可以更改（除非显式标记为只读）。

2. 引用类型 vs 值类型：class 是引用类型，这意味着当您将一个 class 对象赋值给另一个变量时，实际上是复制引用，而不是对象本身。因此，修改其中一个变量会影响另一个变量。而 struct 是值类型，这意味着在赋值时会复制整个对象。record 类型是引用类型，但具有值语义，这意味着它们在比较时具有与值类型类似的行为。

3. 值语义：record 类型具有值语义，这意味着在比较两个 record 对象时，它们会根据它们的属性值进行比较，而不是根据引用。而 class 和 struct 的比较方式不同。class 类型在比较时使用引用语义，而 struct 类型使用值语义。

4. 继承：record 和 class 类型都支持继承，而 struct 类型不支持继承。

5. 构造函数和析构函数：class 类型可以有显式的构造函数和析构函数，而 struct 类型仅支持显式的构造函数，不支持析构函数。record 类型支持构造函数，但不支持析构函数。

6. 解构：record 类型支持解构功能，可以将对象的属性值解构为单独的变量。而 class 和 struct 类型默认不支持解构，需要手动实现解构方法。

总之，record 类型是不可变的引用类型，具有值语义，主要适用于表示不可变的数据结构。class 类型是可变的引用类型，适用于表示具有复杂行为和状态的对象。struct 类型是可变的值类型，适用于表示轻量级的、简单的数据结构。

## 5. record 与 class 和 struct 在底层（内存层面）有什么区别呢？

在内存层面上，record、class 和 struct 之间的区别主要涉及到引用类型与值类型的不同表现以及与垃圾回收相关的行为。

1. 引用类型与值类型：class 和 record 都是引用类型，它们的实例存储在托管堆上。当你将引用类型的实例分配给另一个变量时，实际上复制的是引用（内存地址），而不是对象本身。因此，两个变量指向的是同一个内存地址，修改其中一个变量的内容会影响另一个变量。而 struct 是值类型，它的实例通常存储在栈上（但在某些情况下，例如作为类的成员时，也可能存储在堆上）。值类型在赋值时会复制整个对象，因此修改一个变量的内容不会影响另一个变量。

2. 垃圾回收：由于 class 和 record 类型的实例存储在托管堆上，它们受到 .NET 垃圾回收器（GC）的管理。垃圾回收器会自动检测不再使用的对象，并释放其内存。相对地，值类型（struct）在栈上分配的内存通常在方法返回时自动回收，不受垃圾回收器管理。这意味着 struct 类型在某些情况下可能具有更高的性能，因为它们不会引发垃圾回收操作。需要注意的是，当值类型作为引用类型的成员或存储在托管堆上时，它们仍然受到垃圾回收器的管理。

3. 装箱与拆箱：由于 struct 是值类型，当将 struct 实例分配给 object 类型或实现的接口类型时，会发生装箱（boxing）操作，即将值类型封装在引用类型的对象中。在需要将引用类型对象还原为值类型时，会发生拆箱（unboxing）操作。这些操作在运行时可能导致性能损失。而对于 class 和 record 类型，不存在装箱和拆箱操作。

4. 虽然 record 类型具有值语义，但在内存层面上，它们仍然作为引用类型进行处理。这意味着 record 类型的实例存储在托管堆上，并受到垃圾回收器的管理。与 class 类型相比，record 类型的主要区别在于它们的不可变性和值语义。在选择使用 record、class 还是 struct 时，应根据所需的语义和性能特性来权衡。

## 6. 为什么 record 如此被冷落？

record 类型似乎鲜有应用，也很少听到人们讨论它。为什么它会遭到冷落？这背后反映出一个值得关注的问题——对代码质量的忽视。

在现今竞争激烈的市场环境下，确保软件产品的快速迭代和交付显得尤为重要。但与此同时，有些公司在追求开发速度时，忽略了代码质量的重要性。一些开发者为了迅速完成任务，往往采用快速但不稳定的解决方案，从而导致代码可维护性和可读性降低，甚至增加了潜在的安全隐患。

而 record 类型正是为了提高代码质量而设计的。通过使用 record，我们可以简化代码，提高代码的安全性、可预测性和易于维护性。但如果我们不重视代码质量，那么 record 类型的优势将无法发挥，甚至可能被完全忽略。

## 7. 重视代码质量，让 record 发挥作用！

“不可变性”一定程度上与“代码质量”有关系，我之前的文章 [谁是你的菜？IEnumerable、IQueryable 和 ICollection 选择指南：https://cat.aiursoft.cn/post/2023/3/10/a-guide-to-ienumerable-iqueryable-and-icollection](https://cat.aiursoft.cn/post/2023/3/10/a-guide-to-ienumerable-iqueryable-and-icollection) 中提到过“返回集合类型时应选择 `IReadOnlyCollection<T>` 或它的子类”。

返回 `IReadOnlyCollection<T>` 或其子类作为只读集合类型确实是一个很好的做法，因为这有助于保持数据的**不可变性**，从而提高代码质量。**不可变性有助于减少在多线程环境中的竞争条件，减少因错误修改而导致的 bug，并使代码更加安全、可预测和易于维护。**

使用 record 类型与使用 `IReadOnlyCollection<T>` 的理念在某种程度上是相似的。record 类型的不可变性和值语义使得它们在表示数据时更加稳定和安全。record 类型提供了一种简单、安全的创建不可变对象的方法，可以帮助我们编写更加高质量的代码。

record 类型的优点在于：

- 不可变性：record 类型的实例一旦创建，它的属性值就不再改变。这降低了出现 bug 的风险，尤其在多线程环境中，因为你无需担心其他线程意外地修改了对象的状态。

- 值语义：record 类型具有值语义，这意味着在比较和复制 record 实例时，它们的行为类似于原始数据类型（如 int 和 double）。这使得在处理数据时更容易预测它们的行为。

- 简洁的语法：record 类型通过简化 getter、setter、构造函数和 Equals 等方法的实现，使得我们能够用更少的代码来表示数据。这有助于提高代码的可读性和可维护性。

因此，在实际项目中，我们可以结合使用 record 类型和 `IReadOnlyCollection<T>` 等只读集合类型来构建更加安全、稳定和高质量的代码。例如，在表示领域事件、值对象或数据传输对象等场景中，record 类型和只读集合类型可以一起发挥作用，从而确保数据的不可变性，提高代码质量。

## 8. 但是谁在乎呢？

你是不是以为我会说：

*优秀的代码质量是软件开发的基石。优秀的代码质量不仅能提高软件的稳定性和安全性，还有助于降低维护成本和提高开发效率。因此，我们应当对代码质量给予足够的重视，从而让像 record 这样的优秀特性发挥其应有的作用。*

*现在是时候反思我们在软件开发中的价值观了。我们应当关注并提高代码质量，努力创造出更安全、更稳定、更易维护的软件。让我们一起拥抱 record，以及其他有助于提高代码质量的优秀特性，为我们的软件开发事业注入新的活力！*

*让我们共同努力，打破对代码质量的忽视，让 record 类型在实际项目中大放异彩，为我们的编程实践带来更多美好的变革！*

以前可能我会这样说，但现在谁在乎呢？没有人喜欢 record，没有人在乎 record 的感受！就像没有人在乎代码质量。自从 ChatGPT 这个产品出现之后，特别是 GPT-4 出现之后，感觉程序猿好像真的能被取代了。以后估计就靠它疯狂输出代码了。相信常常使用这些 AI 工具的朋友也能看出这篇文章中明显也有 GPT-4 的影子，它又快又好，大家都想想以后吃哪碗饭吧，谁还在乎什么 record。
