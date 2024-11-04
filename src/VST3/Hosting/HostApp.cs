using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace VST3.Hosting;

[GeneratedComClass]
sealed partial class HostApp : VstObject<IHostApplication>, IHostApplication, IPlugInterfaceSupport
{
    private readonly HashSet<Guid> supportedInterfaces;

    public HostApp(IEnumerable<Type> supportedInterfaces)
    {
        this.supportedInterfaces = supportedInterfaces.Select(t => t.GUID).ToHashSet();
    }

    nint IHostApplication.createInstance(Guid cid, Guid _iid)
    {
        if (cid == typeof(IMessage).GUID && _iid == typeof(IMessage).GUID)
            return new Message();

        if (cid == typeof(IAttributeList).GUID && _iid == typeof(IAttributeList).GUID)
            return new AttributeList();

        throw new NotImplementedException();
    }

    void IHostApplication.getName(Span<char> name)
    {
        "vvvv".CopyTo(name);
    }

    bool IPlugInterfaceSupport.isPlugInterfaceSupported(Guid _iid)
    {
        return supportedInterfaces.Contains(_iid);
    }
}
