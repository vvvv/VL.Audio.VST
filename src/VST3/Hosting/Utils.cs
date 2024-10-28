using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace VST3.Hosting;

internal static class Utils
{
    public static int GetRefCount(this nint pUnk)
    {
        Marshal.AddRef(pUnk);
        return Marshal.Release(pUnk);
    }
}
