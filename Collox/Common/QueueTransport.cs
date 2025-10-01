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

internal class QueueTransport : ITransport
{
    private static readonly Channel<JsonRpcMessage> serverQueue = Channel.CreateUnbounded<JsonRpcMessage>();
    private static readonly Channel<JsonRpcMessage> clientQueue = Channel.CreateUnbounded<JsonRpcMessage>();

    private readonly Origin origin;

    public QueueTransport(Origin origin)
    {
        this.origin = origin;
        MessageReader = origin == Origin.Client ? serverQueue.Reader : clientQueue.Reader;
        messageWriter = origin == Origin.Client ? clientQueue.Writer : serverQueue.Writer;
    }

    public string SessionId { get; }

    public ChannelReader<JsonRpcMessage> MessageReader { get; init; }

    private readonly ChannelWriter<JsonRpcMessage> messageWriter;

    public async ValueTask DisposeAsync()
    {

        messageWriter.Complete();
        await MessageReader.Completion.ConfigureAwait(false);
    }

    public async Task SendMessageAsync(JsonRpcMessage message, CancellationToken cancellationToken = default)
    {
        await messageWriter.WriteAsync(message, cancellationToken);
    }
}

internal class ClientQueueTransport : IClientTransport
{
    public string Name => "In-Memory Queue Transport";

    public Task<ITransport> ConnectAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<ITransport>(new QueueTransport(Origin.Client));
    }
}
