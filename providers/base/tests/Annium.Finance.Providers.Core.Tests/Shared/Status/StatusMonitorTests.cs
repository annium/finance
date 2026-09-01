using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Connectors.Shared;
using Annium.Finance.Providers.Core.Shared.Status;
using Annium.Finance.Providers.Tests.Lib;
using Annium.Testing;
using Xunit;
using static Annium.Finance.Providers.Abstractions.Connectors.Shared.ConnectorStatus;

namespace Annium.Finance.Providers.Core.Tests.Shared.Status;

/// <summary>
/// Pins how <see cref="IStatusMonitor"/> aggregates the statuses reported by one or more bound
/// <see cref="IStatusReporter"/>s: a single reporter enforces bind/unbind ordering and rejects use while
/// unbound, and with multiple reporters bound to distinct targets the monitor reports connected only once every
/// target is connected, and connecting again as soon as any one drops.
/// </summary>
public class StatusMonitorTests : ProvidersTestBase
{
    /// <summary>Records every overall status transition reported by the monitor, in order.</summary>
    private readonly ConcurrentQueue<ConnectorStatus> _statuses = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="StatusMonitorTests"/> class.
    /// </summary>
    /// <param name="outputHelper">The xUnit output helper used to capture test logs.</param>
    public StatusMonitorTests(ITestOutputHelper outputHelper)
        : base(outputHelper) { }

    /// <summary>
    /// The provider is built by the base class during initialization, so anything resolved from it has to
    /// wait for that - a constructor runs too early.
    /// </summary>
    /// <returns>A task representing the asynchronous initialization.</returns>
    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();

        var monitor = Get<IStatusMonitor>();
        monitor.OnStatusChanged += _statuses.Enqueue;
    }

    /// <summary>
    /// Verifies that a reporter throws when used before <see cref="IStatusReporter.Bind{T}"/>, that binding a
    /// second target while already bound throws, and that once bound and later unbound the reporter both stops
    /// forwarding statuses and refuses another <see cref="IStatusReporter.Unbind"/>.
    /// </summary>
    [Fact]
    public void SingleReporter()
    {
        var reporter = Get<IStatusReporter>();
        var target = new A();

        Wrap.It(() => reporter.Connecting()).Throws<InvalidOperationException>().Reports("not bound");
        Wrap.It(() => reporter.Connected()).Throws<InvalidOperationException>().Reports("not bound");
        Wrap.It(() => reporter.Disconnected()).Throws<InvalidOperationException>().Reports("not bound");

        reporter.Bind(target);
        Wrap.It(() => reporter.Bind(new B())).Throws<InvalidOperationException>().Reports("already bound");

        _statuses.IsEmpty();

        reporter.Connecting();

        _statuses.IsEqual(new[] { Connecting });

        reporter.Unbind();
        Wrap.It(() => reporter.Unbind()).Throws<InvalidOperationException>().Reports("already unbound");
        Wrap.It(() => reporter.Connecting()).Throws<InvalidOperationException>().Reports("not bound");
        Wrap.It(() => reporter.Connected()).Throws<InvalidOperationException>().Reports("not bound");
        Wrap.It(() => reporter.Disconnected()).Throws<InvalidOperationException>().Reports("not bound");
    }

    /// <summary>
    /// An error a target reports reaches whoever is listening. The monitor is the only path an error takes
    /// from a provider to a consumer: the connector base subscribes here and re-raises it as its own
    /// OnError, and that is what a caller - or a test collecting failures - is watching.
    /// </summary>
    [Fact]
    public void ReportedError_ReachesListeners()
    {
        // arrange
        var errors = new ConcurrentQueue<ConnectorError>();
        var monitor = Get<IStatusMonitor>();
        monitor.OnError += errors.Enqueue;
        var reporter = Get<IStatusReporter>();
        reporter.Bind(new A());

        // act
        var error = new ConnectorError("something went wrong");

        reporter.Error(error);

        // assert
        errors.IsEqual(new[] { error });
    }

    /// <summary>
    /// Verifies that with two reporters bound to distinct targets, the monitor's overall status is connected only
    /// once both report connected, drops back to connecting the moment either target disconnects, and treats an
    /// unbind the same as that target going away.
    /// </summary>
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

        reporterA.Connected();
        _statuses.IsEqual(new[] { Connecting, Connected, Connecting, Disconnected, Connecting });

        reporterB.Unbind();
        _statuses.IsEqual(new[] { Connecting, Connected, Connecting, Disconnected, Connecting, Connected });
    }
}

/// <summary>Stands in for one component bound to a status reporter, distinguished from <see cref="B"/> only by its type identity.</summary>
file record A;

/// <summary>Stands in for a second, independent component bound to its own status reporter, distinguished from <see cref="A"/> only by its type identity.</summary>
file record B;
