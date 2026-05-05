
using Dim.Abstractions.Authentication;

namespace Dim.Application.Authentication;

public sealed record AccessTokenInfo(LoginId LoginId, string AccessToken, DateTime ExpiresAt);
