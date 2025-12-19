using System.Security;
using System.Security.Cryptography;
using System.Text;
using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Core.Shared.TimeSync;
using Annium.Security;

namespace Annium.Finance.Providers.Crypto.Binance.Base.User.Services;

public sealed class SignatureService
{
    public long ServerTime => _serverTimeSource.ServerTime;
    private readonly SecureString _key;
    private readonly SecureString _secret;
    private readonly IServerTimeSource _serverTimeSource;

    public SignatureService(UserSettings settings, IServerTimeSource serverTimeSource)
    {
        _key = settings.Key.AsSecureString();
        _secret = settings.Secret.AsSecureString();
        _serverTimeSource = serverTimeSource;
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
