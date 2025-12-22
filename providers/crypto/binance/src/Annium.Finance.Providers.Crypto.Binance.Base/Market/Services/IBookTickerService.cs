using System;
using System.Collections.Generic;
using Annium.Finance.Providers.Abstractions.Domain.Market;

namespace Annium.Finance.Providers.Crypto.Binance.Base.Market.Services;

public interface IBookTickerService : IDisposable
{
    event Action<InstrumentTicker> OnData;
    void Subscribe(IReadOnlyCollection<string> symbols);
    void Unsubscribe(IReadOnlyCollection<string> symbols);
}
