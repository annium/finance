using System.Security;
using System.Security.Cryptography;
using System.Text;
using Annium.Finance.Providers.Abstractions.Domain.Models;
using Annium.Security;

namespace Annium.Finance.Providers.Crypto.Binance.Base.Services;

public sealed class SignatureService
{
    public long ServerTime => _serverTimeProvider.ServerTime;
    private readonly SecureString _key;
    private readonly SecureString _secret;
    private readonly ServerTimeProvider _serverTimeProvider;

    public SignatureService(UserSettings settings, ServerTimeProvider serverTimeProvider)
    {
        _key = settings.Key.AsSecureString();
        _secret = settings.Secret.AsSecureString();
        _serverTimeProvider = serverTimeProvider;
    }

    public string GetKey()
    {
        return Encoding.UTF8.GetString(_key.AsBytes());
    }

    public string GetSignature(string data)
    {
        using var hash = new HMACSHA256(_secret.AsBytes());

        return hash.ComputeHash(Encoding.UTF8.GetBytes(data)).ToHexString().ToLowerInvariant();
    }
}
