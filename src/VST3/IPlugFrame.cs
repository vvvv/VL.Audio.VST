using System.Runtime.InteropServices.Marshalling;
using System.Runtime.InteropServices;

namespace VST3;

//------------------------------------------------------------------------
/** Callback interface passed to IPlugView.
\ingroup pluginGUI vstIHost vst300
- [host imp]
- [released: 3.0.0]
- [mandatory]

Enables a plug-in to resize the view and cause the host to resize the window.
*/
[GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper)]
[Guid("367FAF01-AFA9-4693-8D4D-A2A0ED0882A3")]
partial interface IPlugFrame
{
    //------------------------------------------------------------------------
    /** Called to inform the host about the resize of a given view.
	 *	Afterwards the host has to call IPlugView::onSize (). */
    void resizeView([MarshalUsing(typeof(VstInterfaceMarshaller<IPlugView>))] IPlugView view, in ViewRect newSize);
};
