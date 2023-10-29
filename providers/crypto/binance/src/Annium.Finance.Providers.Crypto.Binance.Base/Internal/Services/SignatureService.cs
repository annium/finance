using System;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using Annium.Security;

namespace Annium.Finance.Providers.Crypto.Binance.Base.Internal.Services;

internal class SignatureService
{
    private readonly SecureString _key;
    private readonly SecureString _secret;

    public SignatureService(string apiKey, string apiSecret)
    {
        _key = apiKey.AsSecureString();
        _secret = apiSecret.AsSecureString();
    }

    public ReadOnlySpan<char> GetKey()
    {
        return Encoding.UTF8.GetChars(_key.AsBytes());
    }

    public ReadOnlySpan<char> GetSignature(string data)
    {
        using var hash = new HMACSHA256(_secret.AsBytes());

        return hash.ComputeHash(Encoding.UTF8.GetBytes(data)).ToHexString().ToLowerInvariant();
    }
}
