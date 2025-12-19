namespace Annium.Finance.Providers.Crypto.Binance.Base.Shared.Contracts.Domain;

public sealed record OperationResult(long Code, string Message)
{
    public const long NetworkError = 1;
    public const long Aborted = 2;
    public const long ParseError = 3;
}
