using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.IdentityModel.Tokens;
using Olve.Template.Api.Client;
using Refit;

namespace Olve.Template.Api.IntegrationTests;

public class AppFixture : IAsyncDisposable
{
    private const string SigningKey = "integration-test-signing-key-that-is-long-enough";
    private const string Issuer = "integration-test";
    private const string Audience = "integration-test";

    private readonly WebApplicationFactory<Program> _factory;

    public AppFixture()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("Auth:Authority", Issuer);
                builder.UseSetting("Auth:Audience", Audience);
                builder.UseSetting("Auth:SigningKey", SigningKey);
            });
    }

    public IOlveTemplateApiv1 CreateApiClient()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", GenerateJwt());
        return RestService.For<IOlveTemplateApiv1>(client);
    }

    public HttpClient CreateUnauthenticatedHttpClient() =>
        _factory.CreateClient();

    public ValueTask DisposeAsync() => _factory.DisposeAsync();

    private static string GenerateJwt()
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: [new Claim(ClaimTypes.Name, "test-user")],
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
