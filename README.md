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

### SignalR 接入鉴权

业务项目可以实现并注册 `IDimTokenValidator`，鉴权成功后返回 `uid` 和客户端平台：

```csharp
public sealed class AppDimTokenValidator : IDimTokenValidator
{
    public Task<AuthenticationResult?> ValidateAsync(
        string token,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<AuthenticationResult?>(
            new AuthenticationResult("user-id", DimClientPlatform.Web));
    }
}
```

平台固定为 `ios`、`android`、`web`。默认允许多设备登录，同一用户每个平台只保留一个连接；关闭多设备登录后，同一用户只保留一个连接。

Redis 路由使用 `DimChat:Storage:RedisConnectionString` 配置，并通过 `Ku.Utils.Database.Redis.RedisConnectionFactory` 复用连接。未配置 Redis 时应用仍可启动，但 SignalR 连接会被拒绝。

### CAP 消息队列

框架内置集成 `DotNetCore.CAP`：传输使用 Redis Streams，存储使用 PostgreSQL。配置 `DimChat:Storage:PgMasterSqlConnectionString` 与 `DimChat:Storage:RedisConnectionString` 后会自动注册 CAP，业务代码可以直接注入 `ICapPublisher` 发布消息，并使用 `[CapSubscribe]` 订阅。

- `DimChat:Storage:RedisStreamName`：作为 CAP Topic/Redis Stream 名称前缀，默认 `dim:messages`。
- `DimChat:Storage:CapStorageSchema`：CAP PostgreSQL 表结构，默认 `cap`。
- `DimChat:Storage:CapDefaultGroupName`：CAP 默认消费组，默认 `dim`。

## 本地开发

```bash
dotnet restore
dotnet build
dotnet test
dotnet run --project src/Dim.SampleHost
```
