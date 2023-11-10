using System;
using System.Collections.Concurrent;
using Annium.Core.DependencyInjection;
using Annium.Finance.Providers.Abstractions.Connectors.Connectors;
using Annium.Finance.Providers.Shared.Connectors;
using Annium.Testing;
using Xunit;
using Xunit.Abstractions;
using static Annium.Finance.Providers.Abstractions.Connectors.Connectors.ConnectorStatus;

namespace Annium.Finance.Providers.Tests.Shared.Connectors;

public class StatusMonitorTests : TestBase
{
    private readonly ConcurrentQueue<ConnectorStatus> _statuses = new();

    public StatusMonitorTests(ITestOutputHelper outputHelper)
        : base(outputHelper)
    {
        Register(container =>
        {
            container.AddProvidersSingleton();
        });

        var monitor = Get<IStatusMonitor>();
        monitor.OnStatusChanged += _statuses.Enqueue;
    }

    [Fact]
    public void UnboundReporter_Throws()
    {
        var reporter = Get<IStatusReporter>();

        Wrap.It(() => reporter.Connecting()).Throws<InvalidOperationException>().Reports("not bound");
        Wrap.It(() => reporter.Connected()).Throws<InvalidOperationException>().Reports("not bound");
        Wrap.It(() => reporter.Disconnected()).Throws<InvalidOperationException>().Reports("not bound");
    }

    [Fact]
    public void SingleReporter()
    {
        var reporter = Get<IStatusReporter>();
        reporter.Bind(new A());

        _statuses.IsEmpty();

        reporter.Connecting();

        _statuses.IsEqual(new[] { Connecting });
    }

    [Fact]
    public void MultipleReporters()
    {
        var reporterA = Get<IStatusReporter>();
        reporterA.Bind(new A());
        var reporterB = Get<IStatusReporter>();
        reporterB.Bind(new B());

        _statuses.IsEmpty();

        reporterA.Connecting();
        _statuses.IsEqual(new[] { Connecting });

        reporterB.Connecting();
        _statuses.IsEqual(new[] { Connecting });

        reporterA.Connected();
        _statuses.IsEqual(new[] { Connecting });

        reporterB.Connected();
        _statuses.IsEqual(new[] { Connecting, Connected });

        reporterA.Disconnected();
        _statuses.IsEqual(new[] { Connecting, Connected, Connecting });

        reporterB.Disconnected();
        _statuses.IsEqual(new[] { Connecting, Connected, Connecting, Disconnected });
    }
}

file record A;

file record B;
