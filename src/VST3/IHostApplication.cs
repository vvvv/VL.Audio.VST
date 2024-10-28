using System.Runtime.InteropServices.Marshalling;
using System.Runtime.InteropServices;

namespace VST3;

[GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper)]
[Guid("58e595cc-db2d-4969-8b6a-af8c36a664e5")]
partial interface IHostApplication
{
    /** Gets host application name. */
    void getName([MarshalUsing(ConstantElementCount = 128)] Span<char> name);

    /** Creates host object (e.g. Vst::IMessage). */
    IntPtr createInstance(Guid cid, Guid _iid);
}
