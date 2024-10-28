using System.Runtime.InteropServices.Marshalling;
using System.Runtime.InteropServices;

namespace VST3;

[GeneratedComInterface]
[Guid("7A4D811C-5211-4A1F-AED9-D2EE0B43BF9F")]
partial interface IPluginFactory
{
    /** Fill a PFactoryInfo structure with information about the plug-in vendor. */
    PFactoryInfo getFactoryInfo();

    /** Returns the number of exported classes by this factory. If you are using the CPluginFactory
	 * implementation provided by the SDK, it returns the number of classes you registered with
	 * CPluginFactory::registerClass. */
    [PreserveSig] int countClasses();

    /** Fill a PClassInfo structure with information about the class at the specified index. */
    [PreserveSig] int getClassInfo(int index, out PClassInfo info);

    /** Create a new class instance. */
    IntPtr createInstance(in Guid cid, in Guid _iid);
}
