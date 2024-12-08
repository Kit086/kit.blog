---
title: EF Core 动态构建表达式树为所有实体设置软删除的查询过滤器
slug: ef-core-dynamically-building-expression-trees-sets-soft-delete-query-filter-for-all-entities
create_time: 2022-07-31 20:28:00
last_updated: 2022-07-31 21:51:00
description: 本篇博客基于 EF Core 介绍了如何通过动态构建表达式树来为所有实体设置软删除的查询过滤器，还有“茴”字的多种写法。
tags:
  - CSharp
  - EntityFrameworkCore
---

[TOC]

## 0. 简单介绍

如果您还不理解如何构建表达式树，请看一下我的这篇文章：[https://blog.kitlau.dev/posts/how-to-build-csharp-expression-trees/](https://blog.kitlau.dev/posts/how-to-build-csharp-expression-trees/)。

在设计软件的时候，有的软件会有软删除（SoftDelete）的需求，即用户对实体（Entity）进行删除操作时，并不真正从数据库中删除此条数据，而是将这条数据的 `SoftDelete` 字段置为 `true`（此处假定软删除字段名称为 `SoftDelete`，类型为 bool）。这样，该软件依然有恢复用户已删除数据的能力。

我个人非常不喜欢软删除。多了 `SoftDelete` 字段后，实际上每次对实体表的查询都会多一个 `"p"."SoftDelete" == FALSE` 的查询条件，筛选出并未被软删除的数据，使查询效率下降。这老是让人有点难受。但是很多软件确实需要这个功能。

有个程序猿交流群里有朋友问到 EF Core 如何批量给所有实体设置软删除查询过滤器，恰好我也不知道怎么搞，就写代码试了一下，发现 `IMutableEntityType` 有一个 `SetQueryFilter()` 方法，签名如下：

```c#
/// <summary>
///     Sets the LINQ expression filter automatically applied to queries for this entity type.
/// </summary>
/// <param name="queryFilter">The LINQ expression filter.</param>
void SetQueryFilter(LambdaExpression? queryFilter);
```

它接受一个 lambda 表达式参数。这就意味着可以遍历所有实体类型，动态为每一个实体类型动态构建一个 lambda 表达式，作为 `SetQueryFilter()` 的参数，来实现这个功能。刚好我最近在看表达式树相关的知识，就作为一个例子写出来。

## 1. 准备工作

建议直接复制或下载代码，代码地址：[https://github.com/Kit086/kit.demos/tree/main/ExpressionTrees/ComparisonValueObject](https://github.com/Kit086/kit.demos/tree/main/ExpressionTrees/ComparisonValueObject)

如果你的网络打不开代码地址，也可以跟着下面的步骤操作，但不保证所有代码都复制到了本篇博客中，那样篇幅太长，无关的东西太多。

### 创建项目并引入包

1. 创建一个控制台项目，.NET SDK 版本选择 6.0
2. 引入 Microsoft.EntityFrameworkCore.Sqlite 包，版本 6.0.7
3. 引入 Microsoft.EntityFrameworkCore.Design 包，版本 6.0.7

### 创建实体类的基类

创建 BaseEntity.cs：

```c#
public abstract class BaseEntity
{
    public bool SoftDelete { get; set; }
}
```

该类只有一个 `SoftDelete` 属性，bool 类型。所有实体类都将继承该抽象类。

### 创建实体类

创建 Person.cs 和它的配置类 PersonConfiguration：

```c#
public class Person : BaseEntity
{
    public long Id { get; set; }

    public string Name { get; set; } = null!;

    public override string ToString()
    {
        return $"Person: Id: {Id}, Name: {Name}, SoftDelete: {SoftDelete}";
    }
}

public class PersonConfiguration : IEntityTypeConfiguration<Person>
{
    public void Configure(EntityTypeBuilder<Person> builder)
    {
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Name).IsUnicode().HasMaxLength(128).IsRequired();
    }
}
```

创建 Point.cs 和它的配置类 PointConfiguration：

```c#
public class Point : BaseEntity
{
    public long Id { get; set; }
    
    public double X { get; set; }
    
    public double Y { get; set; }

    public override string ToString()
    {
        return $"Point: Id: {Id}, X: {X}, Y:{Y}, SoftDelete: {SoftDelete}";
    }
}

public class PointConfiguration : IEntityTypeConfiguration<Point>
{
    public void Configure(EntityTypeBuilder<Point> builder)
    {
        builder.HasKey(x => x.Id);
    }
}
```

### 创建数据库上下文

AppDbContext：

```c#
public class AppDbContext : DbContext
{
    public DbSet<Person> Persons { get; set; } = null!;
    public DbSet<Point> Points { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=test.db");

        optionsBuilder.LogTo(Console.WriteLine);

        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        base.OnModelCreating(modelBuilder);
    }

    public async Task SeedAsync()
    {
        if (!this.Persons.Any())
        {
            this.Persons.Add(new Person
            {
                Name = "Zhang Three",
                SoftDelete = true
            });
            this.Persons.Add(new Person
            {
                Name = "Li Four",
                SoftDelete = false
            });
            this.Persons.Add(new Person
            {
                Name = "Wang Five",
                SoftDelete = false
            });
        
            await this.SaveChangesAsync();
        }

        if (!this.Points.Any())
        {
            this.Points.Add(new Point
            {
                X = 0.0D,
                Y = 0.0D,
                SoftDelete = true
            });
            this.Points.Add(new Point
            {
                X = 1.1D,
                Y = 1.1D,
                SoftDelete = true
            });
            this.Points.Add(new Point
            {
                X = 2.2D,
                Y = 2.2D,
                SoftDelete = false
            });

            await this.SaveChangesAsync();
        }
    }
}
```

包括一个插入种子数据的方法 `SeedAsync()`。

### 生成数据库

创建完之后，即可添加迁移，然后更新数据库。分别运行以下两条命令即可：

```bash
dotnet ef migrations add Init
dotnet ef database update
```

然后就可以看到创建好的 Sqlite 数据库文件 test.db 了。

## 2. 常规配置 QueryFilter 的方式

首先我们使用常规的配置方式，修改 `PersonConfiguration` 类：

```c#
public class PersonConfiguration : IEntityTypeConfiguration<Person>
{
    public void Configure(EntityTypeBuilder<Person> builder)
    {
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Name).IsUnicode().HasMaxLength(128).IsRequired();

        builder.HasQueryFilter(x => !x.SoftDelete); // <= 新增的代码
    }
}
```

修改 `PointConfiguration` 类：

```c#
public class PointConfiguration : IEntityTypeConfiguration<Point>
{
    public void Configure(EntityTypeBuilder<Point> builder)
    {
        builder.HasKey(x => x.Id);
        
        builder.HasQueryFilter(x => !x.SoftDelete); // <= 新增的代码
    }
}
```

我们分别给两个实体类的配置类设置了查询过滤器（QueryFilter），默认会只查询出 `SoftDelete` 为 `false` 的数据。

编写 Program.cs 代码进行测试：

```c#
await using AppDbContext dbContext = new AppDbContext();
await dbContext.SeedAsync(); // 设置种子数据

List<Person> persons = await dbContext.Persons.ToListAsync();
List<Point> points = await dbContext.Points.ToListAsync();

foreach (Person shanghaiPerson in persons)
{
    Console.WriteLine(shanghaiPerson.ToString());
}

foreach (Point point in points)
{
    Console.WriteLine(point.ToString());
}
```

运行结果：

```bash
......

info: 2022/7/31 20:57:06.350 RelationalEventId.CommandExecuted[20101] (Microsoft.EntityFrameworkCore.Database.Command)
      Executed DbCommand (0ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      SELECT "p"."Id", "p"."SoftDelete", "p"."X", "p"."Y"
      FROM "Points" AS "p"
      WHERE NOT ("p"."SoftDelete")

......

Person: Id: 2, Name: Li Four, SoftDelete: False
Person: Id: 3, Name: Wang Five, SoftDelete: False
Point: Id: 3, X: 2.2, Y:2.2, SoftDelete: False

......
```

翻译出的 SQL 脚本默认多了一个条件 `WHERE NOT ("p"."SoftDelete")`，打印的结果也没问题。

但是现在我们只有两个实体，等项目慢慢膨胀，有 20 个，200 个实体，这样每个实体都配置一遍 QueryFilter 就太麻烦了。

## 3. 动态构建表达式树进行配置

首先删掉第 2 步在 `PersonConfiguration` 和 `PointConfiguration` 类中的配置 QueryFilter 的代码：`builder.HasQueryFilter(x => !x.SoftDelete);`

修改 `AppDbContext` 类的 `OnModelCreating()` 方法：

```c#
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

    // 遍历实体类型
    foreach (var entityType in modelBuilder.Model.GetEntityTypes().AsParallel())
    {
        // 拿到实体类型的 SoftDelete 属性
        var softDeleteProperty = entityType.FindProperty("SoftDelete");
        
        // 如果实体类型有 SoftDelete 属性，且该属性是 bool 类型的
        if (softDeleteProperty is not null && softDeleteProperty.ClrType == typeof(bool))
        {
            // 构建一个 "x => x.SoftDelete == false" lambda 表达式并作为参数传给 entityType.SetQueryFilter()
            
            // 1. 构建 "x" 参数表达式（ParameterExpression）
            var parameterExpr = Expression.Parameter(entityType.ClrType, "x");

            // 2. 构建 "false" 常量表达式（ConstantExpression）
            var constantExpr = Expression.Constant(false, typeof(bool));
            
            // 3. 构建 "x.SoftDelete" 成员表达式（MemberExpress）
            var memberExpression = Expression.PropertyOrField(parameterExpr, softDeleteProperty.PropertyInfo!.Name);
            
            // 4. 组合出 "x.SoftDelete == false" 相等二元运算表达式（BinaryExpression）
            var equalBinaryExpression = Expression.Equal(memberExpression, constantExpr);
            
            // 5. 组合出 "x => x.SoftDelete == false" lambda 表达式（LambdaExpression）
            var filter = Expression.Lambda(equalBinaryExpression, parameterExpr);
            
            // 6. 设置 QueryFilter
            entityType.SetQueryFilter(filter);
        }
    }

    base.OnModelCreating(modelBuilder);
}
```

`foreach` 中的代码都是新加的代码，动态构建表达式树的解析过程已经在注释中写明，看过 [https://blog.kitlau.dev/posts/how-to-build-csharp-expression-trees/](https://blog.kitlau.dev/posts/how-to-build-csharp-expression-trees/) 这篇文章的朋友应该都能够理解。

然后运行 Program.cs，看一下运行结果：

```bash
......

info: 2022/7/31 21:23:42.099 RelationalEventId.CommandExecuted[20101] (Microsoft.EntityFrameworkCore.Database.Command)
      Executed DbCommand (1ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      SELECT "p"."Id", "p"."SoftDelete", "p"."X", "p"."Y"
      FROM "Points" AS "p"
      WHERE NOT ("p"."SoftDelete")

......

Person: Id: 2, Name: Li Four, SoftDelete: False
Person: Id: 3, Name: Wang Five, SoftDelete: False
Point: Id: 3, X: 2.2, Y:2.2, SoftDelete: False

......
```

没有问题，没有被软删除的数据都查出来了。

我们可以在查询时调用 `IgnoreQueryFilters()` 方法来忽略查询过滤器，我们修改 Program.cs：

```c#
// List<Person> persons = await dbContext.Persons.ToListAsync();
// List<Point> points = await dbContext.Points.ToListAsync();

List<Person> persons = await dbContext.Persons.IgnoreQueryFilters().ToListAsync();
List<Point> points = await dbContext.Points.IgnoreQueryFilters().ToListAsync();
```

给两个查询都调用了 `IgnoreQueryFilters()` 方法，看一下运行结果：

```bash
......

info: 2022/7/31 21:32:20.440 RelationalEventId.CommandExecuted[20101] (Microsoft.EntityFrameworkCore.Database.Command)
      Executed DbCommand (1ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      SELECT "p"."Id", "p"."SoftDelete", "p"."X", "p"."Y"
      FROM "Points" AS "p"

......

Person: Id: 1, Name: Zhang Three, SoftDelete: True
Person: Id: 2, Name: Li Four, SoftDelete: False
Person: Id: 3, Name: Wang Five, SoftDelete: False
Point: Id: 1, X: 0, Y:0, SoftDelete: True
Point: Id: 2, X: 1.1, Y:1.1, SoftDelete: True
Point: Id: 3, X: 2.2, Y:2.2, SoftDelete: False

......
```

已经被软删除的数据也查询出来了。没有问题。SQL 脚本也不再有过滤 `SoftDelete` 的筛选条件了。

### 简单的写法

上面的配置代码为了讲清楚构建表达式树的过程，写得略显繁琐，可以简化一下，只用一次的变量就不再单独拿出来了，直接写到使用变量的代码里：

```c#
foreach (var entityType in modelBuilder.Model.GetEntityTypes().AsParallel())
{
    var softDeleteProperty = entityType.FindProperty("SoftDelete");
    
    if (softDeleteProperty is not null && softDeleteProperty.ClrType == typeof(bool))
    {
        var parameterExpr = Expression.Parameter(entityType.ClrType, "x");
        
        var filter =
            Expression.Lambda(
                Expression.Not(Expression.PropertyOrField(parameterExpr, softDeleteProperty.PropertyInfo!.Name)),
                parameterExpr);
        
        entityType.SetQueryFilter(filter);
    }
}
```

代码短了很多，很爽。

原来我们使用 `Expression.Equal()` 组建 `"x.SoftDelete == false"` 相等二元运算表达式，是为了理解起来更简单，这次我们直接使用了 `Expression.Not()` 方法，组建了 `!x.SoftDelete` 表达式，还额外节省了一个用 `Expression.Constant()` 方法构建的 `false` 常量表达式。这样最终构建出的表达式就是 `x => !x.SoftDelete`。

### 究极简单的写法

虽然 `softDeleteProperty` 这个对象在后面被多次使用，但还是能省掉它的声明语句，直接写到 if 里：

```c#
foreach (var entityType in modelBuilder.Model.GetEntityTypes().AsParallel())
{
    if (entityType.FindProperty("SoftDelete") is { } softDeleteProperty && softDeleteProperty.ClrType == typeof(bool))
    {
        var parameterExpr = Expression.Parameter(entityType.ClrType, "x");
        
        var filter =
            Expression.Lambda(
                Expression.Not(Expression.PropertyOrField(parameterExpr, softDeleteProperty.PropertyInfo!.Name)),
                parameterExpr);
        
        entityType.SetQueryFilter(filter);
    }
}
```

就像“茴”字的 4 种写法一样，实际没有多少意义，图个乐。但是 C# 确实是有意思，在语法上就算做了很久开发的工程师也很难全部掌握，而且年年都在出新语法，感觉很有生命力。

## 总结

没什么好总结的，我还准备了一个“动态构建表达式树来简化和语义化领域驱动设计（DDD）中值对象的比较”的例子，后面会写好发出来。