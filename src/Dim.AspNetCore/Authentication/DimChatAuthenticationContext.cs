using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Dim.AspNetCore.Authentication;

public sealed record DimAuthenticationContext(
    HttpContext? HttpContext,
    ClaimsPrincipal? User,
    string ConnectionId);
