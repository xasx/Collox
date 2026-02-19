using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Collox.Common;

internal enum Origin
{
    Server,
    Client
}

/// <summary>
/// Manages a bidirectional communication session between MCP client and server using in-memory channels.
/// Each session has its own pair of channels that are shared between the client and server transport instances.
/// </summary>
internal class TransportSession
{
    private readonly Channel<JsonRpcMessage> _serverToClientChannel;
    private readonly Channel<JsonRpcMessage> _clientToServerChannel;
    private int _disposeCount;

    public TransportSession()
    {
        _serverToClientChannel = Channel.CreateUnbounded<JsonRpcMessage>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = true
        });
        _clientToServerChannel = Channel.CreateUnbounded<JsonRpcMessage>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = true
        });
    }

    public ChannelReader<JsonRpcMessage> GetReader(Origin origin) =>
        origin == Origin.Client ? _serverToClientChannel.Reader : _clientToServerChannel.Reader;

    public ChannelWriter<JsonRpcMessage> GetWriter(Origin origin) =>
        origin == Origin.Client ? _clientToServerChannel.Writer : _serverToClientChannel.Writer;

    /// <summary>
    /// Disposes the session. When both client and server have disposed, completes both channels.
    /// This ensures neither side blocks waiting for the other during shutdown.
    /// </summary>
    public void Dispose()
    {
        // Both client and server call this when they dispose. We complete the channels
        // only after both have disposed to ensure clean shutdown coordination.
        if (Interlocked.Increment(ref _disposeCount) == 2)
        {
            _serverToClientChannel.Writer.TryComplete();
            _clientToServerChannel.Writer.TryComplete();
        }
    }
}

/// <summary>
/// Singleton manager for the default in-memory MCP transport session.
/// Provides a shared session instance that both client and server can access.
/// </summary>
internal static class DefaultTransportSession
{
    private static readonly Lazy<TransportSession> _defaultSession = new(() => new TransportSession());

    /// <summary>
    /// Gets the default shared transport session for in-memory MCP communication.
    /// </summary>
    public static TransportSession Instance => _defaultSession.Value;
}

internal class QueueTransport : ITransport
{
    private readonly TransportSession _session;
    private readonly Origin _origin;
    private readonly ChannelWriter<JsonRpcMessage> _messageWriter;

    public QueueTransport(Origin origin) : this(DefaultTransportSession.Instance, origin)
    {
    }

    public QueueTransport(TransportSession session, Origin origin)
    {
        _session = session ?? DefaultTransportSession.Instance;
        _origin = origin;
        SessionId = Guid.NewGuid().ToString();
        MessageReader = _session.GetReader(origin);
        _messageWriter = _session.GetWriter(origin);
    }

    public string SessionId { get; }

    public ChannelReader<JsonRpcMessage> MessageReader { get; }

    public ValueTask DisposeAsync()
    {
        _session.Dispose();
        return ValueTask.CompletedTask;
    }

    public async Task SendMessageAsync(JsonRpcMessage message, CancellationToken cancellationToken = default)
    {
        await _messageWriter.WriteAsync(message, cancellationToken);
    }
}

internal class ClientQueueTransport : IClientTransport
{
    private readonly TransportSession _session;

    public ClientQueueTransport() : this(DefaultTransportSession.Instance)
    {
    }

    public ClientQueueTransport(TransportSession session)
    {
        _session = session ?? DefaultTransportSession.Instance;
    }

    public string Name => "In-Memory Queue Transport";

    public Task<ITransport> ConnectAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<ITransport>(new QueueTransport(_session, Origin.Client));
    }
}
