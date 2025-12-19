using System;
using System.Threading;
using Annium.Finance.Providers.Core.Shared.RateLimits;
using Annium.Logging;
using Annium.Threading;

namespace Annium.Finance.Providers.Core.Internal.Shared.RateLimits;

internal class RateLimiter : IRateLimiter, ILogSubject
{
    private const float WaterMarkLevel = 0.8f;
    public ILogger Logger { get; }
    private readonly int _lowerWeightValue;
    private readonly int _lowerWeightDelay;
    private readonly ISequentialTimer _lowerWeight;
    private bool _isLowerWeightRequested;
    private int _waterMark;
    private int _usedWeight;

    public RateLimiter(int limit, int lowerWeightValue, int lowerWeightDelay, ILogger logger)
    {
        _lowerWeightValue = lowerWeightValue;
        _lowerWeightDelay = lowerWeightDelay;
        Logger = logger;
        UpdateLimit(limit);
        _lowerWeight = Timers.Sync(LowerUsedWeight, Timeout.Infinite, Timeout.Infinite, logger);
    }

    public void Dispose()
    {
        _lowerWeight.Dispose();
    }

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

    public void UsedWeight(int weight)
    {
        this.Trace("used weight: {usedWeightBefore} -> {usedWeightAfter}", _usedWeight, weight);
        _usedWeight = weight;

        if (_usedWeight < _waterMark || Interlocked.CompareExchange(ref _isLowerWeightRequested, true, false))
            return;

        _lowerWeight.Change(_lowerWeightDelay, _lowerWeightDelay);
    }

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
