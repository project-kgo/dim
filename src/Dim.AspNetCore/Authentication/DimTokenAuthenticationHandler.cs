using System.Security.Claims;
using System.Text.Encodings.Web;
using Dim.Abstractions.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dim.AspNetCore.Authentication;

public sealed class DimTokenAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IDimTokenValidator tokenValidator) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    private readonly IDimTokenValidator _tokenValidator = tokenValidator;

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var token = GetTokenFromRequest();
        if (string.IsNullOrWhiteSpace(token))
        {
            return AuthenticateResult.NoResult();
        }

        var result = await _tokenValidator.ValidateAsync(token, Context.RequestAborted);
        if (result == null)
        {
            return AuthenticateResult.Fail(new AuthenticationFailureException("Token is not valid"));
        }

        var claims = new List<Claim>
        {
            new(AuthConstants.UserIdClaim, result.UserId),
            new(AuthConstants.PlatformClaim, result.Platform.ToString()),
        };

        var identity = new ClaimsIdentity(claims, AuthConstants.Scheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, AuthConstants.Scheme);

        return AuthenticateResult.Success(ticket);
    }

    private string? GetTokenFromRequest()
    {
        var headerToken = Request.Headers["X-Token"].ToString();
        if (!string.IsNullOrWhiteSpace(headerToken))
        {
            return headerToken;
        }

        var accessToken = Request.Query["access_token"].ToString();
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            return accessToken;
        }

        return null;
    }
}
