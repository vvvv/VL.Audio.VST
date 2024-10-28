using System.Runtime.InteropServices.Marshalling;
using System.Runtime.InteropServices;

namespace VST3;

[GeneratedComInterface]
[Guid("4555A2AB-C123-4E57-9B12-291036878931")]
partial interface IPluginFactory3 : IPluginFactory2
{
    /** Returns the unicode class info for a given index. */
    [PreserveSig] int getClassInfoUnicode(int index, out PClassInfoW info);

    /** Receives information about host*/
    void setHostContext(IntPtr context);
}
