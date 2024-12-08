---
title: 解决运行 Docker 容器时的端口不可用错误
slug: docker-error-ports-are-not-available
create_time: 2022-06-06 21:43:00
last_updated: 2022-06-07 10:21:00
description: 运行 Docker 容器时报 500 错误，端口不可用：Error invoking remote method 'docker-start-container' Ports are not available
tags:
  - Docker
---

[TOC]

## 问题

我在本地使用 Docker 运行 MariaDB 容器，作为开发环境的数据库，随用随建随删非常方便。我的开发机器是 Windows11 系统，像 1433 和 3306 这种端口很容易被占用，netstat 又查不到哪个进程占用了这个端口，有时候重启计算机也没用。

报错信息类似 `Error invoking remote method 'docker-start-container' Ports are not available` ，我暂时没复现出来，没截到图。

## 解决办法

需要先关闭 winnat service，运行起容器后再打开。

1. 以**管理员**身份在 PowerShell 中运行以下命令关闭 winnat service：

   ```powershell
   net stop winnat
   ```

2. 运行起之前端口不可用的容器；

3. 以**管理员**身份在 PowerShell 中运行以下命令重新打开 winnat service：

	```powershell
	net start winnat
	```



