---
title: 5 分钟 .NET 单元测试极简入门
slug: five-minute-intro-to-dotnet-unit-testing
create_time: 2023-08-11 20:00:00
last_updated: 2023-08-12 00:00:00
description: 本篇文章介绍了为什么要花时间写单元测试，并使用代码示例介绍如何编写单元测试，5 分钟即可入门！
tags:
  - dotnet
---

[TOC]

## 0. 为什么要花时间写单元测试？

为什么要花时间写单元测试？我直接让测试团队人肉测试，然后直接上生产，有什么问题吗？

1. 省钱
   你没看错，写单元测试能帮公司省钱。如果不写单元测试，你将无法确定你每次对代码的改动是否能够让应用原有的功能正常运行。即使你进行了手动测试，还是无法覆盖所有的情景，从而导致软件交付后，客户可能会发现 bug。所以单元测试实际上能够帮助公司省下很多钱。省下的钱绝对比工程师额外耗费时间写单元测试花掉的时间等价的工资要多得多。
2. 单元测试可以作为文档
   单元测试代码可以被当作文档来阅读。只需要阅读单元测试的 Method 的名字，就能理解被测试的方法在做什么，从而让团队中的每个人，甚至是刚刚加入团队的新人，理解这个应用程序在做什么。
3. 帮助重构应用
   如果日后需要重构应用，覆盖了每一条业务流程的单元测试可以让你知道你在重构过程中有哪些事情忘记了去做。
4. 可以使程序员写出更好的代码
   因为你需要写更好的代码来适应更好的单元测试，就像你学会了“左右互搏”，单元测试让你写更好的代码，更好的代码让你更轻松地写单元测试，让你的代码水平和整个项目的代码质量都有很大的提升。

本文的示例代码没有使用最近有争议的 `Moq` 库，而是使用了 `NSubstitute` 代替。

