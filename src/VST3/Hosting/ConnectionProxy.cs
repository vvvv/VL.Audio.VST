using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace VST3.Hosting;

[GeneratedComClass]
internal partial class ConnectionProxy : IConnectionPoint
{
    private readonly IConnectionPoint srcConnection;
    private readonly SynchronizationContext? synchronizationContext;
    private IConnectionPoint? dstConnection;

    public ConnectionProxy(IConnectionPoint connectionPoint)
    {
        this.srcConnection = connectionPoint ?? throw new ArgumentNullException(nameof(connectionPoint));
        this.synchronizationContext = SynchronizationContext.Current;
    }

    public void connect(IConnectionPoint other)
    {
        if (other is null)
            throw new ArgumentNullException(nameof(other));

        if (dstConnection is not null)
            throw new InvalidOperationException("Already connected");

        dstConnection = other;
        try
        {
            srcConnection.connect(this);
        }
        catch
        {
            dstConnection = null;
            throw;
        }
    }

    public void disconnect(IConnectionPoint other)
    {
        if (other is null)
            throw new ArgumentNullException(nameof(other));

        if (other != dstConnection)
            throw new ArgumentException("Not connected to this connection point", nameof(other));

        srcConnection.disconnect(this);
        dstConnection = null;
    }

    public void notify(IMessage message)
    {
        if (dstConnection is null)
            throw new InvalidOperationException("Not connected");

        if (synchronizationContext is not null && SynchronizationContext.Current != synchronizationContext)
            synchronizationContext.Post(m => dstConnection.notify((IMessage)m!), message);
        else
            dstConnection.notify(message);
    }

    public void disconnect()
    {
        if (dstConnection is not null)
            disconnect(dstConnection);
    }
}
