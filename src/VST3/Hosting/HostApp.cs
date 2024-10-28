using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace VST3.Hosting;

[GeneratedComClass]
sealed partial class HostApp : IHostApplication
{
    nint IHostApplication.createInstance(Guid cid, Guid _iid)
    {
        if (cid == typeof(IMessage).GUID && _iid == typeof(IMessage).GUID)
        {
            var comObject = VstWrappers.Instance.GetOrCreateComInterfaceForObject(new Message(), CreateComInterfaceFlags.None);
            return GetInterfacePointerFromComInterface(ref _iid, comObject);
        }

        if (cid == typeof(IAttributeList).GUID && _iid == typeof(IAttributeList).GUID)
        {
            var comObject = VstWrappers.Instance.GetOrCreateComInterfaceForObject(new AttributeList(), CreateComInterfaceFlags.None);
            return GetInterfacePointerFromComInterface(ref _iid, comObject);
        }

        throw new NotImplementedException();

        static nint GetInterfacePointerFromComInterface(ref Guid _iid, nint comObject)
        {
            Marshal.QueryInterface(comObject, ref _iid, out var interfacePtr);
            Marshal.Release(comObject);
            return interfacePtr;
        }
    }

    void IHostApplication.getName(Span<char> name)
    {
        "vvvv".CopyTo(name);
    }
}
