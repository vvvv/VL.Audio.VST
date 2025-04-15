using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using VL.Core;
using VL.Core.CompilerServices;
using VST3;
using VST3.Hosting;

namespace VL.Audio.VST;

[Smell(SymbolSmell.Advanced)]
public sealed record PluginState(Guid Id, ImmutableArray<byte> Component, ImmutableArray<byte> Controller)
{
    public static readonly PluginState Default = new PluginState(default, ImmutableArray<byte>.Empty, ImmutableArray<byte>.Empty);

    public bool Equals(PluginState? other)
    {
        if (other is null)
            return false;

        return Id == other.Id
            && Component.SequenceEqual(other.Component)
            && Controller.SequenceEqual(other.Controller);
    }

    public override int GetHashCode() => Id.GetHashCode();

    internal bool HasComponentData => Component.Length > 0;
    internal bool HasControllerData => Controller.Length > 0;

    internal IBStream GetComponentStream() => GetStream(Component);

    internal IBStream GetControllerStream() => GetStream(Controller);

    private IBStream GetStream(ImmutableArray<byte> bytes)
    {
        if (bytes.IsDefault)
            bytes = ImmutableArray<byte>.Empty;

        var buffer = Unsafe.As<ImmutableArray<byte>, byte[]>(ref bytes);
        var memoryStream = new MemoryStream(buffer, 0, buffer.Length, writable: false);
        return new BStreamAdapter(memoryStream);
    }

    internal static PluginState From(Guid id, IComponent component, IEditController? controller)
    {
        var memoryStream = new MemoryStream();
        var componentState = Read(s => component.getState(s));
        var controllerState = Read(s => controller?.getState(s));
        return new PluginState(id, componentState, controllerState);

        ImmutableArray<byte> Read(Action<IBStream> reader)
        {
            var memoryStream = new MemoryStream();
            try
            {
                reader(new BStreamAdapter(memoryStream));
            }
            catch (NotImplementedException)
            {
                // Ok
            }
            memoryStream.Position = 0;
            var array = memoryStream.ToArray();
            return Unsafe.As<byte[], ImmutableArray<byte>>(ref array);
        }
    }
}
