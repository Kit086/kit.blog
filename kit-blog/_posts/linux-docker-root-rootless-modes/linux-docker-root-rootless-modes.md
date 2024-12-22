---
title: Linux Docker 的 root 和 rootless 模式
slug: linux-docker-root-rootless-modes
create_time: 2024-11-17 21:46:00
last_updated: 2024-11-17 21:46:00
description: "本文从一个 Docker pull 的报错：Error response from daemon: Get \"https://registry-1.docker.io/v2/\": net/http: request canceled while waiting for connection (Client.Timeout exceeded while awaiting headers)，引出了笔者对 Linux 系统中的 root 和 rootless 模式的逐步学习，理解，以及 rootless 模式下的一些配置和使用方法。"
tags:
  - AnduinOS
  - Linux
  - Docker
---

[TOC]

## 0. 为什么会有这篇文章

今天是周日，一觉睡到 10 点，起床玩了一会健身环，吃完午饭之后，就开始了无聊模式。想起工作中遇到的，在极度不稳定的卫星网络条件下的 client-server 数据传输问题，就准备打开电脑思考一下，跟 ChatGPT 请教请教。

本着不浪费精神和时间的原则，我径直打开了 AnduinOS，来准备在日常操作中学习 Linux。毕竟我的 Linux 是半路出家，没有系统学习过就赶鸭子上架了。

我计划使用 docker 的 ubuntu 22.04 镜像跑两到三个容器，来模拟一个 server 和一到二个 client。

