namespace Annium.Finance.Providers.Crypto.Binance.Base.Contracts.Shared.Domain;

public sealed record StreamData<T>(string Name, T Data);
