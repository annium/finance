using System;
using Annium.Finance.Providers.Abstractions.Domain.Interfaces;

namespace Annium.Finance.Providers.Crypto.Binance.Base.Connectors;

public sealed record BaseSettings(IMarketConfig Config, Uri HttpApi, Uri WsApi, string WsMarketEndpoint);
