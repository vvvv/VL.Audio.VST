using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace VST3;

//------------------------------------------------------------------------
/** Private plug-in message: Vst::IMessage
\ingroup vstIHost vst300
- [host imp]
- [create via IHostApplication::createInstance]
- [released: 3.0.0]
- [mandatory]

Messages are sent from a VST controller component to a VST editor component and vice versa.
\see IAttributeList, IConnectionPoint, \ref vst3Communication
*/
[GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper)]
[Guid("936F033B-C6C0-47DB-BB08-82F813C1E613")]
partial interface IMessage
{
    /** Returns the message ID (for example "TextMessage"). */
    [PreserveSig] [return: MarshalAs(UnmanagedType.LPStr)] string getMessageID();

    /** Sets a message ID (for example "TextMessage"). */
    [PreserveSig] void setMessageID([MarshalAs(UnmanagedType.LPStr)] string id);

    /** Returns the attribute list associated to the message. */
    [PreserveSig] IAttributeList getAttributes();
}