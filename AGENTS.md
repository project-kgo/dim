---
alwaysApply: true
description: 项目规范 (Project Rules)
---
# 项目规范 (Project Rules)

本文档定义了开发规范，旨在确保代码的一致性和可维护性。AI 生成代码时必须严格遵守此规范。

## 最重要的前提

- 思考代码的可维护性、可扩展性、可复用性。
- 避免重复代码，保持代码的 DRY (Don't Repeat Yourself) 原则。
- 如果能抽象出来一个工具或者组件的，就抽象出来一个工具或者组件。
- 严禁写出代码屎山，即代码重复、逻辑复杂、可维护性差的代码。
- 不用过度设计， 大道至简。
- 主动性能优化， 避免资源浪费。

## 项目介绍

- 本项目为 基于.net 10 im chat 框架，用以供其他项目快速搭建 im chat 功能。

## 技术栈

- **语言**: .net 10
- **框架**: ASP.NET Core Minimal APIs , SignalR websocket, protobuf
- **日志框架**: Serilog
- **数据库**: Entity Framework Core (PostgreSQL)
- **缓存**: Redis (StackExchange.Redis)
- **消息队列**: Redis Stream
- **消息序列化**: protobuf
- **api**: api的请求和响应都通过protobuf定义

## 编码规范

- **代码风格**: 尽量使用最新的 .net 10 代码风格规范，语法糖等。
- **代码格式**: 4 个空格缩进
- **注释**: 中文注释
- **代码质量**: 遵循 .net 10 代码质量规范， 遵循官方最佳实践。
- **代码性能**: 注重性能，能零拷贝零分配的则优先使用