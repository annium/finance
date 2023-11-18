using System;
using System.Threading;
using System.Threading.Tasks;
using Annium.Core.DependencyInjection;
using Annium.Finance.Providers.Abstractions.Domain.Operations;
using Annium.Finance.Providers.Shared.Connectors;
using Annium.Logging;

namespace Annium.Finance.Providers.Shared.Services;

public class LoaderFactory
{
    private readonly IServiceProvider _sp;
    private readonly ILogger _logger;

    public LoaderFactory(IServiceProvider sp, ILogger logger)
    {
        _sp = sp;
        _logger = logger;
    }

    public SnapshotLoader<T> CreateSnapshotLoader<T>(
        SnapshotLoaderConfig cfg,
        Func<CancellationToken, Task<IBaseResult<T>>> load
    )
    {
        var reporter = _sp.Resolve<IStatusReporter>();

        return new SnapshotLoader<T>(cfg, load, reporter, _logger);
    }

    public CompositeLoader<T> CreateCompositeLoader<T>(
        SnapshotLoaderConfig cfg,
        Func<CancellationToken, Task<IBaseResult<T>>> load,
        int intervalPeriod,
        int debouncePeriod
    )
    {
        var reporter = _sp.Resolve<IStatusReporter>();

        return new CompositeLoader<T>(cfg, load, reporter, intervalPeriod, debouncePeriod, _logger);
    }
}
