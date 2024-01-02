namespace Annium.Finance.Providers.Shared.ServerTime;

public sealed record ServerTimeProviderConfig(int LoadInterval, int ConfirmInterval);
