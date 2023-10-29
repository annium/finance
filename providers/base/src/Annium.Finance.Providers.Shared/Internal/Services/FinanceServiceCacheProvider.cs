using System;
using System.Threading;
using System.Threading.Tasks;
using Annium.Core.DependencyInjection;
using Annium.Extensions.Pooling;
using Annium.Finance.Providers.Abstractions.Connectors.Services;
using Annium.Finance.Providers.Abstractions.Domain.Models;
using Annium.Logging;

namespace Annium.Finance.Providers.Shared.Internal.Services;

internal class FinanceServiceCacheProvider : ObjectCacheProvider<ProviderKey, IFinanceService>, ILogSubject
{
    public ILogger Logger { get; }
    public override bool HasCreate => true;
    public override bool HasExternalCreate => false;
    private readonly IIndex<ProviderKey, Func<IFinanceService>> _serviceFactories;

    public FinanceServiceCacheProvider(IIndex<ProviderKey, Func<IFinanceService>> serviceFactories, ILogger logger)
    {
        Logger = logger;
        _serviceFactories = serviceFactories;
    }

    public override async Task<IFinanceService> CreateAsync(ProviderKey providerKey, CancellationToken ct)
    {
        this.Trace("create new {providerKey} finance service", providerKey);
        var service = _serviceFactories[providerKey]();

        this.Trace("init {providerKey} finance service", providerKey);
        await service.InitAsync(providerKey.Environment);

        return service;
    }
}
