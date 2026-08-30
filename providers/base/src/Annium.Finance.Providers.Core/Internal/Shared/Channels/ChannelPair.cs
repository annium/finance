using System;
using System.Reactive.Linq;
using System.Threading.Channels;
using Annium.Logging;
using Annium.Threading.Channels;

namespace Annium.Finance.Providers.Core.Internal.Shared.Channels;

/// <summary>
/// A pair of unbounded channels used to fan values out to subscribers: writers push values into the source
/// channel, and once <see cref="Connect"/> pipes it to the target channel, they become visible on
/// <see cref="Observable"/>. Kept as two channels (rather than one) so writes can happen before any subscriber
/// connects, without values being lost.
/// </summary>
/// <typeparam name="T">The type of value carried through the channel pair.</typeparam>
internal class ChannelPair<T>
{
    /// <summary>
    /// An observable, multicast view of the target channel. Shared across subscribers (via <c>Publish().RefCount()</c>),
    /// so every subscriber sees the same sequence of values.
    /// </summary>
    public IObservable<T> Observable { get; }

    /// <summary>The writer side of the source channel, used by <see cref="Write"/>.</summary>
    private readonly ChannelWriter<T> _sourceWriter;

    /// <summary>The reader side of the source channel, piped into <see cref="_targetWriter"/> by <see cref="Connect"/>.</summary>
    private readonly ChannelReader<T> _sourceReader;

    /// <summary>The writer side of the target channel that backs <see cref="Observable"/>.</summary>
    private readonly ChannelWriter<T> _targetWriter;

    /// <summary>The logger instance used while piping values from the source to the target channel.</summary>
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChannelPair{T}"/> class, creating the underlying source and
    /// target channels.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public ChannelPair(ILogger logger)
    {
        var source = Channel.CreateUnbounded<T>();
        _sourceWriter = source.Writer;
        _sourceReader = source.Reader;

        var target = Channel.CreateUnbounded<T>();
        _targetWriter = target.Writer;
        Observable = target.Reader.AsObservable().Publish().RefCount();

        _logger = logger;
    }

    /// <summary>
    /// Writes a value into the source channel. The value only reaches <see cref="Observable"/> once
    /// <see cref="Connect"/> has been called.
    /// </summary>
    /// <param name="value">The value to write.</param>
    public void Write(T value) => _sourceWriter.Write(value);

    /// <summary>
    /// Starts piping values from the source channel to the target channel, so they start arriving on
    /// <see cref="Observable"/>.
    /// </summary>
    /// <returns>A disposable that stops the piping when disposed.</returns>
    public IAsyncDisposable Connect() => _sourceReader.Pipe(_targetWriter, _logger);
}
