using System;
using System.Threading;
using System.Threading.Tasks;
using Annium.Core.DependencyInjection;
using Annium.Extensions.Pooling;
using Annium.Finance.Providers.Abstractions.Connectors.Services;
using Annium.Finance.Providers.Abstractions.Domain.Models;
using Annium.Logging;
using OneOf;

namespace Annium.Finance.Providers.Shared.Internal.Services;

internal class FinanceServiceCacheProvider : ObjectCacheProvider<ProviderKey, IFinanceService>, ILogSubject
{
    public ILogger Logger { get; }
    private readonly IServiceProvider _sp;

    public FinanceServiceCacheProvider(IServiceProvider sp, ILogger logger)
    {
        Logger = logger;
        _sp = sp;
    }

    public override async Task<OneOf<IFinanceService, IDisposableReference<IFinanceService>>> CreateAsync(
        ProviderKey providerKey,
        CancellationToken ct
    )
    {
        this.Trace("create new {providerKey} finance service", providerKey);
        var service = _sp.ResolveKeyed<IFinanceService>(providerKey);

        this.Trace("init {providerKey} finance service", providerKey);
        await service.InitAsync(providerKey.Environment);

        return OneOf<IFinanceService, IDisposableReference<IFinanceService>>.FromT0(service);
    }
}
