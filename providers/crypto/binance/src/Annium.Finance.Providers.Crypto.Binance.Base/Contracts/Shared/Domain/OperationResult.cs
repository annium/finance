namespace Annium.Finance.Providers.Crypto.Binance.Base.Contracts.Shared.Domain;

public sealed record OperationResult(long Code, string Message)
{
    public const long Aborted = 1;
    public const long ParseError = 2;
}
