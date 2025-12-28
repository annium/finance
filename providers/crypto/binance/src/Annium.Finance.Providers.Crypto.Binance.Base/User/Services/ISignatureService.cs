namespace Annium.Finance.Providers.Crypto.Binance.Base.User.Services;

public interface ISignatureService
{
    long ServerTime { get; }
    string GetKey();
    string GetSignature(string data);
}
