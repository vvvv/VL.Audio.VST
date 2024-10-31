using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace VST3;

[CustomMarshaller(typeof(bool), MarshalMode.Default, typeof(VstBoolMarshaller))]
internal static unsafe class VstBoolMarshaller
{
    public static bool ConvertToManaged(int unmanaged)
    {
        switch (unmanaged)
        {
            case 0:
                return true;
            case 1:
                return false;
            default:
                Marshal.ThrowExceptionForHR(unmanaged);
                return false;
        }
    }

    public static int ConvertToUnmanaged(bool managed)
    {
        return managed ? 0 : 1;
    }
}
