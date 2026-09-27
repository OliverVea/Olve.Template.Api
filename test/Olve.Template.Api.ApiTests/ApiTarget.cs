using System.Net.Http.Headers;
using TUnit.Core.Interfaces;

namespace Olve.Template.Api.ApiTests;

/// <summary>
/// The server the API tests talk to. One suite, two targets:
/// <list type="bullet">
///   <item>default: in-process (<see cref="ApiFactory"/>); what <c>dotnet test</c> and the pipeline's
///   <c>check</c> step run.</item>
///   <item>base-URL mode: when <c>API_BASE_URL</c> is set, any running server (a local
///   <c>dotnet run</c>, the Docker image via <c>mise run api:image</c>, live beta via
///   <c>test-after-beta</c>). To mint tokens it needs the server's <c>API_SIGNING_KEY</c>,
///   <c>API_ISSUER</c> and <c>API_AUDIENCE</c>; without them, tests that need a token are skipped.</item>
/// </list>
/// </summary>
public sealed class ApiTarget : IAsyncInitializer, IAsyncDisposable
{
    public const string BaseUrlVariable = "API_BASE_URL";
    public const string SigningKeyVariable = "API_SIGNING_KEY";
    public const string IssuerVariable = "API_ISSUER";
    public const string AudienceVariable = "API_AUDIENCE";

    private readonly Uri? _baseUrl = Environment.GetEnvironmentVariable(BaseUrlVariable) is { Length: > 0 } url ? new Uri(url) : null;
    private readonly Lock _clients = new();
    private ApiFactory? _factory;

    public bool IsInProcess => _baseUrl is null;

    /// <summary>The token settings, or null in base-URL mode when the environment doesn't provide them.</summary>
    public TestTokens? Tokens { get; } = Environment.GetEnvironmentVariable(BaseUrlVariable) is { Length: > 0 }
        ? FromEnvironment()
        : ApiFactory.Tokens;

    public async Task InitializeAsync()
    {
        if (IsInProcess)
        {
            _factory = new ApiFactory();
            _ = _factory.Server; // start now, so host failures surface in setup, not in the first test
            return;
        }

        // Fail fast (and clearly) if nothing is listening.
        using var client = CreateClient();
        using var health = await client.GetAsync("/health");
        health.EnsureSuccessStatusCode();
    }

    /// <remarks>
    /// One at a time: the factory records its clients in a plain list (to dispose them with it), and
    /// tests creating clients in parallel could corrupt it.
    /// </remarks>
    public HttpClient CreateClient()
    {
        if (!IsInProcess)
        {
            return new HttpClient { BaseAddress = _baseUrl };
        }

        lock (_clients)
        {
            return _factory!.CreateClient();
        }
    }

    /// <summary>A client with a valid bearer token; skips the calling test if none can be minted.</summary>
    public HttpClient CreateAuthenticatedClient()
    {
        Skip.When(Tokens is null,
            $"Needs {SigningKeyVariable}, {IssuerVariable} and {AudienceVariable} (the target's Auth settings) to mint a token.");

        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Tokens!.Mint());
        return client;
    }

    public async ValueTask DisposeAsync()
    {
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }
    }

    private static TestTokens? FromEnvironment() =>
        (Environment.GetEnvironmentVariable(SigningKeyVariable),
         Environment.GetEnvironmentVariable(IssuerVariable),
         Environment.GetEnvironmentVariable(AudienceVariable)) is ({ Length: > 0 } key, { Length: > 0 } issuer, { Length: > 0 } audience)
            ? new TestTokens(key, issuer, audience)
            : null;
}
