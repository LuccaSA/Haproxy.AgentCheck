using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Lucca.Infra.Haproxy.AgentCheck.Authentication;

public class BasicSchemeHandler(IOptionsMonitor<BasicSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<BasicSchemeOptions>(options, logger, encoder)
{
    private const string Basic = "Basic";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authenticationHeaderValues = Request.Headers.Authorization
            .Select(h => AuthenticationHeaderValue.Parse(h!))
            .Where(h => h is { Scheme: Basic, Parameter: not null })
            .Select(h => h.Parameter!);

        var expectedParameter = Convert.ToBase64String(Encoding.UTF8.GetBytes(string.Join(':', Options.Username, Options.Password)));
        if (authenticationHeaderValues.Any(authenticationHeaderValue => authenticationHeaderValue.Equals(expectedParameter, StringComparison.OrdinalIgnoreCase)))
        {
            var principal = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, Options.Username) }, Basic));
            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }

        return Task.FromResult(AuthenticateResult.NoResult());
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.Headers.WWWAuthenticate = "Basic realm=Haproxy.AgentCheck";
        return base.HandleChallengeAsync(properties);
    }
}

public class BasicSchemeOptions : AuthenticationSchemeOptions
{
    public string Username { get; set; } = default!;
    public string Password { get; set; } = default!;
}

public class BasicSchemeConfigureOptions(IAuthenticationConfigurationProvider authenticationConfigurationProvider)
    : IConfigureNamedOptions<BasicSchemeOptions>
{
    public void Configure(BasicSchemeOptions options)
    {
        Configure(Options.DefaultName, options);
    }

    public void Configure(string? name, BasicSchemeOptions options)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        var configuration = authenticationConfigurationProvider.GetSchemeConfiguration(name);
        configuration.Bind(options);
    }
}

internal static class BasicAuthenticationExtensions
{
    public static void AddBasic(this AuthenticationBuilder builder, string schemeName)
    {
        builder.AddScheme<BasicSchemeOptions, BasicSchemeHandler>(schemeName, default);
        builder.Services.AddTransient<IConfigureOptions<BasicSchemeOptions>, BasicSchemeConfigureOptions>();
    }
}
