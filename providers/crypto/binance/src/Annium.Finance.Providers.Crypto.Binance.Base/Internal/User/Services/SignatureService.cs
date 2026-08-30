using System.Security;
using System.Security.Cryptography;
using System.Text;
using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Core.Shared.TimeSync;
using Annium.Finance.Providers.Crypto.Binance.Base.User.Services;
using Annium.Security;

namespace Annium.Finance.Providers.Crypto.Binance.Base.Internal.User.Services;

/// <summary>
/// Holds the account's API key and secret in memory as <see cref="SecureString"/>s and computes the HMAC-SHA256
/// signature Binance requires on signed endpoints.
/// </summary>
internal class SignatureService : ISignatureService
{
    /// <summary>Gets the current server time, used as the request <c>timestamp</c> parameter when signing.</summary>
    public long ServerTime => _serverTimeSource.ServerTime;

    /// <summary>The API key, held as a secure string.</summary>
    private readonly SecureString _key;

    /// <summary>The API secret used to compute request signatures, held as a secure string.</summary>
    private readonly SecureString _secret;

    /// <summary>The source of the synchronized server time used as the request timestamp.</summary>
    private readonly IServerTimeSource _serverTimeSource;

    /// <summary>Initializes a new instance of the <see cref="SignatureService"/> class.</summary>
    /// <param name="settings">The user settings providing the API key and secret.</param>
    /// <param name="serverTimeSource">The source of the synchronized server time used as the request timestamp.</param>
    public SignatureService(UserSettings settings, IServerTimeSource serverTimeSource)
    {
        _key = settings.Key.AsSecureString();
        _secret = settings.Secret.AsSecureString();
        _serverTimeSource = serverTimeSource;
    }

    /// <summary>Gets the API key in plain text, for use in the <c>x-mbx-apikey</c> header.</summary>
    /// <returns>The API key.</returns>
    public string GetKey()
    {
        return Encoding.UTF8.GetString(_key.AsBytes());
    }

    /// <summary>Computes the HMAC-SHA256 signature of the given data using the account's API secret.</summary>
    /// <param name="data">The data to sign, typically the request's query string.</param>
    /// <returns>The lowercase hexadecimal signature.</returns>
    public string GetSignature(string data)
    {
        using var hash = new HMACSHA256(_secret.AsBytes());

        return hash.ComputeHash(Encoding.UTF8.GetBytes(data)).ToHexString().ToLowerInvariant();
    }
}
