namespace Annium.Finance.Providers.Crypto.Binance.Base.Internal.Contracts.Shared.Domain;

internal sealed record StreamData<T>(string Name, T Data);
