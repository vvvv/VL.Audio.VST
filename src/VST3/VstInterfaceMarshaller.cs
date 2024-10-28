using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace VST3;

[CustomMarshaller(typeof(CustomMarshallerAttribute.GenericPlaceholder), MarshalMode.Default, typeof(VstInterfaceMarshaller<>))]
static unsafe class VstInterfaceMarshaller<T>
{
    private static readonly Guid? TargetInterfaceIID = StrategyBasedComWrappers.DefaultIUnknownInterfaceDetailsStrategy.GetIUnknownDerivedDetails(typeof(T).TypeHandle)?.Iid;

    public static void* ConvertToUnmanaged(T? managed)
    {
        if (managed == null)
        {
            return null;
        }

        if (!ComWrappers.TryGetComInstance(managed, out nint unknown))
        {
            unknown = VstWrappers.Instance.GetOrCreateComInterfaceForObject(managed, CreateComInterfaceFlags.None);
        }
        return CastIUnknownToInterfaceType(unknown);
    }

    public static T? ConvertToManaged(void* unmanaged)
    {
        if (unmanaged == null)
        {
            return default;
        }

        return (T)VstWrappers.Instance.GetOrCreateObjectForComInstance((nint)unmanaged, CreateObjectFlags.Unwrap);
    }

    public static void Free(void* unmanaged)
    {
        if (unmanaged != null)
        {
            Marshal.Release((nint)unmanaged);
        }
    }

    internal static void* CastIUnknownToInterfaceType(nint unknown)
    {
        if (TargetInterfaceIID is null)
        {
            // If the managed type isn't a GeneratedComInterface-attributed type, we'll marshal to an IUnknown*.
            return (void*)unknown;
        }
        if (Marshal.QueryInterface(unknown, in Nullable.GetValueRefOrDefaultRef(in TargetInterfaceIID), out nint interfacePointer) != 0)
        {
            Marshal.Release(unknown);
            throw new InvalidCastException($"Unable to cast the provided managed object to a COM interface with ID '{TargetInterfaceIID.GetValueOrDefault():B}'");
        }
        Marshal.Release(unknown);
        return (void*)interfacePointer;
    }
}