本文承接上一篇文章：[借 Moq 事件谈一谈单元测试的重要性：https://cat.aiursoft.cn/post/2023/8/10/importance-of-unit-testing-with-moq](https://cat.aiursoft.cn/post/2023/8/10/importance-of-unit-testing-with-moq)，但如果您只想学习如何编写单元测试，则没有必要浏览上一篇文章。

下面我们尝试 **根据实际例子学会单元测试**。

本文示例代码已上传到 Github：[https://github.com/Kit086/kit.demos/tree/main/UnitTesting](https://github.com/Kit086/kit.demos/tree/main/UnitTesting)

## 1. 被测试代码

我准备了一个待测试的例子，它是一个简单的图书管理系统，项目名叫 `BookManager`，包括一个 `Book` 类，一个 `IBookRepository` 接口，一个 `BookService` 类，以及一些方法。

`Book` 类：

```csharp
public sealed class Book
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public DateTime PublishedDate { get; set; }
    public List<string> Genres { get; set; } = new();

    public Book(int id, string title, string author, DateTime publishedDate, List<string> genres)
    {
        Id = id;
        Title = title;
        Author = author;
        PublishedDate = publishedDate;
        Genres = genres;
    }
}
```

`IBookRepository` 接口：

```csharp
public interface IBookRepository
{
    IReadOnlyCollection<Book> GetAllBooks();

    IReadOnlyCollection<Book> GetBooksByAuthor(string author);

    IReadOnlyCollection<Book> GetBooksByPublishedYearRange(int startYear, int endYear);

    Book? GetBookById(int id);

    void AddBook(Book book);

    void UpdateBook(Book book);

    void DeleteBook(int id);
}
```

`BookService` 类：

```csharp
public sealed class BookService
{
    private readonly IBookRepository _bookRepository;

    public BookService(IBookRepository bookRepository)
    {
        _bookRepository = bookRepository;
    }

    public IReadOnlyCollection<Book> GetBooksByAuthor(string author)
    {
        if (string.IsNullOrEmpty(author))
        {
            throw new ArgumentException("Author name cannot be null or empty");
        }

        return _bookRepository.GetBooksByAuthor(author);
    }

    public IReadOnlyCollection<Book> GetBooksByPublishedYearRange(int startYear, int endYear)
    {
        if (startYear < 0 || endYear < 0)
        {
            throw new ArgumentException("Year cannot be negative");
        }

        if (startYear > endYear)
        {
            throw new ArgumentException("Start year cannot be greater than end year");
        }

        return _bookRepository.GetBooksByPublishedYearRange(startYear, endYear);
    }
}
```

我们要测试的就是 `BookService` 类的这两个方法。

## 2. 如何构建测试项目

单元测试是一种验证代码功能正确性的方法，它可以使用一些特定的框架和工具来编写和运行。在这个例子中，我们使用了 `xUnit`、`NSubstitiue` 和 `FluentAssertions` 这 3 个库来进行单元测试。

`xUnit` 是一个流行的 .NET 单元测试框架，它提供了一些特性和约定来编写和组织测试用例。`NSubstitiue` 是一个轻量级的模拟框架（Mock），它可以用来创建和配置模拟对象，以便在测试中替代真实的依赖项。`FluentAssertions` 是一个断言库，它可以用来验证测试结果是否符合预期，它提供了一些易于阅读和表达的断言方法。

为了为 `BookService` 写单元测试，我们需要遵循以下的步骤：

1. 创建一个新的项目，命名为 `BookManager.Tests.Unit`，意为对 `BookManager` 项目的单元测试。该单元测试项目引用 `xUnit`、`NSubstitiue` 和 `FluentAssertions` 这 3 个 Nuget 库，以及 `BookManager` 项目。
2. 创建一个 `BookServiceTests` 类，用 `[Fact]` 特性或 `[Theory]` 特性标记每个测试方法，这两个特性之间的区别我们一会介绍代码时再说。
3. 在这个测试类中，创建一个 `BookService` 的实例，命名为 `_sut`，意为 system under test，被测试的系统，并在该类的构造函数中为它赋值。创建实例时传入一个 `IBookRepository` 的模拟对象作为参数，该模拟对象使用 `NSubstitute` 构建。 **这样就能避免调用真正的 `BookRepository`，从而对数据库产生影响**，也省去了在单元测试项目中配置数据库连接字符串等东西的操作。
4. 使用 `NSubstitiue` 来配置模拟对象的行为，例如返回一些预设的数据或抛出一些异常。
5. 调用 `BookService` 的方法，并使用 `FluentAssertions` 来验证返回值或异常是否符合预期。
6. 运行所有的测试，并检查是否通过。

步骤 1 创建项目和添加引用很简单，我这里不再详述。我们直接进入步骤 2。

## 3. `BookServiceTests` 测试类的基本配置

```csharp
public class BookServiceTests
{
    private readonly IBookRepository _bookRepository = Substitute.For<IBookRepository>(); // 为 IBookRepository 接口创建一个模拟对象

    private readonly BookService _sut; // System Under Test 被测试的系统

    private readonly List<Book> _books = new() // 创建一个用于测试的书籍列表
        {
            new Book(1, "The Lord of the Rings", "J.R.R. Tolkien", new DateTime(1954, 7, 29), new List<string>() { "Fantasy", "Adventure" }),
            new Book(2, "The Hobbit", "J.R.R. Tolkien", new DateTime(1937, 9, 21), new List<string>() { "Fantasy", "Children" }),
            new Book(3, "Harry Potter and the Philosopher's Stone", "J.K. Rowling", new DateTime(1997, 6, 26), new List<string>() { "Fantasy", "Young Adult" }),
            new Book(4, "Nineteen Eighty-Four", "George Orwell", new DateTime(1949, 6, 8), new List<string>() { "Dystopian", "Political" })
        };

    public BookServiceTests()
    {
        _sut = new(_bookRepository);
    }
}
```

这段代码完成了步骤 2 和步骤 3 中 `BookServiceTests` 这个测试类的基本构建：

- 我们使用 `NSubstitute` 构建了一个模拟的 `_bookRepository`；
- 我们构建了要被测试的 `BookService` 对象 `_sut`；
- 我们在测试类的构造函数中把 `_bookRepository` 传给了 `_sut` 做构造函数参数；
- 我们准备了一组书籍 List 做测试数据。

每行代码做了什么，我在代码示例里均提供了注释，请阅读。

## 4. 编写测试方法

### `[Fact]`

首先我们写第一个测试方法：

```csharp
[Fact]
public void GetBooksByAuthor_WithValidAuthorName_ReturnsMatchingBooks()
{
    // Arrange
    var author = "J.R.R. Tolkien"; // 为测试定义作者名 author
    _bookRepository.GetBooksByAuthor(author).Returns(_books.Where(b => b.Author == author).ToList()); // 当调用 _bookRepository.GetBooksByAuthor 且传入参数为 author 时配置模拟对象返回预定义好的书籍列表 _books 中作者为 author 的书籍
    // Act
    var result = _sut.GetBooksByAuthor(author); // 调用参数为 author 的 GetBooksByAuthor 方法，并将返回值赋值给 result 变量
    // Assert
    result.Should().NotBeNull(); // 断言 result 不为 null
    result.Should().HaveCount(2); // 断言 result 包含两本书
    result.Should().OnlyContain(b => b.Author == author); // 断言 result 中的书籍作者都是 author
}
```

这个方法命名为：`GetBooksByAuthor_WithValidAuthorName_ReturnsMatchingBooks`，第一部分表示它测试的是 `GetBooksByAuthor` 方法，第二部分表示它会输入一个有效的 Author Name，第三部分表示它会返回匹配的 Books。

怎么样，是不是看测试方法的名字，就知道被测试的方法的业务功能了？只看这个浅显的例子，你未必能有很大的感受，但是在实际项目中是有用的。

这个方法被 `[Fact]` 标记，表示它是一个测试方法。

这个方法分为 3 部分：Arrange、Act 和 Assert。也就是安排、执行和断言。顾名思义，安排就是准备一些测试数据和设置模拟返回值，执行就是执行 `_sut` 的被测试方法，断言就是根据执行方法拿到的结果，判断是否正确，以决定是否通过测试。详情我都写在代码的注释里了，请详细阅读。

这里就体现出 `NSubstitute` 的功能了。我们使用 `_bookRepository.GetBooksByAuthor(author).Returns(_books.Where(b => b.Author == author).ToList());` 这行代码，定义了当调用 `_bookRepository.GetBooksByAuthor` 且传入参数为 `author` 时配置模拟对象返回预定义好的书籍列表 `_books` 中作者为 `author` 的书籍。

然后，我们使用：

```csharp
result.Should().NotBeNull(); // 断言 result 不为 null
result.Should().HaveCount(2); // 断言 result 包含两本书
result.Should().OnlyContain(b => b.Author == author); // 断言 result 的书籍作者都是 author
```

这三行代码来断言测试的结果。当返回结果为 null 或不包含两本书或包含的书的 Author 属性不等于 author 变量值时，这个测试都会失败。

**插播一条广告：😁鄙人正在寻找新的工作机会，最好是 work-life balance 的工作，base 青岛，有远程工作机会也不错，感兴趣的请通过电子邮件联系我：kit.j.lau@outlook.com，谢谢！**

### `[Theory]`

我们来看一下第二个测试方法：

```csharp
[Theory]
[InlineData(null)]
[InlineData("")]
public void GetBooksByAuthor_WithNullOrEmptyAuthorName_ThrowsArgumentException(string author)
{
    // Act and Assert
    _sut.Invoking(bs => bs.GetBooksByAuthor(author)) // 用 null 或空字符串为参数调用 GetBooksByAuthor 方法
        .Should().Throw<ArgumentException>() // 验证是否抛出 ArgumentException 异常
        .WithMessage("Author name cannot be null or empty"); // 验证异常消息是否正确
}
```

它虽然也测试的是 `GetBooksByAuthor` 方法，但略有不同。它测试的是当 author 参数传入 `null` 或 `""` 时，是否抛出了对应的异常，对应的异常信息是否正确。它使用了 `[Theory]`，表示这个方法会接受多轮输入。然后又使用了两个 `[InlineData]`。这个测试方法会被运行两次，一次的参数 author 会是 `null`，另一次的会是 `""`。很简单吧。

而当传入 `null` 或 `""` 时，都应该抛出 `ArgumentException`，而且异常信息为 `Author name cannot be null or empty`。这个方法就没有 Arrange 了，或者说在公共的 Arrange 中。而且用我们的写法，Act 和 Assert 是写到一起的。

有了这个测试，我们就不再担忧忘记处理 `null` 或空字符串参数的情况了，它会帮你自动测出来！

### 其它例子

以下是对 `BookService` 的另一个方法 `GetBooksByPublishedYearRange` 的单元测试代码。

```csharp
[Fact]
public void GetBooksByPublishedYearRange_WithValidYearRange_ReturnsMatchingBooks()
{
    // Arrange
    var startYear = 1950; // 为测试定义起始年份
    var endYear = 2000; // 为测试定义结束年份
    _bookRepository.GetBooksByPublishedYearRange(startYear, endYear).Returns(_books.Where(b => b.PublishedDate.Year >= startYear && b.PublishedDate.Year <= endYear).ToList()); // 当调用 _bookRepository.GetBooksByPublishedYearRange 且传入参数为 startYear 和 endYear 时配置模拟对象返回预定义好的书籍列表 _books 中出版年份在范围内的书籍
    // Act
    var result = _sut.GetBooksByPublishedYearRange(startYear, endYear); // 调用带有年份范围的 GetBooksByPublishedYearRange 方法
    // Assert
    result.Should().NotBeNull(); // 验证结果不为 null
    result.Should().HaveCount(2); // 验证结果有两本书
    result.Should().OnlyContain(b => b.PublishedDate.Year >= startYear && b.PublishedDate.Year <= endYear); /// 验证结果只包含出版年份在范围内的书籍
}

[Theory]
[InlineData(-1, 2000)]
[InlineData(-100, -50)]
public void GetBooksByPublishedYearRange_WithNegativeStartYear_ThrowsArgumentException(int startYear, int endYear)
{
    // Act and Assert
    _sut.Invoking(bs => bs.GetBooksByPublishedYearRange(startYear, endYear)) // 用负起始年份调用 GetBooksByPublishedYearRange 方法
        .Should().Throw<ArgumentException>() // 验证是否抛出 ArgumentException 异常
        .WithMessage("Year cannot be negative"); // 验证异常消息是否正确
}

[Theory]
[InlineData(1950, -1)]
[InlineData(-50, -100)]
public void GetBooksByPublishedYearRange_WithNegativeEndYear_ThrowsArgumentException(int startYear, int endYear)
{
    // Act and Assert
    _sut.Invoking(bs => bs.GetBooksByPublishedYearRange(startYear, endYear)) // 用负结束年份调用 GetBooksByPublishedYearRange 方法
        .Should().Throw<ArgumentException>() // 验证是否抛出 ArgumentException 异常
        .WithMessage("Year cannot be negative"); // 验证异常消息是否正确
}

[Theory]
[InlineData(2000, 1950)]
[InlineData(2023, 2022)]
public void GetBooksByPublishedYearRange_WithStartYearGreaterThanEndYear_ThrowsArgumentException(int startYear, int endYear)
{
    // Act and Assert
    _sut.Invoking(bs => bs.GetBooksByPublishedYearRange(startYear, endYear)) // 用起始年份大于结束年份调用 GetBooksByPublishedYearRange 方法
        .Should().Throw<ArgumentException>() // 验证是否抛出 ArgumentException 异常
        .WithMessage("Start year cannot be greater than end year"); // 验证异常消息是否正确
}

[Theory]
[InlineData(1900, 1920)]
[InlineData(2020, 2030)]
public void GetBooksByPublishedYearRange_WithNoMatchingBooks_ReturnsEmptyList(int startYear, int endYear)
{
    // Arrange
    _bookRepository.GetBooksByPublishedYearRange(Arg.Any<int>(), Arg.Any<int>()).Returns(Enumerable.Empty<Book>().ToList()); // 当调用 _bookRepository.GetAllBooks 且传入任意 int 类型参数时配置模拟对象返回预定义的书籍列表 _books
    // Act
    var result = _sut.GetBooksByPublishedYearRange(startYear, endYear); // 调用年份范围不匹配任何书籍的 GetBooksByPublishedYearRange 方法
    // Assert
    result.Should().NotBeNull(); // 验证结果不为 null
    result.Should().BeEmpty(); // 验证结果为空
}
```

如果你看懂了上一部分我的解释，以及这些注释，那么这里我就只有一个东西需要说一下了。`NSubstitute` 允许我们使用 `Arg.Any<T>()` 这种参数来表示“任意 `T` 类型的数据”。

## 5. 运行单元测试

除了在您的 IDE 中直接运行之外，您还可以运行 `dotnet test` 命令来运行单元测试：

```powershell
~\Documents\GitHub\kit.demos\UnitTesting git:main ❯❯❯ dotnet test
  Determining projects to restore...
  Restored C:\Users\kit\Documents\GitHub\kit.demos\UnitTesting\BookManager\BookManager.csproj (in 536 ms).
  Restored C:\Users\kit\Documents\GitHub\kit.demos\UnitTesting\BookManager.Tests.Unit\BookManager.Tests.Unit.csproj (in 602 ms).

.....

Starting test execution, please wait...
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:    12, Skipped:     0, Total:    12, Duration: 86 ms - BookManager.Tests.Unit.dll (net7.0)
```

可以看到运行了 12 次测试，全部通过。

如果您想单独运行某个测试方法或者调试某个测试方法，请根据您使用的 IDE 中的提示来进行，一般都是在该方法签名的这行代码的左侧有一个绿色的三角按钮。或者右键它选择 run test 或 debug test。

但在 IDE 中运行，看测试结果会比较直观。

还有生成测试报告文件等功能，但这些都不属于入门内容，所以本篇教程不涉及。

## 6. 总结

因为这只是一个 5 分钟的入门教程，所以不再展开讲，剩下的您只需要去这几个库的官方文档按照您的需要来查询即可。编写单元测试的核心内容我都已经讲完了。

`xUnit` 还有一些高端功能，在单元测试中用得不多，但集成测试中很有用。如果我后面还有时间的话，可以考虑写集成测试的入门介绍。

**最后来一条广告：😁鄙人正在寻找新的工作机会，最好是 work-life balance 的工作，base 青岛，有远程工作机会也不错，感兴趣的请通过电子邮件联系我：kit.j.lau@outlook.com，谢谢！**