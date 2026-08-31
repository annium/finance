using Xunit;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Tests;

/// <summary>
/// Groups every test class that reaches the live exchange into one collection, so xUnit runs them one after
/// another rather than in parallel.
/// </summary>
/// <remarks>
/// They share a single real account. Left in the default collection-per-class, the class that places orders
/// would trade while the market, provider and signature classes hold their own connections to that account
/// and spend the same request-weight budget - and since each class builds its own container, each gets a
/// rate limiter that believes it is the only caller. The offline tests stay parallel; only these are held
/// in line.
/// </remarks>
[CollectionDefinition(Name)]
public sealed class ExchangeCollection
{
    /// <summary>The collection name every exchange-facing test class joins.</summary>
    public const string Name = "exchange";
}
