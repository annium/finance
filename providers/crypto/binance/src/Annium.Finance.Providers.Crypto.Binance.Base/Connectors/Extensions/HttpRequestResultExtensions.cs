using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Domain.Operations;
using Annium.Finance.Providers.Crypto.Binance.Base.Contracts.Shared.Domain;
using Annium.Finance.Providers.Shared.Connectors.Extensions;
using Annium.Net.Http;

namespace Annium.Finance.Providers.Crypto.Binance.Base.Connectors.Extensions;

public static class HttpRequestResultExtensions
{
    public static Task<MarketResult<T>> AsMarketResultAsync<T>(this IHttpRequest request, T defaultValue) =>
        request.AsMarketResultAsync<T, OperationResult>(defaultValue, x => x.Message);

    public static Task<UserResult<T>> AsUserResultAsync<T>(this IHttpRequest request, T defaultValue) =>
        request.AsUserResultAsync<T, OperationResult>(defaultValue, x => x.Message);
}
