namespace Annium.Finance.Providers.Core.Shared.TimeSync;

/// <summary>
/// Timing configuration for an
/// <see cref="Annium.Finance.Providers.Core.Internal.Shared.TimeSync.ServerTimeSource"/>.
/// </summary>
/// <param name="LoadInterval">The interval, in milliseconds, between refresh attempts before the first successful load, or after a confirm attempt fails.</param>
/// <param name="ConfirmInterval">The interval, in milliseconds, between refresh attempts once server time has been successfully loaded at least once.</param>
public sealed record ServerTimeProviderConfig(int LoadInterval, int ConfirmInterval);
