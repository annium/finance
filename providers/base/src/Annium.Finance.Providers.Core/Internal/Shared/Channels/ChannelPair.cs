using System;
using System.Reactive.Linq;
using System.Threading.Channels;
using Annium.Logging;
using Annium.Threading.Channels;

namespace Annium.Finance.Providers.Core.Internal.Shared.Channels;

internal class ChannelPair<T>
{
    public IObservable<T> Observable { get; }

    private readonly ChannelWriter<T> _sourceWriter;
    private readonly ChannelReader<T> _sourceReader;
    private readonly ChannelWriter<T> _targetWriter;
    private readonly ILogger _logger;

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

    public void Write(T value) => _sourceWriter.Write(value);

    public IDisposable Connect() => _sourceReader.Pipe(_targetWriter, _logger);
}
