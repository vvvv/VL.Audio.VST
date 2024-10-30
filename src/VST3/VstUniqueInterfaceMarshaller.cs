using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace VST3;

[CustomMarshaller(typeof(CustomMarshallerAttribute.GenericPlaceholder), MarshalMode.Default, typeof(VstUniqueInterfaceMarshaller<>))]
static unsafe class VstUniqueInterfaceMarshaller<T>
{
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
        return VstInterfaceMarshaller<T>.CastIUnknownToInterfaceType(unknown);
    }

    public static T? ConvertToManaged(void* unmanaged)
    {
        if (unmanaged == null)
        {
            return default;
        }

        return (T)VstWrappers.Instance.GetOrCreateObjectForComInstance((nint)unmanaged, CreateObjectFlags.Unwrap | CreateObjectFlags.UniqueInstance);
    }

    public static void Free(void* unmanaged)
    {
        if (unmanaged != null)
        {
            Marshal.Release((nint)unmanaged);
        }
    }
}
