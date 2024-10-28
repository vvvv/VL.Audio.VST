using System.Runtime.InteropServices.Marshalling;
using System.Runtime.InteropServices;

namespace VST3;

[GeneratedComInterface]
[Guid("0007B650-F24B-4C0B-A464-EDB9F00B2ABB")]
partial interface IPluginFactory2 : IPluginFactory
{
    [PreserveSig] int getClassInfo2(int index, out PClassInfo2 info);
}
