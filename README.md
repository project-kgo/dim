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

业务项目需要实现并注册 `IDimChatAuthenticator`，鉴权成功后返回 `uid` 和客户端平台：

```csharp
public sealed class AppDimChatAuthenticator : IDimChatAuthenticator
{
    public ValueTask<DimChatAuthenticationResult?> AuthenticateAsync(
        DimChatAuthenticationContext context,
        CancellationToken cancellationToken)
    {
        return ValueTask.FromResult<DimChatAuthenticationResult?>(
            new DimChatAuthenticationResult("user-id", DimClientPlatform.Web));
    }
}
```

平台固定为 `ios`、`android`、`web`。默认允许多设备登录，同一用户每个平台只保留一个连接；关闭多设备登录后，同一用户只保留一个连接。

Redis 路由使用 `DimChat:Storage:RedisConnectionString` 配置，并通过 `Ku.Utils.Database.Redis.RedisConnectionFactory` 复用连接。未配置 Redis 时应用仍可启动，但 SignalR 连接会被拒绝。

## 本地开发

```bash
dotnet restore
dotnet build
dotnet test
dotnet run --project src/Dim.SampleHost
```
