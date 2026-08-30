using System.Runtime.CompilerServices;

namespace Annium.Finance.Providers.Tests.Lib.User;

/// <summary>
/// Computes the flat fee tests expect a fake fill to have been charged, so expected balances and order state
/// can be asserted without hardcoding the fee rate at every call site.
/// </summary>
public static class FeeTestExtensions
{
    /// <summary>The fee rate applied per unit of multiplier (0.015%).</summary>
    public const decimal Value = 0.00015m;

    /// <summary>
    /// Returns the value increased by its fee, as charged when the fee is added on top (e.g. buying).
    /// </summary>
    /// <param name="value">The value the fee is computed from.</param>
    /// <param name="multiplier">The number of fee units to apply.</param>
    /// <returns>The value plus its fee.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static decimal PlusFee(this decimal value, byte multiplier = 1) => value * (1 + multiplier * Value);

    /// <summary>
    /// Returns the fee charged on the value.
    /// </summary>
    /// <param name="value">The value the fee is computed from.</param>
    /// <param name="multiplier">The number of fee units to apply.</param>
    /// <returns>The fee amount.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static decimal Fee(this decimal value, byte multiplier = 1) => value * multiplier * Value;

    /// <summary>
    /// Returns the value decreased by its fee, as charged when the fee is deducted (e.g. selling).
    /// </summary>
    /// <param name="value">The value the fee is computed from.</param>
    /// <param name="multiplier">The number of fee units to apply.</param>
    /// <returns>The value minus its fee.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static decimal MinusFee(this decimal value, byte multiplier = 1) => value * (1 - multiplier * Value);
}