首先我打开 AnduinOS 的 Docker 安装文档： [https://docs.anduinos.com/Applications/Development/Docker/Docker.html](https://docs.anduinos.com/Applications/Development/Docker/Docker.html)，来运行了其中的 `Install Docker` 部分和 `Allow current user to run Docker commands without sudo` 部分，因为我知道存在一些恶意 docker 镜像，显然你不会希望他们在你的 root 权限的 docker engine 中运行。

注意，后面我曾经以为是这个安装方式不对，所以我卸载了，然后重新使用 docker 官方文档的 `install-using-the-repository` 方式进行了安装：[https://docs.docker.com/engine/install/ubuntu/#install-using-the-repository](https://docs.docker.com/engine/install/ubuntu/#install-using-the-repository)，但我并没有回退 AnduinOS 文档中的 `Allow current user to run Docker commands without sudo` 部分。在这里记一下，防止我忘记了。

另外，AnduinOS 的文档上也为我踩过了一些坑，例如文档中明确提到不推荐使用 docker desktop 的多个原因，以及

然后我参考网络上的教程配置了镜像（registry mirrors），大部分教程都会教你创建一个 `/etc/docker/daemon.json` 文件，然后写入一堆 registry mirrors 的地址。我照做了，然后我尝试 pull 一些镜像，结果会报这种错误：

```bash
Error response from daemon: Get "https://registry-1.docker.io/v2/": net/http: request canceled while waiting for connection (Client.Timeout exceeded while awaiting headers)
```

我理所应当的认为是网络问题，然后我尝试多次添加、删除、修改配置，重启 docker 服务，修改各个位置的配置，以及运行类似下面的命令为 docker service 创建 drop-in 配置文件等：

```bash
sudo mkdir -p /etc/systemd/system/docker.service.d
sudo nano /etc/systemd/system/docker.service.d/override.conf
```

但是这些尝试均宣告失败。期间我多次询问 ChatGPT 等，但是都没有解决问题。我认为是我始终没问到点子上。

由于我前面已经配置了 `Allow current user to run Docker commands without sudo`，我误以为现在 `docker` 命令和 `sudo docker` 命令是等价的，但实际问题恰恰就出在这里，它们并不是等价的。然后我开始了向 ChatGPT 的连环提问学习，从而把我带进了一个对我来说完全陌生的全新领域：**rootless 模式**。

## 1. 什么是 rootless 模式？

当我偶然重新在 `/etc/docker/daemon.json` 中添加了一大串镜像地址，然后又偶然为 docker pull 命令添加了 sudo 时（`sudo docker pull xxx`），我发现了一个有趣的现象：我可以 pull 镜像了。然后我立刻使用 `docker images` 命令查看我的镜像，却差点惊掉下巴：我 pull 的镜像都不见了。

在反复确认之前的 pull 命令没有报错，而是正确的输出了 image 的 id 后，我开始疑惑，然后大脑飞速运转，想到了以前碰到无数次的“计算机玄学”，想到了天地玄黄，金木水火土等等，然后我想到了一个可能的原因：`sudo`。我迅速运行了 `sudo docker images` 命令，然后我刚刚 pull 的镜像就出现了。

这时候我才意识到 `docker` 和 `sudo docker` 是不同的。知道了现象，我就知道该如何向 ChatGPT 提问了。然后就引出了一堆我之前并不了解的知识。

首先我用 `groups` 命令，确定我当前的用户在 docker 用户组中，用 `ls -l /var/run/docker.sock` 命令检查了 docker 套接字的权限，发现一切均没有问题。我再分别运行 `docker info` 和 `sudo docker info` 命令，发现 sudo 命令输出了我之前配置好的一些 registry mirrors，而无 sudo 的命令没有输出它们。显然我破案了。

ChatGPT 从以下几个方面给了我解释：

### 1. **运行模式**
- **不加 `sudo`：**
  - 使用了 **rootless 模式** (`Security Options: rootless`)。
  - `Docker Root Dir` 是 `/home/kit/.local/share/docker`，这表明 Docker 的存储位置基于你的用户目录，运行的容器和镜像也都是在这个目录下。

- **加 `sudo`：**
  - 使用的是标准 **root 模式**，`Docker Root Dir` 是 `/var/lib/docker`。
  - 容器和镜像的数据存储在全局的系统目录下，受 root 用户管理。

### 2. **镜像和容器的数据分离**
- **不加 `sudo`** 时，你看到的是用户级别的 rootless Docker 实例，没有共享 root 模式下的镜像和容器。
  - 镜像：`Images: 0`（当前用户实例没有镜像）。
  - 容器：`Containers: 0`（当前用户实例没有容器）。

- **加 `sudo`** 时，你看到的是全局的 root 模式实例：
  - 镜像：`Images: 3`。
  - 容器：`Containers: 1`（1 个停止的容器）。

### 3. **安全选项**
- **不加 `sudo`**：
  - 运行的是 rootless 模式。此模式下，Docker 的操作权限受到限制，适合非特权用户，提供更高的安全性，但可能限制某些功能（例如 CPU 和 I/O 限制支持）。
  - 提示了许多未启用的功能（如 `cpu cfs quota`、`io.max` 等），这是 rootless 模式的限制。

- **加 `sudo`**：
  - 运行的是全特权模式。功能更加完整（例如 `apparmor` 被启用），但需要提升到 root 权限。

### 4. **注册表和插件配置**
- 加 `sudo` 时显示了多个配置的注册表镜像（如 `Registry Mirrors`），这些在不加 `sudo` 时的 rootless 模式下并未启用。
- 插件信息一致，表明两种模式下的 Docker CLI 是相同的。

### 5. **为什么会这样？**
- 你当前的配置实际上是在同一系统上同时运行了两套 Docker 实例：
  - 一个是用户级别的 rootless Docker 实例。
  - 一个是全局的 root 模式 Docker 实例。
- rootless Docker 是以你的普通用户权限运行的，它的存储和运行环境与全局的 Docker 引擎互相独立。
- rootless 模式可以独立运行，不需要 root 权限，但由于其限制，无法共享 root 模式的镜像和容器。

显然 rootless 是一个更安全的模式，但是它也有一些限制，例如无法共享 root 模式的镜像和容器，以及无法使用一些功能（例如 CPU 和 I/O 限制支持）。而 root 模式则是一个全特权模式，功能更加完整，但需要提升到 root 权限。

另外，两种模式运行的也是两个 docker，我是可以停掉其中一个的。

考虑到我目前并不需要使用一些高级功能，即使模拟糟糕的卫星网络环境，也是 Linux 系统上的事情，与 Docker 无关，所以我决定继续使用 rootless 模式。我尝试运行以下命令，停止 root 模式：

```bash
sudo systemctl stop docker.service
# 如果只 stop 了 docker.service，docker.socket 仍然在运行，所以要一并停掉
sudo systemctl stop docker.socket

sudo systemctl disable docker.service
sudo systemctl disable docker.socket
```

等我有需求的时候再开启。

## 2. 什么是 `docker.socket`？

- `docker.socket` 是一个 systemd 套接字单元，用于监听 Docker 的 UNIX 套接字 `/var/run/docker.sock`。
- 当有应用尝试连接这个套接字时，`docker.socket` 会激活并启动 `docker.service`。
- 这是一种按需启动服务的机制，可以节省系统资源。

## 3. docker rootless 模式如何配置镜像地址？

首先要解决 registry mirrors 的问题，rootless 的需要配置的 daemon.json 文件通常位于：`~/.config/docker/daemon.json` 位置，如果没有，你可以手动创建，然后把镜像地址写入即可。

配置完之后容器 docker 服务，显然不再使用 `sudo systemctl restart docker` 命令了，而是要使用：

```bash
systemctl --user restart docker
```

这对我来说又是一个新知识点了，我之前并不知道 systemctl 工具还有 `--user` 选项。我对 systemd 的所有了解均来自于 msdn 的 ASP.NET Core 应用部署文档，再加上它名称中有一个 system 字样，我一直以为它是一个只能 sudo 使用的系统级的，root 级的工具。

其它一些点，我放在后面的补充内容中了，但是由于都是 ChatGPT 告诉我的，我目前并未一一验证。

## 4. 还有哪些著名应用有 rootless 模式？

除了前面提到的 docker 和 systemd，还有一些常见的应用有 rootless 模式：
- podman，与 docker 类似的容器引擎
- Caddy，与 Nginx 类似

## 5. systemd 的 rootless 模式

显然 systemd 是一个常用的工具，既然我已经了解到这里了，我当然想进一步了解一下它的 rootless 模式。

以 docker 为例，显然 docker 在 `/etc/systemd/system/` 下会有一个 `docker.service` 文件，而 rootless 模式下，它会在 `~/.config/systemd/user/` 下有一个 `docker.service` 文件。所以 rootless 模式的 systemd 服务文件是在用户级别的 `~/.config/systemd/user/` 下。

如果我们将来想要在 rootless 模式下使用 systemd，我们可以在 `~/.config/systemd/user/` 下创建一个 `my-service.service` 文件，然后使用 `systemctl --user` 的命令来管理它。

另外，两种模式下，docker 的数据存储目录也是不同的，root 模式下是 `/var/lib/docker`，而 rootless 模式下是 `~/.local/share/docker`。


下面是一些补充内容，基本都是我与 ChatGPT 的对话，未经验证的一些知识点，我将会在将来用到的时候回来查阅并验证。

## 补充内容 1. ChatGPT 对 rootless docker 的一些解释

**Rootless Docker** 是一个对 Linux 用户非常重要的功能，尤其是在需要更高安全性和用户隔离的场景下。下面是一个全面的介绍，让你快速了解它的核心概念、优点、限制和适用场景。

### **什么是 Rootless Docker？**

**Rootless Docker** 是 Docker 引擎的一种运行模式，在这种模式下，Docker 不需要以 `root` 权限运行。用户可以在自己的权限范围内运行容器，而无需提升为超级用户。

- **传统 Docker 模式：**
  - Docker 引擎需要 `root` 权限运行。
  - 容器也运行在 `root` 权限下。
  - 虽然通过 `root` 权限增强了功能，但也引入了安全隐患：恶意容器可能影响主机系统。

- **Rootless Docker 模式：**
  - 以普通用户权限运行 Docker 容器。
  - 容器的运行和存储环境完全隔离，不能影响主机系统的全局环境。
  - 容器运行时（如 `runc`）会使用用户命名空间（User Namespace）技术将容器内部的 `root` 用户映射为主机的普通用户。

### **为什么需要 Rootless Docker？**

1. **安全性**：
   - 避免 Docker 容器和主机系统共享 `root` 权限。
   - 降低容器逃逸攻击的风险（即容器中的恶意代码获取主机控制权）。

2. **无需 `root` 权限**：
   - 用户可以在没有系统管理员权限的情况下运行 Docker。
   - 对于多人共享的服务器，普通用户可以独立运行容器。

3. **隔离性**：
   - 每个用户的 Rootless Docker 实例完全独立。
   - 数据存储、网络和配置互不影响。

4. **简化权限管理**：
   - 不需要为每个用户配置 `sudo` 或将用户加入 `docker` 组。

### **Rootless Docker 的工作原理**

1. **用户命名空间 (User Namespace)**：
   - 容器中的 `root` 用户实际上映射到主机上的普通用户。
   - 容器内的其他用户（如 UID 1000）也映射到主机上的非特权用户。

2. **文件系统和存储**：
   - 容器的数据存储在用户目录中（默认路径：`~/.local/share/docker`）。
   - 不依赖于 `/var/lib/docker`，避免与系统级存储冲突。

3. **网络隔离**：
   - Rootless 模式默认使用 `slirp4netns` 或 `VPNKit` 来模拟用户态网络。
   - 不需要修改主机的网络配置（如 `iptables`），避免影响其他用户。

### **Rootless Docker 的优点**

1. **更高的安全性**：
   - 容器即使被攻破，也无法获得主机的 `root` 权限。

2. **更灵活的部署**：
   - 在共享主机或多租户环境下，无需管理员干预即可运行容器。

3. **用户独立性**：
   - 每个用户的 Docker 实例相互独立，互不干扰。

4. **无需复杂配置**：
   - 不需要配置系统级的存储或网络，适合轻量化需求。

### **Rootless Docker 的限制**

1. **性能较低**：
   - 用户态网络模拟（如 `slirp4netns`）性能比系统级网络慢。
   - 文件系统操作可能因为权限转换而略有性能损失。

2. **功能受限**：
   - 不支持一些需要高权限的功能，例如：
     - 启用 `Cgroups`（例如限制 CPU、内存的使用）。
     - 直接访问主机网络（需要配置桥接）。
   - 默认不支持 `iptables` 规则。

3. **兼容性问题**：
   - 某些镜像或工具可能需要额外配置才能在 rootless 模式下运行。

4. **系统要求较高**：
   - 需要 Linux Kernel >= 5.11 才能获得最佳体验。
   - 不同 Linux 发行版对 rootless 的支持可能不同。

### **Rootless Docker 的使用场景**

1. **开发环境**：
   - 开发人员在本地测试容器化应用时，避免因操作失误影响主机系统。
   
2. **共享主机**：
   - 在多人共享的服务器上，提供独立的 Docker 实例，避免冲突。

3. **安全敏感的生产环境**：
   - 对安全性要求极高的场景，容器运行时无需 `root` 权限，减少攻击面。

4. **教育与实验**：
   - 在不需要管理员权限的情况下进行容器实验。

### **如何使用 Rootless Docker？**

1. **检查系统支持**：
   - 确保内核支持用户命名空间：
     ```bash
     zgrep CONFIG_USER_NS /proc/config.gz
     ```
     输出应包含：
     ```
     CONFIG_USER_NS=y
     ```

2. **安装 Docker 并启用 Rootless 模式**：
   ```bash
   dockerd-rootless-setuptool.sh install
   ```

3. **配置环境变量**：
   将以下内容添加到 `~/.bashrc` 或 `~/.zshrc`：
   ```bash
   export PATH=$HOME/bin:$PATH
   export DOCKER_HOST=unix://$XDG_RUNTIME_DIR/docker.sock
   ```

4. **启动 rootless Docker 服务**：
   ```bash
   systemctl --user start docker
   ```

5. **验证 rootless 模式**：
   运行：
   ```bash
   docker info
   ```
   查看 `Security Options` 是否包含 `rootless`。

### **Rootless Docker 的优化建议**

1. **提升网络性能**：
   - 替换 `slirp4netns` 为 `vpnkit`，或使用配置桥接网络。

2. **容器资源管理**：
   - 虽然默认不支持 Cgroups，可以通过启用 `systemd` 用户服务的 `Delegate=yes` 配置部分功能。

3. **共享配置**：
   - 为每个用户配置统一的 `~/.config/docker/daemon.json`，避免重复配置。

4. **监控与调试**：
   - 使用 `docker logs` 和 `docker stats` 监控 rootless 容器的性能。

Rootless Docker 是一个功能强大的工具，尤其适合开发和安全敏感场景。如果你感兴趣，可以尝试在本地实验并探索它的完整功能！

## 补充内容 2. 两种模式下的关键配置文件和目录的对比

**Rootless Docker** 和 **Root Docker** 的配置文件和相关目录确实不一样，它们在运行模式和存储结构上有明显的差异。这种设计是为了确保 Rootless Docker 的独立性和隔离性，同时与 Root Docker 共存不会冲突。

以下是两种模式下的关键配置文件和目录的对比：


### **Root Docker 和 Rootless Docker 配置文件位置对比**

| 配置项                 | Root Docker (需要 `sudo`)                     | Rootless Docker (普通用户)                       |
|:-:|:-:|:-:|
| **Docker 主配置文件**   | `/etc/docker/daemon.json`                  | `~/.config/docker/daemon.json`                |
| **运行时套接字**        | `/var/run/docker.sock`                     | `$XDG_RUNTIME_DIR/docker.sock`                |
| **存储目录**           | `/var/lib/docker`                          | `~/.local/share/docker`                       |
| **日志目录**           | `/var/lib/docker/containers/<container-id>` | `~/.local/share/docker/containers/<container-id>` |
| **系统服务配置**       | `/lib/systemd/system/docker.service`        | `~/.config/systemd/user/docker.service`       |

### **Rootless Docker 的配置文件详解**

1. **`~/.config/docker/daemon.json`**
Rootless 模式的 `daemon.json` 用于配置 Docker 守护进程的行为，与 `/etc/docker/daemon.json` 功能一致。

**常见配置：**
```json
{
  "registry-mirrors": [
    "https://mirror1.example.com",
    "https://mirror2.example.com"
  ],
  "log-driver": "json-file",
  "storage-driver": "overlay2"
}
```

2. **运行时套接字**
Rootless Docker 的套接字位于 `$XDG_RUNTIME_DIR` 下，默认路径：
```bash
/run/user/$(id -u)/docker.sock
```
可以通过以下命令查看：
```bash
echo $XDG_RUNTIME_DIR
```

3. **数据存储目录**
所有容器、镜像和卷的数据存储在：
```bash
~/.local/share/docker
```
这是普通用户目录，避免与系统全局的 `/var/lib/docker` 冲突。

### **与 Root Docker 的共存**

1. 如何优雅管理 Rootless 和 Root Docker？
- **单独配置文件**：
   - Root Docker：编辑 `/etc/docker/daemon.json`。
   - Rootless Docker：编辑 `~/.config/docker/daemon.json`。

- **服务独立运行**：
   - Root Docker 服务：由系统级别的 `systemctl` 管理。
     ```bash
     sudo systemctl start docker
     ```
   - Rootless Docker 服务：由用户级别的 `systemctl` 管理。
     ```bash
     systemctl --user start docker
     ```

- **独立的运行环境**：
   - 两者的网络、镜像和容器数据完全隔离。
   - 使用 `DOCKER_HOST` 环境变量指定要操作的 Docker 实例。
