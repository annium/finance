namespace Annium.Finance.Providers.Crypto.Binance.Base.Contracts.Shared.Domain;

public sealed record OperationResult(long Code, string Message)
{
    public const long NetworkError = 1;
    public const long Aborted = 2;
    public const long ParseError = 3;
}
