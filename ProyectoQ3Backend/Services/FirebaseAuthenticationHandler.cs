using System.Security.Claims;
using System.Text.Encodings.Web;
using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace ProyectoQ3Backend.Services;

public class FirebaseAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Bearer";
    private readonly FirebaseService _firebaseService;

    public FirebaseAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        FirebaseService firebaseService)
        : base(options, logger, encoder)
    {
        _firebaseService = firebaseService;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authorization = Request.Headers.Authorization.ToString();

        if (string.IsNullOrWhiteSpace(authorization) ||
            !authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.NoResult();
        }

        var idToken = authorization["Bearer ".Length..].Trim();
        if (string.IsNullOrWhiteSpace(idToken))
        {
            return AuthenticateResult.Fail("El token Bearer está vacío.");
        }

        try
        {
            var firebaseToken = await _firebaseService.Auth.VerifyIdTokenAsync(idToken);
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, firebaseToken.Uid),
                new("user_id", firebaseToken.Uid),
                new("sub", firebaseToken.Uid)
            };

            if (firebaseToken.Claims.TryGetValue("email", out var email) && email is not null)
            {
                claims.Add(new Claim(ClaimTypes.Email, email.ToString()!));
            }

            var profile = await _firebaseService
                .GetCollection("users")
                .Document(firebaseToken.Uid)
                .GetSnapshotAsync();

            if (profile.Exists && profile.TryGetValue<string>("Role", out var role))
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var identity = new ClaimsIdentity(claims, SchemeName);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, SchemeName);

            return AuthenticateResult.Success(ticket);
        }
        catch (FirebaseAuthException exception)
        {
            return AuthenticateResult.Fail($"Token de Firebase inválido: {exception.Message}");
        }
    }
}
