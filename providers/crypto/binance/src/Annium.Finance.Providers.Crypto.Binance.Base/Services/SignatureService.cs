using System.Security;
using System.Security.Cryptography;
using System.Text;
using Annium.Security;
using NodaTime;

namespace Annium.Finance.Providers.Crypto.Binance.Base.Services;

public class SignatureService
{
    public long ServerTime => SystemClock.Instance.GetCurrentInstant().ToUnixTimeMilliseconds();
    private readonly SecureString _key;
    private readonly SecureString _secret;

    public SignatureService(string apiKey, string apiSecret)
    {
        _key = apiKey.AsSecureString();
        _secret = apiSecret.AsSecureString();
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
