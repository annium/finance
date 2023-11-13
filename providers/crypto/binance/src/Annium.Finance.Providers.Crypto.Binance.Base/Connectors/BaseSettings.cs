using System;
using Annium.Finance.Providers.Abstractions.Domain.Enums;

namespace Annium.Finance.Providers.Crypto.Binance.Base.Connectors;

public sealed record BaseSettings(ProviderEnvironment Env, Uri HttpApi, Uri WsApi, string WsMarketEndpoint);
