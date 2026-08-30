namespace Annium.Finance.Providers.Crypto.Binance.Base.User.Services;

/// <summary>Signs requests to Binance's SIGNED and USER_DATA endpoints with the account's API key and secret.</summary>
public interface ISignatureService
{
    /// <summary>Gets the current server time, used as the request <c>timestamp</c> parameter when signing.</summary>
    long ServerTime { get; }

    /// <summary>Gets the API key in plain text, for use in the <c>x-mbx-apikey</c> header.</summary>
    /// <returns>The API key.</returns>
    string GetKey();

    /// <summary>Computes the HMAC-SHA256 signature of the given data using the account's API secret.</summary>
    /// <param name="data">The data to sign, typically the request's query string.</param>
    /// <returns>The lowercase hexadecimal signature.</returns>
    string GetSignature(string data);
}
