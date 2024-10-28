using System.Runtime.InteropServices.Marshalling;
using System.Runtime.InteropServices;

namespace VST3;

//------------------------------------------------------------------------
/** Edit controller component interface: Vst::IEditController
\ingroup vstIPlug vst300
- [plug imp]
- [released: 3.0.0]
- [mandatory]

The controller part of an effect or instrument with parameter handling (export, definition, conversion...).
\see \ref IComponent::getControllerClassId, \ref IMidiMapping
*/
[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("DCD7BBE3-7742-448D-A874-AACC979C759E")]
partial interface IEditController : IPluginBase
{
    /** Receives the component state. */
    void setComponentState(IBStream state);

    /** Sets the controller state. */
    void setState(IBStream state);

    /** Gets the controller state. */
    void getState(IBStream state);

    // parameters -------------------------
    /** Returns the number of parameters exported. */
    [PreserveSig] int getParameterCount();
    /** Gets for a given index the parameter information. */
    ParameterInfo getParameterInfo(int paramIndex);

    /** Gets for a given paramID and normalized value its associated string representation. */
    String128 getParamStringByValue(ParamID id, double valueNormalized /*in*/);
    /** Gets for a given paramID and string its normalized value. */
    void getParamValueByString(ParamID id, [MarshalAs(UnmanagedType.LPWStr)] string @string /*in*/, out double valueNormalized /*out*/);

    /** Returns for a given paramID and a normalized value its plain representation
		(for example -6 for -6dB - see \ref vst3AutomationIntro). */
    [PreserveSig] double normalizedParamToPlain(ParamID id, double valueNormalized);
    /** Returns for a given paramID and a plain value its normalized value. (see \ref vst3AutomationIntro) */
    [PreserveSig] double plainParamToNormalized(ParamID id, double plainValue);

    /** Returns the normalized value of the parameter associated to the paramID. */
    [PreserveSig] double getParamNormalized(ParamID id);
    /** Sets the normalized value to the parameter associated to the paramID. The controller must never
	    pass this value-change back to the host via the IComponentHandler. It should update the according
		GUI element(s) only!*/
    void setParamNormalized(ParamID id, double value);

    // handler ----------------------------
    /** Gets from host a handler which allows the Plugin-in to communicate with the host.
		Note: This is mandatory if the host is using the IEditController! */
    void setComponentHandler(IComponentHandler handler);

    // view -------------------------------
    /** Creates the editor view of the plug-in, currently only "editor" is supported, see \ref ViewType.
		The life time of the editor view will never exceed the life time of this controller instance. */
    [return:MarshalUsing(typeof(VstInterfaceMarshaller<IPlugView>))] [PreserveSig] IPlugView createView([MarshalAs(UnmanagedType.LPStr)] string name);
};
