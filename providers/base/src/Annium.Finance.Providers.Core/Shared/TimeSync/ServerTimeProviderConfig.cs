namespace Annium.Finance.Providers.Core.Shared.TimeSync;

public sealed record ServerTimeProviderConfig(int LoadInterval, int ConfirmInterval);
