using System;
using System.Threading;
using Annium.Finance.Providers.Core.Shared.RateLimits;
using Annium.Logging;
using Annium.Threading;

namespace Annium.Finance.Providers.Core.Internal.Shared.RateLimits;

/// <summary>
/// Default <see cref="IRateLimiter"/> implementation. Tracks used weight against a water mark set below the
/// configured limit, and gradually decays reported weight back down on a timer once it has crossed that water
/// mark, so <see cref="CanExecute"/> opens back up without waiting for the next <see cref="UsedWeight"/> report.
/// </summary>
internal class RateLimiter : IRateLimiter, ILogSubject
{
    /// <summary>The fraction of the configured limit below which <see cref="CanExecute"/> allows requests.</summary>
    private const float WaterMarkLevel = 0.8f;

    /// <summary>Gets the logger instance.</summary>
    public ILogger Logger { get; }

    /// <summary>The amount by which used weight is decayed on each tick of <see cref="_lowerWeight"/>.</summary>
    private readonly int _lowerWeightValue;

    /// <summary>The delay, in milliseconds, between decay ticks of <see cref="_lowerWeight"/>.</summary>
    private readonly int _lowerWeightDelay;

    /// <summary>The timer that periodically decays used weight once it has crossed the water mark.</summary>
    private readonly ISequentialTimer _lowerWeight;

    /// <summary>Whether the decay timer is currently armed, guarding against arming it more than once.</summary>
    private bool _isLowerWeightRequested;

    /// <summary>The weight threshold, derived from the configured limit, below which requests are allowed.</summary>
    private int _waterMark;

    /// <summary>The most recently reported used weight.</summary>
    private int _usedWeight;

    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimiter"/> class.
    /// </summary>
    /// <param name="limit">The initial rate limit.</param>
    /// <param name="lowerWeightValue">The amount by which used weight is decayed on each tick once it crosses the water mark.</param>
    /// <param name="lowerWeightDelay">The delay, in milliseconds, between decay ticks.</param>
    /// <param name="logger">The logger instance.</param>
    public RateLimiter(int limit, int lowerWeightValue, int lowerWeightDelay, ILogger logger)
    {
        _lowerWeightValue = lowerWeightValue;
        _lowerWeightDelay = lowerWeightDelay;
        Logger = logger;
        UpdateLimit(limit);
        _lowerWeight = Timers.Sync(LowerUsedWeight, Timeout.Infinite, Timeout.Infinite, logger);
    }

    /// <summary>
    /// Disposes the decay timer.
    /// </summary>
    public void Dispose()
    {
        _lowerWeight.Dispose();
    }

    /// <summary>
    /// Updates the rate limit and recalculates the water mark below which <see cref="CanExecute"/> allows requests.
    /// </summary>
    /// <param name="limit">The new rate limit.</param>
    public void UpdateLimit(int limit)
    {
        var waterMark = (limit * WaterMarkLevel).FloorInt32();
        this.Debug(
            "update limit: {limit}, waterMark: {waterMarkBefore} -> {waterMarkAfter}",
            limit,
            _waterMark,
            waterMark
        );
        _waterMark = waterMark;
    }

    /// <summary>
    /// Checks whether the currently used weight is still under the water mark.
    /// </summary>
    /// <returns><see langword="true"/> if a request can be executed now; otherwise, <see langword="false"/>.</returns>
    public bool CanExecute()
    {
        var canExecute = _usedWeight < _waterMark;
        this.Trace(
            "water mark: {waterMark}, usedWeight: {usedWeight} => {canExecute}",
            _waterMark,
            _usedWeight,
            canExecute
        );

        return canExecute;
    }

    /// <summary>
    /// Reports the weight currently used, replacing any previously reported value. If this pushes usage over the
    /// water mark, arms the decay timer (once) so used weight is gradually lowered over time.
    /// </summary>
    /// <param name="weight">The currently used weight.</param>
    public void UsedWeight(int weight)
    {
        this.Trace("used weight: {usedWeightBefore} -> {usedWeightAfter}", _usedWeight, weight);
        _usedWeight = weight;

        if (_usedWeight < _waterMark || Interlocked.CompareExchange(ref _isLowerWeightRequested, true, false))
            return;

        _lowerWeight.Change(_lowerWeightDelay, _lowerWeightDelay);
    }

    /// <summary>
    /// Callback for the decay timer: reduces the used weight by <see cref="_lowerWeightValue"/>, disarming the
    /// timer again once usage drops back under the water mark.
    /// </summary>
    private void LowerUsedWeight()
    {
        // decrease used weight with given value
        var usedWeight = Math.Max(_usedWeight - _lowerWeightValue, 0);

        // reset timer before state changes to keep safe
        if (usedWeight < _waterMark)
        {
            _isLowerWeightRequested = false;
            _lowerWeight.Change(Timeout.Infinite, Timeout.Infinite);
            this.Trace("lower timer reset");
        }

        this.Trace("{usedWeightBefore} -> {usedWeightAfter}", _usedWeight, usedWeight);
        _usedWeight = usedWeight;
    }
}
