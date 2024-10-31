using System.Runtime.InteropServices.Marshalling;
using System.Runtime.InteropServices;

namespace VST3;

//------------------------------------------------------------------------
/**  Plug-in definition of a view.
\ingroup pluginGUI vstIPlug vst300
- [plug imp]
- [released: 3.0.0]

\par Sizing of a view
Usually, the size of a plug-in view is fixed. But both the host and the plug-in can cause
a view to be resized:
\n
- \b Host: If IPlugView::canResize () returns kResultTrue the host will set up the window
  so that the user can resize it. While the user resizes the window,
  IPlugView::checkSizeConstraint () is called, allowing the plug-in to change the size to a valid
  a valid supported rectangle size. The host then resizes the window to this rect and has to call IPlugView::onSize ().
\n
\n
- \b Plug-in: The plug-in can call IPlugFrame::resizeView () and cause the host to resize the
  window.\n\n
  Afterwards, in the same callstack, the host has to call IPlugView::onSize () if a resize is needed (size was changed).
  Note that if the host calls IPlugView::getSize () before calling IPlugView::onSize () (if needed),
  it will get the current (old) size not the wanted one!!\n
  Here the calling sequence:\n
    - plug-in->host: IPlugFrame::resizeView (newSize)
	- host->plug-in (optional): IPlugView::getSize () returns the currentSize (not the newSize!)
	- host->plug-in: if newSize is different from the current size: IPlugView::onSize (newSize)
    - host->plug-in (optional): IPlugView::getSize () returns the newSize
\n
<b>Please only resize the platform representation of the view when IPlugView::onSize () is
called.</b>

\par Keyboard handling
The plug-in view receives keyboard events from the host. A view implementation must not handle
keyboard events by the means of platform callbacks, but let the host pass them to the view. The host
depends on a proper return value when IPlugView::onKeyDown is called, otherwise the plug-in view may
cause a malfunction of the host's key command handling.

\see IPlugFrame, \ref platformUIType
*/
[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("5BC32507-D060-49EA-A615-1B522B755B29")]
partial interface IPlugView
{
    //------------------------------------------------------------------------
    /** Is Platform UI Type supported
	    \param type : IDString of \ref platformUIType */
    [PreserveSig][return: MarshalAs(UnmanagedType.Error)] int isPlatformTypeSupported([MarshalAs(UnmanagedType.LPStr)] string type);

    /** The parent window of the view has been created, the (platform) representation of the view
		should now be created as well.
	    Note that the parent is owned by the caller and you are not allowed to alter it in any way
		other than adding your own views.
	    Note that in this call the plug-in could call a IPlugFrame::resizeView ()!
	    \param parent : platform handle of the parent window or view
	    \param type : \ref platformUIType which should be created */
    void attached(IntPtr parent, [MarshalAs(UnmanagedType.LPStr)] string type);

    /** The parent window of the view is about to be destroyed.
	    You have to remove all your own views from the parent window or view. */
    void removed();

    /** Handling of mouse wheel. */
    void onWheel(float distance);

    /** Handling of keyboard events : Key Down.
	    \param key : unicode code of key
	    \param keyCode : virtual keycode for non ascii keys - see \ref VirtualKeyCodes in keycodes.h
	    \param modifiers : any combination of modifiers - see \ref KeyModifier in keycodes.h
	    \return kResultTrue if the key is handled, otherwise kResultFalse. \n
	            <b> Please note that kResultTrue must only be returned if the key has really been
	   handled. </b> Otherwise key command handling of the host might be blocked! */
    void onKeyDown(char key, short keyCode, short modifiers);

    /** Handling of keyboard events : Key Up.
	    \param key : unicode code of key
	    \param keyCode : virtual keycode for non ascii keys - see \ref VirtualKeyCodes in keycodes.h
	    \param modifiers : any combination of KeyModifier - see \ref KeyModifier in keycodes.h
	    \return kResultTrue if the key is handled, otherwise return kResultFalse. */
    void onKeyUp(char key, short keyCode, short modifiers);

    /** Returns the size of the platform representation of the view. */
    ViewRect getSize();

    /** Resizes the platform representation of the view to the given rect. Note that if the plug-in
	 *	requests a resize (IPlugFrame::resizeView ()) onSize has to be called afterward. */
    void onSize(in ViewRect newSize);

    /** Focus changed message. */
    void onFocus([MarshalAs(UnmanagedType.U1)] bool state);

    /** Sets IPlugFrame object to allow the plug-in to inform the host about resizing. */
    void setFrame(IPlugFrame frame);

    /** Is view sizable by user. */
    [PreserveSig][return: MarshalUsing(typeof(VstBoolMarshaller))] bool canResize();

    /** On live resize this is called to check if the view can be resized to the given rect, if not
	 *	adjust the rect to the allowed size. */
    [PreserveSig][return: MarshalUsing(typeof(VstBoolMarshaller))] bool checkSizeConstraint(ref ViewRect rect);
};
