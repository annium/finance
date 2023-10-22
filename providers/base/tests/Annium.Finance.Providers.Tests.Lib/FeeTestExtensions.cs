using System.Runtime.CompilerServices;

namespace Annium.Finance.Providers.Tests.Lib;

public static class FeeTestExtensions
{
    public const decimal Value = 0.00015m;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static decimal PlusFee(this decimal value, byte multiplier = 1) => value * (1 + multiplier * Value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static decimal Fee(this decimal value, byte multiplier = 1) => value * multiplier * Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static decimal MinusFee(this decimal value, byte multiplier = 1) => value * (1 - multiplier * Value);
}