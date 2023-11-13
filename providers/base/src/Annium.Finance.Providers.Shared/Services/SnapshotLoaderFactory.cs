using System;
using System.Threading;
using System.Threading.Tasks;
using Annium.Core.DependencyInjection;
using Annium.Finance.Providers.Abstractions.Domain.Operations;
using Annium.Finance.Providers.Shared.Connectors;
using Annium.Logging;

namespace Annium.Finance.Providers.Shared.Services;

public class SnapshotLoaderFactory
{
    private readonly IServiceProvider _sp;
    private readonly ILogger _logger;

    public SnapshotLoaderFactory(IServiceProvider sp, ILogger logger)
    {
        _sp = sp;
        _logger = logger;
    }

    public SnapshotLoader<TData> Create<TData>(
        SnapshotLoaderConfig cfg,
        Func<CancellationToken, Task<IBaseResult<TData>>> load
    )
    {
        var reporter = _sp.Resolve<IStatusReporter>();

        return new SnapshotLoader<TData>(cfg, load, reporter, _logger);
    }
}
