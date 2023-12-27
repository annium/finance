using System.Security;
using System.Security.Cryptography;
using System.Text;
using Annium.Security;

namespace Annium.Finance.Providers.Crypto.Binance.Base.Services;

public sealed class SignatureService
{
    public long ServerTime => _serverTimeWatcher.ServerTime;
    private readonly SecureString _key;
    private readonly SecureString _secret;
    private readonly ServerTimeWatcher _serverTimeWatcher;

    public SignatureService(UserConfigBase config, ServerTimeWatcher serverTimeWatcher)
    {
        _key = config.Key.AsSecureString();
        _secret = config.Secret.AsSecureString();
        _serverTimeWatcher = serverTimeWatcher;
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
