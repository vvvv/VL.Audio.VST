using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Threading.Tasks;

namespace VST3.Hosting;

internal static class Utils
{
    public static void ReleaseComObject(this object obj)
    {
        if (obj is ComObject com && IsUniqueInstance(com))
        {
            // See https://github.com/dotnet/runtime/issues/96901
            GC.SuppressFinalize(com);
            com.FinalRelease();
        }

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_UniqueInstance")]
        extern static bool IsUniqueInstance(ComObject comObject);
    }

    public static nint GetComPtr(this object? obj, in Guid guid)
    {
        if (obj is null)
            return default;

        var pUnk = VstWrappers.Instance.GetOrCreateComInterfaceForObject(obj, CreateComInterfaceFlags.None);
        Marshal.QueryInterface(pUnk, in guid, out var pInt);
        Marshal.Release(pUnk);
        return pInt;
    }

    public static int GetRefCount(this nint pUnk)
    {
        Marshal.AddRef(pUnk);
        return Marshal.Release(pUnk);
    }
}
