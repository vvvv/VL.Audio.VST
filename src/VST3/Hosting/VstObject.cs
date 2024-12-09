using VL.Audio.VST.Internal;

namespace VST3.Hosting;

internal abstract class VstObject<TComInterface>
{
    private static readonly Guid comInterfaceGuid = typeof(TComInterface).GUID;
    private readonly nint comInterfacePtr;

    public VstObject()
    {
        comInterfacePtr = this.GetComPtr(comInterfaceGuid);
    }

    public nint ComInterfacePtr => comInterfacePtr;

    public static implicit operator nint(VstObject<TComInterface> obj) => obj.ComInterfacePtr;
}
