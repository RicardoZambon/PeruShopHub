using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PeruShopHub.Core.Interfaces;
using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.Application.Services;

public class IntegrationService : IIntegrationService
{
    private readonly PeruShopHubDbContext _db;
    private readonly ICacheService _cache;
    private readonly ITokenEncryptionService _tokenEncryption;
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<IntegrationService> _logger;

    private const string OAuthStateCachePrefix = "oauth:state:";
    private static readonly TimeSpan OAuthStateTtl = TimeSpan.FromMinutes(5);

    public IntegrationService(
        PeruShopHubDbContext db,
        ICacheService cache,
        ITokenEncryptionService tokenEncryption,
        IConfiguration configuration,
        IServiceProvider serviceProvider,
        ILogger<IntegrationService> logger)
    {
        _db = db;
        _cache = cache;
        _tokenEncryption = tokenEncryption;
        _configuration = configuration;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<OAuthInitResult> InitiateOAuthAsync(string marketplaceId, CancellationToken ct = default)
    {
        var adapter = ResolveAdapter(marketplaceId);

        // Generate PKCE code_verifier and code_challenge
        var codeVerifier = GenerateCodeVerifier();
        var codeChallenge = ComputeCodeChallenge(codeVerifier);

        // Generate state token for CSRF prevention
        var state = Guid.NewGuid().ToString("N");

        // Store state + code_verifier in Redis with 5 min TTL
        var oauthState = new OAuthStateData(codeVerifier, marketplaceId);
        await _cache.SetAsync($"{OAuthStateCachePrefix}{state}", oauthState, OAuthStateTtl, ct);

        var redirectUri = _configuration[$"Marketplaces:{GetConfigKey(marketplaceId)}:RedirectUri"]
            ?? throw new InvalidOperationException($"RedirectUri not configured for {marketplaceId}");

        var authUrl = adapter.GetAuthorizationUrl(redirectUri, state, codeChallenge);

        _logger.LogInformation("OAuth initiated for marketplace {MarketplaceId}, state={State}", marketplaceId, state);

        return new OAuthInitResult(authUrl);
    }

    public async Task<OAuthCallbackResult> HandleOAuthCallbackAsync(
        string marketplaceId, string code, string state, CancellationToken ct = default)
    {
        // Retrieve and validate state from Redis
        var cacheKey = $"{OAuthStateCachePrefix}{state}";
        var oauthState = await _cache.GetAsync<OAuthStateData>(cacheKey, ct)
            ?? throw new InvalidOperationException("Invalid or expired OAuth state. Please try again.");

        // Remove state from cache (one-time use)
        await _cache.RemoveAsync(cacheKey, ct);

        if (oauthState.MarketplaceId != marketplaceId)
            throw new InvalidOperationException("Marketplace ID mismatch in OAuth state.");

        var adapter = ResolveAdapter(marketplaceId);

        var redirectUri = _configuration[$"Marketplaces:{GetConfigKey(marketplaceId)}:RedirectUri"]
            ?? throw new InvalidOperationException($"RedirectUri not configured for {marketplaceId}");

        // Exchange code for tokens
        var tokenResult = await adapter.ExchangeCodeAsync(code, redirectUri, oauthState.CodeVerifier, ct);

        // Update MarketplaceConnection
        var connection = await _db.MarketplaceConnections
            .FirstOrDefaultAsync(m => m.MarketplaceId == marketplaceId, ct)
            ?? throw new InvalidOperationException($"Marketplace connection '{marketplaceId}' not found.");

        connection.IsConnected = true;
        connection.Status = "Active";
        connection.AccessTokenProtected = _tokenEncryption.Encrypt(tokenResult.AccessToken);
        connection.RefreshTokenProtected = _tokenEncryption.Encrypt(tokenResult.RefreshToken);
        connection.TokenExpiresAt = DateTime.UtcNow.AddSeconds(tokenResult.ExpiresInSeconds);
        connection.ExternalUserId = tokenResult.UserId;
        connection.SellerNickname = tokenResult.Nickname;
        connection.LastSyncAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "OAuth completed for marketplace {MarketplaceId}, user={UserId}, nickname={Nickname}",
            marketplaceId, tokenResult.UserId, tokenResult.Nickname);

        return new OAuthCallbackResult(
            marketplaceId,
            tokenResult.Nickname,
            "Active",
            connection.TokenExpiresAt.Value);
    }

    public async Task DisconnectAsync(string marketplaceId, CancellationToken ct = default)
    {
        var connection = await _db.MarketplaceConnections
            .FirstOrDefaultAsync(m => m.MarketplaceId == marketplaceId, ct)
            ?? throw new InvalidOperationException($"Marketplace connection '{marketplaceId}' not found.");

        connection.IsConnected = false;
        connection.Status = "Disconnected";
        connection.AccessTokenProtected = null;
        connection.RefreshTokenProtected = null;
        connection.TokenExpiresAt = null;
        connection.ExternalUserId = null;
        connection.SellerNickname = null;

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Marketplace {MarketplaceId} disconnected", marketplaceId);
    }

    private IMarketplaceAdapter ResolveAdapter(string marketplaceId)
    {
        return _serviceProvider.GetKeyedService<IMarketplaceAdapter>(marketplaceId)
            ?? throw new InvalidOperationException($"No adapter registered for marketplace '{marketplaceId}'.");
    }

    private static string GetConfigKey(string marketplaceId) => marketplaceId switch
    {
        "mercadolivre" => "MercadoLivre",
        "amazon" => "Amazon",
        "shopee" => "Shopee",
        _ => marketplaceId
    };

    private static string GenerateCodeVerifier()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Base64UrlEncode(bytes);
    }

    private static string ComputeCodeChallenge(string codeVerifier)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.ASCII.GetBytes(codeVerifier));
        return Base64UrlEncode(bytes);
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}

internal record OAuthStateData(string CodeVerifier, string MarketplaceId);
