---
title: 为什么我的接口，慢得跟蜗牛一样啊？系列文章目录与导读
slug: why-is-my-web-api-so-slow
create_time: 2023-03-14 21:00:00
last_updated: 2023-03-13 21:00:00
description: 本文是“为什么我的接口，慢得跟蜗牛一样啊？”系列文章目录与导读。
tags:
  - dotnet
  - performance
---

[TOC]

## 0. 说明

我的博客会分发到微信订阅号、境外博客网站、境内博客网站三个媒介。

本系列文章由“为什么我的接口，慢得跟蜗牛一样啊？”这个生产环境经常遇到的问题引出，通过多篇文章介绍了分析与处理线上问题的方法。该系列第一部分已经完结。

## 1. 计时和诊断结构化日志记录分析

阅读完第一部分文章，可以了解如何为应用配置完善的日志、计时、请求追踪和诊断信息，将日志记录到 Seq 这种日志中心，轻松进行日志分析。

### 微信公众号地址：

1. [为什么我的接口，慢得跟蜗牛一样啊？- 1. 使用 Serilog 结构化日志：https://mp.weixin.qq.com/s/Qp55_7We9R5t1YVi5UC-6g](https://mp.weixin.qq.com/s/Qp55_7We9R5t1YVi5UC-6g)
2. [为什么我的接口，慢得跟蜗牛一样啊？- 2. Serilog 记录计时和诊断日志：https://mp.weixin.qq.com/s/TGXpV0m1bHFkc_lJjo9G7Q](https://mp.weixin.qq.com/s/TGXpV0m1bHFkc_lJjo9G7Q)
3. [为什么我的接口，慢得跟蜗牛一样啊？- 3. Seq 中心化结构化日志服务：https://mp.weixin.qq.com/s/sMFchhi5miVJz6Q2jEMSMw](https://mp.weixin.qq.com/s/sMFchhi5miVJz6Q2jEMSMw)

### 境内博客地址：

1. [为什么我的接口，慢得跟蜗牛一样啊？- 1. 使用 Serilog 结构化日志：https://cat.aiursoft.cn/post/2023/3/12/why-is-my-web-api-so-slow-1-structured-logging-with-serilog](https://cat.aiursoft.cn/post/2023/3/12/why-is-my-web-api-so-slow-1-structured-logging-with-serilog)
2. [为什么我的接口，慢得跟蜗牛一样啊？- 2. Serilog 记录计时和诊断日志：https://cat.aiursoft.cn/post/2023/3/12/why-is-my-web-api-so-slow-2-timing-and-diagnostic](https://cat.aiursoft.cn/post/2023/3/12/why-is-my-web-api-so-slow-2-timing-and-diagnostic)
3. [为什么我的接口，慢得跟蜗牛一样啊？- 3. Seq 中心化结构化日志服务：https://cat.aiursoft.cn/post/2023/3/13/why-is-my-web-api-so-slow-3-seq-centralized-structured-logs](https://cat.aiursoft.cn/post/2023/3/13/why-is-my-web-api-so-slow-3-seq-centralized-structured-logs)

### 境外博客地址：

1. [为什么我的接口，慢得跟蜗牛一样啊？- 1. 使用 Serilog 结构化日志：https://blog.kitlau.dev/posts/why-is-my-web-api-crawling-at-a-snails-pace-1-structured-logging-with-serilog/](https://blog.kitlau.dev/posts/why-is-my-web-api-crawling-at-a-snails-pace-1-structured-logging-with-serilog/)
2. [为什么我的接口，慢得跟蜗牛一样啊？- 2. Serilog 记录计时和诊断日志：https://blog.kitlau.dev/posts/why-is-my-web-api-crawling-at-a-snails-pace-2-timing-and-diagnostic/](https://blog.kitlau.dev/posts/why-is-my-web-api-crawling-at-a-snails-pace-2-timing-and-diagnostic/)
3. [为什么我的接口，慢得跟蜗牛一样啊？- 3. Seq 中心化结构化日志服务：https://blog.kitlau.dev/posts/why-is-my-web-api-so-slow-3-seq-centralized-structured-logs/](https://blog.kitlau.dev/posts/why-is-my-web-api-so-slow-3-seq-centralized-structured-logs/)

## 2. OpenTelemetry 实现跟踪（traces），度量（metrics）和日志（ logs）

敬请期待。