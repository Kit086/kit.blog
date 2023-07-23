---
title: 如何使用 Optional 模式解决 C# 中的烦人的空引用问题
slug: solving-null-reference-problem-with-optional-pattern-in-csharp
create_time: 2023-07-22 23:00:00
last_updated: 2023-07-22 23:00:00
description: 空引用异常是 C# 开发者经常遇到的一个问题，它会导致程序崩溃和数据丢失。本文介绍了一种使用 Optional 模式来避免空引用异常的方法，它可以让开发者更优雅地处理可空值，而不需要使用 null 值。
tags:
  - CSharp
---

[TOC]

> 参考资料：
> 
> [https://github.com/zoran-horvat/optional](https://github.com/zoran-horvat/optional) 本文中展示的 Optional 模式的实现完全来自于 zoran horvat 大佬的这个 repo，我添加了如果没有使用 Optional 模式时而是使用 `Nullable` 的代码，您可以在我的 repo 中找到：[https://github.com/Kit086/kit.demos/tree/main/OptionalPattern](https://github.com/Kit086/kit.demos/tree/main/OptionalPattern)；
> 
> [Build Your Own Option Type in C# and Use It Like a Pro - zoran horvat: https://www.youtube.com/watch?v=gpOQl2q0PTU](https://www.youtube.com/watch?v=gpOQl2q0PTU) 这是 zoran horvat 对于如何构建 Option 类型的视频讲解，强烈建议订阅他的 Youtube 频道！

## 0. 前言

我之前写过这篇文章：[C# required：跟 string 的空引用异常说再见：https://cat.aiursoft.cn/post/2023/7/18/say-goodbye-to-string-null-reference-exceptions-with-csharps-required-keyword](https://cat.aiursoft.cn/post/2023/7/18/say-goodbye-to-string-null-reference-exceptions-with-csharps-required-keyword)，来尝试部分地解决 null reference 问题。今天这篇文章是使用 Optional 模式来尝试更加彻底地解决这个问题。

## 1. Null Reference Exception !!!!

写代码这几年，null reference exception 一直是我心里挥之不去的噩梦。不管是进入测试阶段还是修改线上 bug，每次打开日志，十有八九都是满屏的 null reference exception。常规的处理方法是：找到出错的代码位置，加个判断，接着把代码上线，就结束了，危机解除。

|![图 1 - reference meme](assets/2023-07-22-18-31-21.png)|
|:-:|
|<b>图 1 - reference meme</b>|

或许有一天，这种忘记进行 null 检查的“小失误”会给我带来大麻烦。如果我平常就能够写出没有 null reference 的代码，这些危机都不用发生，显然生活会变得更加美好。所以今天来探索一下如何避免 null reference exception。

## 2. `Nullable` 是永远摆脱空引用异常的方法？

我浏览了视频 [这就是永远摆脱空引用异常的方法：https://www.youtube.com/watch?v=v0aB9YCs1oc](https://www.youtube.com/watch?v=v0aB9YCs1oc)，它是由 .NET 官方团队的一个大佬讲述的，这是 GPT 的总结：

> 它介绍了 C# 中新引入的**可空引用类型**特性，它可以帮助开发者避免空引用异常，提高代码的健壮性和可读性。视频通过演示了如何在代码中使用可空引用类型，以及如何在库和框架中注释可空性，来展示这个特性的优势和注意事项。视频还解释了编译器是如何进行流分析和推断可空性的，以及如何处理泛型、接口和虚方法等情况。最后介绍了如何在项目中启用可空引用类型特性，以及一些常见的问题和解决方案。视频的目的是让开发者了解可空引用类型特性的原理和用法，以及如何在自己的项目中应用它，从而减少空引用异常的发生，提升代码质量。视频的长度是 38 分钟 17 秒。

但这个视频是播客性质的，两个人通过聊天的形式来讲，对于英语一般的人包括我来说，真的很难看下去，半天讲不到重点，扯东扯西，看完了也依然不知道“永远摆脱空引用异常的方法”是什么。并不是说它讲得不好，是我菜了。

在我看来，这个视频实际上在告诉我们如何使用当时推出的 C# 的 `Nullable` 特性，也就是我们常见的 `?`，也就是这种形式的代码： `string? firstName = null`。如果您对此有兴趣，可以浏览这篇博客：[https://devblogs.microsoft.com/dotnet/try-out-nullable-reference-types/?WT.mc_id=ondotnet-c9-cxa](https://devblogs.microsoft.com/dotnet/try-out-nullable-reference-types/?WT.mc_id=ondotnet-c9-cxa)

但是引入了 `Nullable` 特性，也就引入了新的问题。从该视频评论就能看得出来：

|![图 2 - 评论](assets/2023-07-22-20-01-48.png)|
|:-:|
|<b>图 2 - 评论</b>|

翻译过来就是： **我情愿让我的代码上线后炸成渣，被老板炒了鱿鱼，去农场种地，也不想再碰到“可能为空引用的返回”这个烦人的玩意儿。**

他至少还能去农场种地，你我有去农场种地的机会吗？

如果你有使用 `Nullable` 特性的经验，你应该会清楚，如果一个地方出现了 `?`，那么很快，它的上层的调用也会出现一堆 `?`，很快整个项目里就会充满了 `?`，`?.` 和 `??`，各种各样的 null check 和 null guard。就像病毒在传播一样，很难受。

## 3. 我们需要什么才能解决因 null 而产生的头痛？

1. 我们需要一个安全地访问可为空的引用的方式，以此来一劳永逸地避免空引用问题，让我们不需要在所有的代码中都添加一大堆 `?`、`?.`、`??` 等符号来确保引用安全；
2. 另外，我认为应该由调用者来决定当结果为 `null` 时该返回什么，这样代码可维护性和可读性都更好。当你有两个高层的方法调用某个底层方法时，对结果为 `null` 时所需要的返回值不同，例如有一个需要返回 `null`，有一个需要返回 `string.Empty`，如果调用方可以直接控制，就不需要写多个底层方法或者使用 `?? string.Empty` 这种写法了，虽然这种写法也还行，可以实现我们的需求；
3. 我希望在可能出现 null reference 异常的地方会直接编译不通过，而不是在 IDE 中的波浪下划线警告。因为很多人是不看警告的，我在很急的时候也常常忽略警告，但这恰恰是 bug 之源。
4. 最后，我希望尽可能减少代码中的 `null`，甚至干掉业务代码中的 `null`。我觉得这样会让我的代码人生更加快乐。

## 4. Optional 模式的实现

我听说 JVM 系列的语言，还有 Rust 等，都使用了 Optional 模式来避免上述的问题。它似乎是来源于函数式编程的一个模式。但 C# 目前还没有内置 Optional 模式的实现，所以我们只能自己写，或者用别的大佬写好的。

[https://github.com/zoran-horvat/optional](https://github.com/zoran-horvat/optional)

上面这个 github repo 是 **Zoran Horvat** 大佬创建的 optional 模式的类和对应的使用示例代码，我们可以在学习完它的用法之后，直接把该 repo 中的 `Option.cs`、`OptionalExtensions.cs`、`ValueOption.cs` 复制到我们的项目中使用。

他在 youtube 上也配有视频，介绍了用法和设计这个类的思路：[Build Your Own Option Type in C# and Use It Like a Pro：https://www.youtube.com/watch?v=gpOQl2q0PTU](https://www.youtube.com/watch?v=gpOQl2q0PTU)

这个仓库包含了使用 C# 实现的 Optional 模式。Optional 模式提供了一种更优雅的方式来处理可空值，避免了使用 null 值。

这个仓库包含了几个实现 Optional 模式的类：

- `Option.cs`：定义了一个泛型结构体 `Option<T>`，其中 `T` 是一个引用类型。这个结构体提供了一些方法，如 `Some`、`None`、`Map`、`MapValue`、`MapOptional`、`MapOptionalValue`、`Reduce`、`Where` 和 `WhereNot`，用于创建和操作 `Option<T>` 类型的值。

- `OptionalExtensions.cs`：定义了一些扩展方法，如 `ToOption`、`Where` 和 `WhereNot`，用于将可空引用类型转换为 `Option<T>` 类型的值。

- `ValueOption.cs`：定义了一个泛型结构体 `ValueOption<T>`，其中 `T` 是一个值类型。这个结构体提供了一些方法，如 `Some`、`None`、`Map`、`MapValue`、`MapOptional`、`MapOptionalValue`、`Reduce`、`Where` 和 `WhereNot`，用于创建和操作 `ValueOption<T>` 类型的值。

与 C# 自带的 `Nullable` 模式相比，Optional 模式提供了更多的方法来操作可空值。例如，可以使用 `Map` 方法来对可空值进行转换，使用 `Reduce` 方法来提供默认值，使用 `Where` 和 `WhereNot` 方法来对可空值进行过滤。这些方法可以链式调用，使得代码更加简洁易读。

此外，该代码仓库还提供了 `Option<T>` 和 `ValueOption<T>` 两种类型，分别用于处理可空引用类型和可空值类型。这样可以避免使用 `Nullable<T>` 类型时需要进行装箱和拆箱操作。

这里展示一下 Zoran Horvat 大佬写的 `Option.cs`：

```csharp
public struct Option<T> : IEquatable<Option<T>> where T : class
{
    private T? _content;

    public static Option<T> Some(T obj) => new() { _content = obj };
    public static Option<T> None() => new();

    public Option<TResult> Map<TResult>(Func<T, TResult> map) where TResult : class =>
        new() { _content = _content is not null ? map(_content) : null };
    public ValueOption<TResult> MapValue<TResult>(Func<T, TResult> map) where TResult : struct =>
        _content is not null ? ValueOption<TResult>.Some(map(_content)) : ValueOption<TResult>.None();

    public Option<TResult> MapOptional<TResult>(Func<T, Option<TResult>> map) where TResult : class =>
        _content is not null ? map(_content) : Option<TResult>.None();
    public ValueOption<TResult> MapOptionalValue<TResult>(Func<T, ValueOption<TResult>> map) where TResult : struct =>
        _content is not null ? map(_content) : ValueOption<TResult>.None();

    public T Reduce(T orElse) => _content ?? orElse;
    public T Reduce(Func<T> orElse) => _content ?? orElse();

    public Option<T> Where(Func<T, bool> predicate) =>
        _content is not null && predicate(_content) ? this : Option<T>.None();

    public Option<T> WhereNot(Func<T, bool> predicate) =>
        _content is not null && !predicate(_content) ? this : Option<T>.None();

    public override int GetHashCode() => _content?.GetHashCode() ?? 0;
    public override bool Equals(object? other) => other is Option<T> option && Equals(option);

    public bool Equals(Option<T> other) =>
        _content is null ? other._content is null
        : _content.Equals(other._content);

    public static bool operator ==(Option<T>? a, Option<T>? b) => a is null ? b is null : a.Equals(b);
    public static bool operator !=(Option<T>? a, Option<T>? b) => !(a == b);
}
```

`OptionalExtensions.cs`：

```csharp
public static class OptionalExtensions
{
    public static Option<T> ToOption<T>(this T? obj) where T : class =>
        obj is null ? Option<T>.None() : Option<T>.Some(obj);

    public static Option<T> Where<T>(this T? obj, Func<T, bool> predicate) where T : class =>
        obj is not null && predicate(obj) ? Option<T>.Some(obj) : Option<T>.None();

    public static Option<T> WhereNot<T>(this T? obj, Func<T, bool> predicate) where T : class =>
        obj is not null && !predicate(obj) ? Option<T>.Some(obj) : Option<T>.None();
}
```

`ValueOption.cs`:

```csharp
public struct ValueOption<T> : IEquatable<ValueOption<T>> where T : struct
{
    private T? _content;

    public static ValueOption<T> Some(T obj) => new() { _content = obj };
    public static ValueOption<T> None() => new();

    public Option<TResult> Map<TResult>(Func<T, TResult> map) where TResult : class =>
        _content.HasValue ? Option<TResult>.Some(map(_content.Value)) : Option<TResult>.None();
    public ValueOption<TResult> MapValue<TResult>(Func<T, TResult> map) where TResult : struct =>
        new() { _content = _content.HasValue ? map(_content.Value) : null };

    public Option<TResult> MapOptional<TResult>(Func<T, Option<TResult>> map) where TResult : class =>
        _content.HasValue ? map(_content.Value) : Option<TResult>.None();
    public ValueOption<TResult> MapOptionalValue<TResult>(Func<T, ValueOption<TResult>> map) where TResult : struct =>
        _content.HasValue ? map(_content.Value) : ValueOption<TResult>.None();

    public T Reduce(T orElse) => _content ?? orElse;
    public T Reduce(Func<T> orElse) => _content ?? orElse();

    public ValueOption<T> Where(Func<T, bool> predicate) =>
        _content.HasValue && predicate(_content.Value) ? this : ValueOption<T>.None();

    public ValueOption<T> WhereNot(Func<T, bool> predicate) =>
        _content.HasValue && !predicate(_content.Value) ? this : ValueOption<T>.None();

    public override int GetHashCode() => _content?.GetHashCode() ?? 0;
    public override bool Equals(object? other) => other is ValueOption<T> option && Equals(option);

    public bool Equals(ValueOption<T> other) =>
        _content.HasValue ? other._content.HasValue && _content.Value.Equals(other._content.Value)
        : !other._content.HasValue;

    public static bool operator ==(ValueOption<T> a, ValueOption<T> b) => a.Equals(b);
    public static bool operator !=(ValueOption<T> a, ValueOption<T> b) => !(a.Equals(b));
}
```

使用了 Option Type 的 Person 和 Book 的类：

```csharp
public class Person
{
    public string FirstName { get; }
    public Option<string> LastName { get; }

    private Person(string firstName, Option<string> lastName) =>
        (FirstName, LastName) = (firstName, lastName);

    public static Person Create(string firstName) =>
        new(firstName, Option<string>.None());

    public static Person Create(string firstName, string lastName) =>
        new(firstName, Option<string>.Some(lastName));

    public override string ToString() =>
        this.LastName
            .Map(lastName => $"{FirstName} {lastName}")
            .Reduce(FirstName);
}

public class Book
{
    public string Title { get; }
    public Option<Person> Author { get; }

    private Book(string title, Option<Person> author) =>
        (Title, Author) = (title, author);

    public static Book Create(string title) =>
        new(title, Option<Person>.None());

    public static Book Create(string title, Person author) =>
        new(title, Option<Person>.Some(author));

    public override string ToString() =>
        Author.Map(author => $"{Title} by {author}").Reduce(Title);
}
```

如果没有 `Option`，使用 `Nullable` 模式的话，`Person` 类的 `public Option<string> LastName { get; }` 属性应该会是 `public string? LastName { get; }`，`Book` 类的 `public Option<Person> Author { get; }` 属性应该会是 `public Person? Author { get; }`。不用我说，您也应该能想到后续对这两个类使用的时候，要加多少 `?`、`?.` 和 `??` 操作符了，可能还会有 `!`。

这是我写的如果没有使用 `Option` 而是使用 `Nullable` 的 `Book` 和 `Person` 类的代码，分别命名为 `NullableBook` 和 `NullablePerson`。这个命名显然会产生歧义，但是为了看起来分明，所以我还是这样命名了：

```csharp
public class NullableBook
{
    public string Title { get; }
    public NullablePerson? Author { get; }

    private NullableBook(string title, NullablePerson? author) =>
        (Title, Author) = (title, author);

    public static NullableBook Create(string title) =>
        new(title, null);

    public static NullableBook Create(string title, NullablePerson author) =>
        new(title, author);

    public override string ToString() =>
        this.Author is not null
            ? $"{Title} by {Author}"
            : Title;
}

public class NullablePerson
{
    public string FirstName { get; }
    public string? LastName { get; }

    private NullablePerson(string firstName, string? lastName) =>
        (FirstName, LastName) = (firstName, lastName);

    public static NullablePerson Create(string firstName) =>
        new(firstName, null);

    public static NullablePerson Create(string firstName, string lastName) =>
        new(firstName, lastName);

    public override string ToString() =>
        !string.IsNullOrWhiteSpace(this.LastName)
            ? $"{FirstName} {LastName}"
            : FirstName;
}
```

使用 `Option` 的示例代码：

```csharp
Person mann = Person.Create("Thomas", "Mann");
Person aristotle = Person.Create("Aristotle");
Person austen = Person.Create("Jane", "Austen");
Person asimov = Person.Create("Isaac", "Asimov");
Person marukami = Person.Create("Haruki", "Murakami");

Book faustus = Book.Create("Doctor Faustus", mann);
Book rhetoric = Book.Create("Rhetoric", aristotle);
Book nights = Book.Create("One Thousand and One Nights");
Book foundation = Book.Create("Foundation", asimov);
Book robots = Book.Create("I, Robot", asimov);
Book pride = Book.Create("Pride and Prejudice", austen);
Book mahabharata = Book.Create("Mahabharata");
Book windup = Book.Create("Windup Bird Chronicle", marukami);

IEnumerable<Book> library = new[] { faustus, rhetoric, nights, foundation, robots, pride, mahabharata, windup };

var bookshelf = library
    .GroupBy(GetAuthorInitial)
    .OrderBy(group => group.Key.Reduce(string.Empty));

foreach (var group in bookshelf)
{
    string header = group.Key.Map(initial => $"[ {initial} ]").Reduce("[   ]");
    foreach (var book in group)
    {
        Console.WriteLine($"{header} -> {book}");
        header = "     ";
    }
}

Console.WriteLine(new string('-', 40));

var authorNameLengths = library
    .GroupBy(GetAuthorNameLength)
    .OrderBy(group => group.Key.Reduce(0));

foreach (var group in authorNameLengths)
{
    string header = group.Key.Map(length => $"[ {length,2} ]").Reduce("[    ]");
    foreach (var book in group)
    {
        Console.WriteLine($"{header} -> {book}");
        header = "      ";
    }
}

ValueOption<int> GetAuthorNameLength(Book book) =>
    book.Author.Map(GetName).MapValue(s => s.Length);

string GetName(Person person) =>
    person.LastName
        .Map(lastName => $"{person.FirstName} {lastName}")
        .Reduce(person.FirstName);

Option<string> GetAuthorInitial(Book book)
{
    return book.Author.MapOptional(GetPersonInitial);
}

Option<string> GetPersonInitial(Person person) =>
    person.LastName
        .MapValue(GetInitial)
        .Reduce(() => GetInitial(person.FirstName));

Option<string> GetInitial(string name) =>
    name.WhereNot(string.IsNullOrWhiteSpace)
        .Map(s => s.TrimStart().Substring(0, 1).ToUpper());
```

如果不使用 Option，那么上面这个例子中的代码应该是这样的：

```csharp
NullablePerson mann = NullablePerson.Create("Thomas", "Mann");
NullablePerson aristotle = NullablePerson.Create("Aristotle");
NullablePerson austen = NullablePerson.Create("Jane", "Austen");
NullablePerson asimov = NullablePerson.Create("Isaac", "Asimov");
NullablePerson marukami = NullablePerson.Create("Haruki", "Murakami");

NullableBook faustus = NullableBook.Create("Doctor Faustus", mann);
NullableBook rhetoric = NullableBook.Create("Rhetoric", aristotle);
NullableBook nights = NullableBook.Create("One Thousand and One Nights");
NullableBook foundation = NullableBook.Create("Foundation", asimov);
NullableBook robots = NullableBook.Create("I, Robot", asimov);
NullableBook pride = NullableBook.Create("Pride and Prejudice", austen);
NullableBook mahabharata = NullableBook.Create("Mahabharata");
NullableBook windup = NullableBook.Create("Windup Bird Chronicle", marukami);

IEnumerable<NullableBook> library = new[] { faustus, rhetoric, nights, foundation, robots, pride, mahabharata, windup };

var author = GetAuthorInitial(rhetoric);

Console.WriteLine(author);

var bookshelf = library
    .GroupBy(GetAuthorInitial)
    .OrderBy(group => group.Key ?? string.Empty);

foreach (var group in bookshelf)
{
    string header = !string.IsNullOrWhiteSpace(group.Key)?  $"[ {group.Key} ]" : "[   ]";
    foreach (var book in group)
    {
        Console.WriteLine($"{header} -> {book}");
        header = "     ";
    }
}

Console.WriteLine(new string('-', 40));

var authorNameLengths = library
    .GroupBy(GetAuthorNameLength)
    .OrderBy(group => group.Key ?? 0);

foreach (var group in authorNameLengths)
{
    string header = group.Key is not null ? $"[ {group.Key,2} ]" : "[    ]";
    foreach (var book in group)
    {
        Console.WriteLine($"{header} -> {book}");
        header = "      ";
    }
}

int? GetAuthorNameLength(NullableBook book) =>
    book.Author is not null
        ? GetName(book.Author).Length
        : null;

string GetName(NullablePerson person) =>
    person.LastName is not null
        ? $"{person.FirstName} {person.LastName}"
        : person.FirstName;

string? GetAuthorInitial(NullableBook book) =>
    book.Author is not null && !string.IsNullOrWhiteSpace(book.Author.LastName)
        ? GetPersonInitial(book.Author)
        : book.Author is not null && !string.IsNullOrWhiteSpace(book.Author.FirstName)
            ? GetPersonInitial(book.Author)
            : null;

string? GetPersonInitial(NullablePerson person) =>
    !string.IsNullOrWhiteSpace(person.LastName)
        ? GetInitial(person.LastName)
        : GetInitial(person.FirstName);

string? GetInitial(string name) =>
    name?.TrimStart()?[..1]?.ToUpper();
```

没有使用 `Option` 模式，而是使用 `Nullable` 的这些代码是我自己添加的，您可以在我的 repo 中找到：[https://github.com/Kit086/kit.demos/tree/main/OptionalPattern](https://github.com/Kit086/kit.demos/tree/main/OptionalPattern)；

## 5. Optional 模式相对于 C# 的 Nullable 特性的优势在哪？

您可以对比 Zoran Horvat 与我的代码，来查看 Optional 模式和 Nullable 模式的区别，来选择您更喜欢的方式。

看起来，Optional 模式导致代码写起来更加复杂了，可读性也并没有变好多少，那它的优点是什么呢？

上一个小节 **4. Optional 模式** 中已经穿插讲过了它的部分优点，这里说一下我体会到的优势：

示例代码中，没有一个 `null`。我们不在方法中传递 `null`，就基本上避免了 null reference 异常了，会很省心，不用每次都检查方法的返回值是否是 `null`，而且每次都担惊受怕，害怕自己是不是又忘了检查 `null` 了。对于 Optional 的对象，当它不存在的时候，根本不会发生调用，也就不用担心调用某个方法会返回 `null` 了。

而且我在 **3. 我们需要什么才能解决因 null 而产生的头痛？** 这一小节中提到的需要解决的问题，Optional 模式也全都解决了！

在我看来，这两种模式都不错，但是 Optional 模式写起来感觉稍微绕一些，可能是因为我并不熟悉函数式编程。但使用 Optional 模式确实能规避我们不小心忘记进行 null check，同时急着上线项目而没有写单元测试集成测试，又忽视了 IDE 的警告，从而导致 null reference exception 的情况。

而使用 `Nullable`，可以看到一堆 null check，例如 `book.Author is not null`，`!string.IsNullOrWhiteSpace(book.Author.LastName)`，还有很多 `?`、`?:`、`?.` 和 `??`，是有一些恼人，影响可读性。

## 6. 总结

`Nullable` 和 Optional 模式，如果让我选择，我可能会根据项目的大小，参与项目的成员等因素来决定使用哪种方法，但它们都是不错的 null reference 的解决方案。