namespace Annium.Finance.Providers.Crypto.Binance.Base.Shared.Contracts.Domain;

public sealed record StreamData<T>(string Name, T Data);
