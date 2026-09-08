using System.Security.Cryptography;
using System.Text;
using Authentication.Configuration;
using Microsoft.Extensions.Options;

namespace Authentication.Services;

public sealed class PasswordResetTokenGenerator(IOptions<EmailOptions> options)
{
    private readonly EmailOptions _options = options.Value;

    public (string RawToken, string TokenHash) Generate()
    {
        Span<byte> buffer = stackalloc byte[32];
        RandomNumberGenerator.Fill(buffer);
        var raw = Convert.ToBase64String(buffer)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        var hash = Hash(raw);
        return (raw, hash);
    }

    public static string Hash(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public string BuildResetLink(string rawToken)
    {
        var baseUrl = _options.PasswordReset.BaseUrl.TrimEnd('/');
        return $"{baseUrl}?token={Uri.EscapeDataString(rawToken)}";
    }

    public TimeSpan TokenLifetime =>
        TimeSpan.FromMinutes(_options.PasswordReset.TokenLifetimeMinutes);
}
