using System.Runtime.InteropServices.Marshalling;
using System.Runtime.InteropServices;

namespace VST3;

//------------------------------------------------------------------------
/** Plug-in view content scale support
\ingroup pluginGUI vstIPlug vst366
- [plug impl]
- [extends IPlugView]
- [released: 3.6.6]
- [optional]

This interface communicates the content scale factor from the host to the plug-in view on
systems where plug-ins cannot get this information directly like Microsoft Windows.

The host calls setContentScaleFactor directly before or after the plug-in view is attached and when
the scale factor changes while the view is attached (system change or window moved to another screen
with different scaling settings).

The host may call setContentScaleFactor in a different context, for example: scaling the plug-in
editor for better readability.

When a plug-in handles this (by returning kResultTrue), it needs to scale the width and height of
its view by the scale factor and inform the host via a IPlugFrame::resizeView(). The host will then
call IPlugView::onSize().

Note that the host is allowed to call setContentScaleFactor() at any time the IPlugView is valid.
If this happens before the IPlugFrame object is set on your view, make sure that when the host calls
IPlugView::getSize() afterwards you return the size of your view for that new scale factor.

It is recommended to implement this interface on Microsoft Windows to let the host know that the
plug-in is able to render in different scalings.
*/
[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("65ed9690-8ac4-4525-8aad-ef7a72ea703f")]
partial interface IPlugViewContentScaleSupport
{
    void setContentScaleFactor(float factor);
};
