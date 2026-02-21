using System.Threading.Channels;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Serilog;

namespace Collox.Common;

/// <summary>
/// Identifies which side of the MCP communication the transport instance belongs to,
/// determining channel read/write direction.
/// </summary>
internal enum Origin
{
    Server,
    Client
}

/// <summary>
/// Manages a bidirectional communication session between MCP client and server using in-memory channels.
/// Each session has its own pair of channels that are shared between the client and server transport instances.
/// </summary>
/// <remarks>
/// Channel completion is deferred until both the server and client sides have disposed.
/// This prevents one side from prematurely closing the channels while the other is still using them.
/// Per-side disposal is idempotent: disposing the same side multiple times has no additional effect.
/// </remarks>
internal class TransportSession : IDisposable
{
    private static readonly ILogger Logger = Log.ForContext<TransportSession>();

    private readonly Channel<JsonRpcMessage> _serverToClientChannel;
    private readonly Channel<JsonRpcMessage> _clientToServerChannel;
    private int _serverDisposed;
    private int _clientDisposed;

    public TransportSession()
    {
        SessionId = Guid.NewGuid().ToString();
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
        Logger.Debug("Created TransportSession {SessionId}", SessionId);
    }

    /// <summary>
    /// Gets the unique identifier for this transport session.
    /// </summary>
    public string SessionId { get; }

    /// <summary>
    /// Gets whether both the server and client sides have disposed this session.
    /// </summary>
    public bool IsFullyDisposed =>
        Volatile.Read(ref _serverDisposed) == 1 && Volatile.Read(ref _clientDisposed) == 1;

    /// <summary>
    /// Gets the channel reader for the specified origin.
    /// The client reads messages sent by the server, and the server reads messages sent by the client.
    /// </summary>
    public ChannelReader<JsonRpcMessage> GetReader(Origin origin) =>
        origin == Origin.Client ? _serverToClientChannel.Reader : _clientToServerChannel.Reader;

    /// <summary>
    /// Gets the channel writer for the specified origin.
    /// The client writes to the client-to-server channel, and the server writes to the server-to-client channel.
    /// </summary>
    public ChannelWriter<JsonRpcMessage> GetWriter(Origin origin) =>
        origin == Origin.Client ? _clientToServerChannel.Writer : _serverToClientChannel.Writer;

    /// <summary>
    /// Records that one side of the session has disposed. When both sides have disposed,
    /// completes both channels to unblock any pending reads.
    /// </summary>
    /// <remarks>
    /// Per-side disposal is idempotent. Calling this multiple times for the same origin has no effect
    /// beyond the first call. Channels are only completed once both sides have disposed.
    /// </remarks>
    /// <param name="origin">Which side of the transport is disposing.</param>
    public void Dispose(Origin origin)
    {
        ref int flag = ref origin == Origin.Server ? ref _serverDisposed : ref _clientDisposed;
        if (Interlocked.Exchange(ref flag, 1) != 0)
            return;

        Logger.Debug("TransportSession {SessionId} {Origin} side disposed", SessionId, origin);

        if (IsFullyDisposed)
        {
            if (!_serverToClientChannel.Writer.TryComplete())
                Logger.Warning("TransportSession {SessionId} server-to-client channel was already completed", SessionId);
            if (!_clientToServerChannel.Writer.TryComplete())
                Logger.Warning("TransportSession {SessionId} client-to-server channel was already completed", SessionId);

            Logger.Information("TransportSession {SessionId} fully disposed, channels completed", SessionId);
        }
    }

    /// <summary>
    /// Disposes both sides of the session. Prefer <see cref="Dispose(Origin)"/> for coordinated shutdown.
    /// </summary>
    void IDisposable.Dispose()
    {
        Dispose(Origin.Server);
        Dispose(Origin.Client);
    }
}

/// <summary>
/// Default session provider for in-memory MCP transport.
/// Provides a shared session instance that both client and server can access.
/// Automatically creates a new session if the previous one has been fully disposed.
/// </summary>
internal static class DefaultTransportSession
{
    private static readonly ILogger Logger = Log.ForContext(typeof(DefaultTransportSession));

    private static TransportSession _currentSession = new();
    private static readonly object _lock = new();

    /// <summary>
    /// Gets the default shared transport session for in-memory MCP communication.
    /// If the current session has been fully disposed, creates a fresh session.
    /// </summary>
    public static TransportSession Instance
    {
        get
        {
            lock (_lock)
            {
                if (_currentSession.IsFullyDisposed)
                {
                    Logger.Information(
                        "Default TransportSession {OldSessionId} was fully disposed; creating replacement",
                        _currentSession.SessionId);
                    _currentSession = new TransportSession();
                }
                return _currentSession;
            }
        }
    }
}

/// <summary>
/// In-memory implementation of <see cref="ITransport"/> that reads and writes JSON-RPC messages
/// through a shared <see cref="TransportSession"/>'s channel pair.
/// </summary>
internal class QueueTransport : ITransport
{
    private static readonly ILogger Logger = Log.ForContext<QueueTransport>();

    private readonly TransportSession _session;
    private readonly Origin _origin;
    private readonly ChannelWriter<JsonRpcMessage> _messageWriter;
    private int _disposed;

    public QueueTransport(Origin origin) : this(DefaultTransportSession.Instance, origin)
    {
    }

    /// <summary>
    /// Creates a new QueueTransport for the specified session and origin.
    /// </summary>
    /// <param name="session">The transport session to use. Must not be fully disposed.</param>
    /// <param name="origin">Whether this transport is for the client or server side.</param>
    /// <exception cref="ObjectDisposedException">Thrown if the session is already fully disposed.</exception>
    public QueueTransport(TransportSession session, Origin origin)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.IsFullyDisposed)
            throw new ObjectDisposedException(nameof(TransportSession),
                $"Cannot create transport: session '{session.SessionId}' is already fully disposed.");

        _session = session;
        _origin = origin;
        MessageReader = _session.GetReader(origin);
        _messageWriter = _session.GetWriter(origin);
    }

    public string SessionId => _session.SessionId;

    public ChannelReader<JsonRpcMessage> MessageReader { get; }

    public ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
        {
            _session.Dispose(_origin);
        }
        return ValueTask.CompletedTask;
    }

    public async Task SendMessageAsync(JsonRpcMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            await _messageWriter.WriteAsync(message, cancellationToken).ConfigureAwait(false);
        }
        catch (ChannelClosedException)
        {
            throw new ObjectDisposedException(
                nameof(QueueTransport),
                $"Transport session '{_session.SessionId}' (origin: {_origin}) was disposed while sending a message.");
        }
    }
}

/// <summary>
/// Client transport factory that creates in-memory <see cref="QueueTransport"/> instances
/// connected to a <see cref="TransportSession"/> as the client side.
/// </summary>
internal class ClientQueueTransport : IClientTransport
{
    private readonly TransportSession _session;

    public ClientQueueTransport() : this(DefaultTransportSession.Instance)
    {
    }

    /// <summary>
    /// Creates a new ClientQueueTransport for the specified session.
    /// </summary>
    /// <param name="session">The transport session to use.</param>
    public ClientQueueTransport(TransportSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        _session = session;
    }

    public string Name => "In-Memory Queue Transport";

    public Task<ITransport> ConnectAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<ITransport>(new QueueTransport(_session, Origin.Client));
    }
}
