---
title: 【译】.NET SDK 将会内置容器支持，不再需要 Dockerfile
slug: announcing-built-in-container-support-for-the-dotnet-sdk
create_time: 2022-08-28 18:00:00
last_updated: 2022-08-28 19:14:00
description: 本文介绍了 dotnet 7 的内置容器支持功能。
tags:
  - CSharp
  - dotnet
---

[TOC]

## 0. 为什么会翻译这篇文章

原文地址：https://devblogs.microsoft.com/dotnet/announcing-builtin-container-support-for-the-dotnet-sdk/

这篇文章在 25 日发布之后，在网上被转发爆了，但是看国内的开发者们很多还没有注意到它，所以翻译一下。首先机翻，然后我通读机翻结果后修改了一些人类看不懂的地方。我在微信平台上发布的这篇文章可能很不要脸的标注了原创，这波是既白嫖 dotnet 团队又白嫖翻译软件，将白嫖进行到底😁。

原文作者在文中插入了大量链接，如果微信订阅号无法看到这些链接，请移步我的博客：[https://blog.kitlau.dev/posts/announcing-built-in-container-support-for-the-dotnet-sdk/](https://blog.kitlau.dev/posts/announcing-built-in-container-support-for-the-dotnet-sdk/)。

下面开始都是本文的翻译。

## 1. 前言

容器已成为在云中分发和运行应用程序和服务套件的最简单方法之一。多年前，dotnet 运行时[已针对容器进行了强化](https://devblogs.microsoft.com/dotnet/announcing-net-core-3-0/#docker-and-cgroup-limits)。现在，您只需使用 `dotnet publish`。 容器镜像现在是 dotnet SDK 支持的输出类型。

## 2. TL;DR（Too long; Didn't read - 太长，不看）

在我们详细介绍它的工作原理之前，我想展示一个从零开始到容器化的 ASP.NET Core 应用程序的样子：

```PowerShell
# 创建一个新的项目，并且 cd 到它的目录下
dotnet new mvc -n my-awesome-container-app
cd my-awesome-container-app

# 引用一个能够创建容器的 NuGet 包
dotnet add package Microsoft.NET.Build.Containers

# 发布你的项目（linux-64）
dotnet publish --os linux --arch x64 -p:PublishProfile=DefaultContainer

# 用新的容器运行你的项目
docker run -it --rm -p 5010:80 my-awesome-container-app:1.0.0
```

现在你可以访问 http://localhost:5010，你应该看到示例 MVC 应用程序，它的所有荣耀呈现。

> 对于此版本，您必须安装并运行 Docker 才能使此示例正常工作。此外，仅支持 Linux-x64 容器。

## 3. Motivation（动机）

容器是打包和发布应用程序的绝佳方式。构建容器镜像的一种流行方式是通过 Dockerfile，一个描述如何创建和配置容器镜像的特殊文件。让我们看一下 [ASP.NET Core 应用程序的 Dockerfile](https://github.com/dotnet/dotnet-docker/blob/1ea6550437f5e6f5150792444bc26aa25e9773da/samples/aspnetapp/Dockerfile) 示例之一：

```Dockerfile
# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /source

# copy csproj and restore as distinct layers
COPY aspnetapp/*.csproj .
RUN dotnet restore --use-current-runtime  

# copy everything else and build app
COPY aspnetapp/. .
RUN dotnet publish -c Release -o /app --use-current-runtime --self-contained false --no-restore

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "aspnetapp.dll"]
```

该项目使用[多阶段构建](https://docs.docker.com/develop/develop-images/multistage-build/)来构建和运行应用程序。它使用 7.0 SDK docker 镜像来 restore 应用程序的依赖项，将应用程序发布到文件夹，最后从该目录创建最终的运行时映像。

这个 Dockerfile 工作得很好，但是有一些注意事项不够明显，它们来自 Docker [构建上下文](https://docs.docker.com/engine/reference/commandline/build/#description)的概念。构建上下文是可在 Dockerfile 中访问的一组文件，并且通常（但不总是）与 Dockerfile 位于同一目录。如果您的项目文件旁边有一个 Dockerfile，但您的项目文件位于解决方案根目录下，那么您的 Docker 构建上下文很容易不包含像 Directory.Packages.props 或 NuGet.config 这种配置文件。但是正常的情况下，这些配置文件会在正常的 `dotnet build` 命令中被使用。对于任何分层配置模型，例如 EditorConfig 或存储库本地 git 配置，您都会遇到相同的情况。

明确定义的 Docker 构建上下文和 dotnet build 过程之间的这种不匹配是驱动此功能诞生的因素之一。构建镜像所需的所有信息都存在于一个标准 `dotnet build` 中，我们只需要找出正确的方法，以像 Docker 这样的容器运行时可以使用的方式来表示这些数据。

### 它是如何工作的

容器镜像由两个主要部分组成：
1. 一些 JSON 配置，其中包含有关如何运行镜像的元数据
2. 代表文件系统的原始码档案列表

在 dotnet 7 中，我们向 dotnet 运行时添加了几个 API 来[处理 TAR 文件和流](https://devblogs.microsoft.com/dotnet/announcing-dotnet-7-preview-4/#added-new-tar-apis)，这为以编程方式操作容器映像打开了大门。

这种方法已经在 J​​ava 生态系统中的 [Jib](https://github.com/GoogleContainerTools/jib) 、Go 生态系统中的 [Ko](https://github.com/google/ko) 甚至在 dotnet 中的 [konet](https://github.com/lippertmarkus/konet) 等项目中都得到了很好的证明。很明显，一种简单的工具驱动的方法来生成容器镜像正变得越来越流行。

我们基于以下目标构建了 dotnet SDK 解决方案：

1. 提供与现有构建逻辑的无缝集成，以防止前面提到的各种上下文的差距
2. 用 C# 实现，以利用我们自己的工具并从 dotnet 运行时性能改进中受益
3. 集成到 dotnet SDK 中，使 dotnet 团队可以直接在现有的 dotnet SDK 流程中改进和服务

## 4. 自定义

那些有编写 Dockerfile 经验的人可能想知道 Dockerfile 的所有复杂的东西都到哪里去了。`FROM` 基础镜像在哪里声明？使用什么 tag？您可以通过使用 MSBuild properties 和 items 来自定义生成的镜像的许多方面（您可以在[文档](https://aka.ms/dotnet/containers/customization)中详细了解这些内容），但我们提供了一些默认值以使更容易入门。

### Base 镜像

Base 镜像定义了您将拥有的功能以及您将使用的操作系统和版本。自动选择以下 Base 镜像：

- 对于 ASP.NET Core 应用程序，`mcr.microsoft.com/dotnet/aspnet`
- 对于独立的应用程序，`mcr.microsoft.com/dotnet/runtime-deps`
- 对于所有其他应用程序，`mcr.microsoft.com/dotnet/runtime`

在所有情况下，用于 Base 镜像的 tag 是已为应用程序选择的 TargetFramework 的 version。例如，`net7.0` 的 TargetFramework 会使用 `7.0` 作为 tag。 

这些简单版本标签基于 linux 的 Debian 发行版——如果你想使用不同的支持发行版，如 Alpine 或 Ubuntu，那么你需要手动指定该 tag（参见下面的 `ContainerBaseImage` 示例）。 

我们认为选择默认为基于 Debian 的运行时镜像版本是广泛兼容大多数应用程序需求的最佳选择。当然，您可以为您的容器自由选择任何 Base 镜像。只需将 `ContainerBaseImage` 属性设置为任何图像名称，剩下的就交给我们了。例如，也许您想在 Alpine Linux 上运行您的 Web 应用程序。看起来像这样：

```xml
<ContainerBaseImage>mcr.microsoft.com/dotnet/aspnet:7.0-alpine</ContainerBaseImage>
```

### 镜像 version 和 name

默认情况下，您的镜像名称将基于您的项目的 `AssemblyName`。如果您觉得不妥，可以使用 `ContainerImageName` 属性显式设置图像的名称。

镜像也需要一个 tag，所以我们默认使用 `Version` 属性的值。默认值是 `1.0.0`，但您可以以任何方式自由自定义它。这特别适合像 [GitVersioning](https://github.com/dotnet/Nerdbank.GitVersioning)这样的自动版本控制方案，它的 version 来自代码仓库中的一些配置数据。这有助于确保您始终拥有唯一 version 的应用程序来运行。唯一的版本 tag 对于镜像尤其重要，因为将具有相同 tag 的镜像推送到注册表会覆盖之前存储为该标签的镜像。

### 其它的一切

可以在镜像上设置许多其他属性，例如自定义 Entrypoints、环境变量，甚至是任意 Labels（通常用于跟踪元数据，如 SDK 版本，或谁构建了图像）。此外，通常通过指定不同的 base registry 将图像推送到 destination registries。在 dotnet SDK 的后续版本中，我们计划以与上述类似的方式添加对这些功能的支持——所有这些都通过 MSBuild 项目属性进行控制。

## 5. 你什么时候应该使用它

### 本地开发

如果您需要一个用于本地开发的容器，现在您只需一个命令即可：

```PowerShell
dotnet publish --os linux --arch x64 -p:PublishProfile=DefaultContainer
```

将制作一个以您的项目命名的使用 debug 配置的容器镜像。一些用户通过将这些属性放入 Directory.Build.props 使这变得更加简单，更容易运行 `dotnet publish`：

```xml
<Project>
    <PropertyGroup>
        <RuntimeIdentifier>linux-x64</RuntimeIdentifier>
        <PublishProfile>DefaultContainer<PublishProfile>
    </PropertyGroup>
</Project>
```

您还可以使用其他 SDK 和 MSBuild 功能（如响应文件或 PublishProfiles）来创建这些属性 group 以便于使用。

### CI 管道

总体而言，使用 SDK 构建容器镜像应该无缝集成到您现有的构建过程中。一个用于容器化应用程序的最小 GitHub Actions 工作流示例只有大约 30 行配置：

```yml
name: Containerize ASP.NET Core application

on: [push]

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - name: Setup .NET SDK
        uses: actions/setup-dotnet@v2
      # Package the app into a linux-x64 container based on the dotnet/aspnet image
      - name: Publish
        run: dotnet publish --os linux --arch x64 --configuration Release -p:PublishProfile=DefaultContainer
      # Because we don't yet support pushing to authenticated registries, we have to use docker to
      # login, tag and push the image. In the future none of these steps will be required!
      # 1. Login to our registry so we can push the image. Could use a raw docker command as well.
      - name: Docker Login
        uses: actions-hub/docker/login@master
        env:
          DOCKER_REGISTRY_URL: sdkcontainerdemo.azurecr.io
          DOCKER_USERNAME: ${{ secrets.ACR_USERNAME }}
          DOCKER_PASSWORD: ${{ secrets.ACR_PAT }}
      # 2. Use the tag command to rename the local container to match our Azure Container Registry URL
      - name: Tag built container with Azure Container Registry url
        uses: actions-hub/docker/cli@master
        with:
          args: tag sdk-container-demo:1.0.0 sdkcontainerdemo.azurecr.io/baronfel/sdk-container-demo:latest
      # 3. Push the renamed container to ACR.
      - name: Push built container to Azure Container Registry
        uses: actions-hub/docker/cli@master
        with:
          args: push sdkcontainerdemo.azurecr.io/baronfel/sdk-container-demo:latest
```

这是一个名为 [baronfel/sdk-container-demo](https://github.com/baronfel/sdk-container-demo) 的示例 repo 的一部分，用于演示这个端到端过程。

但是，有一种主要情况是我们完全无法使用 SDK 构建的容器来处理的。

### RUN 命令

无法使用 dotnet SDK 执行 RUN 命令。这些命令通常用于安装一些操作系统包或创建一个新的操作系统用户，或任意数量的任意事情。如果您想继续使用 SDK 容器构建功能，您可以改为创建具有这些变更的自定义 base image，然后使用它作为前面提到的 `ContainerBaseImage`

例如，假设我正在开发一个库，该库需要在主机上安装 libxml2 库才能进行一些自定义 XML 处理。我可以编写一个像这样的 Dockerfile 来捕获这个原生依赖：

```Dockerfile
FROM mcr.microsoft.com/dotnet/runtime:7.0

RUN apt-get update && \
        apt-get install -y libxml2 && \
        rm -rf /var/lib/apt/lists/*
```

现在，我可以 build，tag，并将此镜像推送到我的 registry，并将其用作我的应用程序的 base：

```PowerShell
docker build -f Dockerfile . -t registry.contoso.com/my-base-image:1.0.0
docker push registry.contoso.com/my-base-image:1.0.0
```

在我的应用程序的项目文件（.csproj）中，我将 `ContainerBaseImage` 属性设置为这个新镜像：

```xml
<Project>
  <PropertyGroup>
    ...
    <ContainerBaseImage>registry.contoso.com/my-base-image:1.0.0</ContainerBaseImage>
    ...
  </PropertyGroup>
</Project>
```

## 6. 暂时的缺点

这是 SDK 构建的容器镜像的初始预览，因此我们还无法提供一些功能。

### Windows 镜像和非 x64 架构

我们专注于此初始版本的 Linux-x64 镜像部署方案。Windows 镜像和其他架构是我们计划支持完整版本的关键场景（也就是说当前尚未支持，将来会支持），我们可以拭目以待。

### 推送到远程 registries

我们尚未实现对身份验证的支持，这对许多用户来说至关重要。这是我们最高优先级的项目之一。在此期间，我们建议推送到您的本地 Docker deamon，然后使用 `docker tag` 和 `docker push` 将生成的镜像推送到您的目标仓库。上面的示例 GitHub Action 展示了如何做到这一点。

### 一些镜像定制

一些镜像元数据自定义尚未实现。这包括自定义 Entrypoints、环境变量和自定义 user/group 信息。完整列表可以在 [dotnet/sdk-container-builds](https://aka.ms/dotnet/containers/customization#unsupported-properties) 上看到。

## 7. 下一步

在 dotnet 7 版本的 rc 阶段，我们将添加新的镜像元数据，支持将镜像推送到远程 registries，并支持 Windows 镜像。您可以在我们为此创建的 [milestone](https://github.com/dotnet/sdk-container-builds/issues?q=is%3Aopen+is%3Aissue+milestone%3A7.0.100-rc1) 上跟踪该进度。

我们还计划在整个发布过程中将这项工作直接集成到 SDK 中。这个功能实现后，我们将在 NuGet 上发布包的“最终”版本，该版本会警告您变更，并要求您从项目中完全删除这个包。

如果您对该工具的前沿构建感兴趣，我们还有一个 [GitHub 包提供](https://github.com/dotnet/sdk-container-builds/packages/1588543)，您可以在其中选择尝试最新的特性。

如果您在使用 SDK 容器镜像生成器时遇到问题，请 [告诉我们](https://github.com/dotnet/sdk-container-builds/issues/new)，我们将尽最大努力帮助您。

## 8. 结束的想法

我们希望那些构建 Linux 容器的人尝试使用 dotnet SDK 构建它们。我个人在本地尝试它们时非常开心——我很开心地访问了我的一些演示存储库并使用一个命令将它们容器化，我希望你们都有同样的感受！

快乐的构建镜像！

## 我的总结

读完这篇文章之后，我简单总结一下。

RUN 命令看起来应该是暂时不会提供支持了，将来可能提供支持的功能都写在了（6. 暂时的缺点）中，dotnet 7 正式发布时会支持的功能写在了（7. 下一步）中。

可能有的朋友觉得这个功能很鸡肋，因为 Dockerfile 的使用也并非难事，而且功能更强大。我刚读完时也有这个想法，但这篇文章开头解释了为什么要实现这么个功能，以及它相对于 Dockerfile 的优势。随着这个功能的不断开发，应该会越来越完善。
