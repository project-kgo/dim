# Dim

Dim 是一个基于 .NET 10 的可引用 IM Chat 框架骨架，目标是让业务项目通过 NuGet 包或本地项目引用快速接入 IM 能力。

## 项目结构

- `src/Dim.Abstractions`：对外稳定接口、配置模型、扩展点。
- `src/Dim.Contracts`：protobuf 协议定义与生成配置。
- `src/Dim.Domain`：领域模型和值对象。
- `src/Dim.Application`：应用服务与业务编排。
- `src/Dim.Infrastructure`：EF Core、PostgreSQL、Redis、Redis Stream 实现预留。
- `src/Dim.AspNetCore`：ASP.NET Core 宿主集成包。
- `src/Dim.SampleHost`：示例宿主，模拟业务项目接入方式。
- `tests/Dim.UnitTests`：单元测试。
- `tests/Dim.IntegrationTests`：集成测试。

## 快速接入

业务项目引用 `Dim.AspNetCore` 后，在 `Program.cs` 中注册：

```csharp
builder.Services.AddDimChat(builder.Configuration);

var app = builder.Build();

app.MapDimChat();
app.Run();
```

默认会映射：

- `GET /dim/health`
- SignalR Hub：`/dim/hub`

## 本地开发

```bash
dotnet restore
dotnet build
dotnet test
dotnet run --project src/Dim.SampleHost
```

第一阶段只初始化框架骨架，不实现消息收发、会话、在线状态和鉴权等业务功能。
