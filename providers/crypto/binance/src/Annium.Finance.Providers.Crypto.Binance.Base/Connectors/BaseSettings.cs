using System;

namespace Annium.Finance.Providers.Crypto.Binance.Base.Connectors;

public sealed record BaseSettings(Uri HttpApi, Uri WsApi, string WsMarketEndpoint);
