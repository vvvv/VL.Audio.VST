using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace VST3;

/// <summary>
/// MIDI Learn interface: Vst::IMidiLearn
/// </summary>
/// <remarks>
/// <para>
/// If this interface is implemented by the edit controller, the host will call this method whenever
/// there is live MIDI-CC input for the plug-in. This way, the plug-in can change its MIDI-CC parameter
/// mapping and inform the host via the IComponentHandler::restartComponent with the
/// kMidiCCAssignmentChanged flag.
/// </para>
/// <para>
/// Use this if you want to implement custom MIDI-Learn functionality in your plug-in.
/// </para>
/// <example>
/// <code>
/// //------------------------------------------------
/// // in MyController class declaration
/// class MyController : public Vst::EditController, public Vst::IMidiLearn
/// {
///     // ...
///     //--- IMidiLearn ---------------------------------
///     tresult PLUGIN_API onLiveMIDIControllerInput (int32 busIndex, int16 channel,
///                                                   CtrlNumber midiCC) SMTG_OVERRIDE;
///     // ...
///
///     OBJ_METHODS (MyController, Vst::EditController)
///     DEFINE_INTERFACES
///         // ...
///         DEF_INTERFACE (Vst::IMidiLearn)
///     END_DEFINE_INTERFACES (Vst::EditController)
///     //...
/// }
///
/// //------------------------------------------------
/// // in mycontroller.cpp
/// #include "pluginterfaces/vst/ivstmidilearn.h"
///
/// namespace Steinberg {
///     namespace Vst {
///         DEF_CLASS_IID (IMidiLearn)
///     }
/// }
///
/// //------------------------------------------------------------------------
/// tresult PLUGIN_API MyController::onLiveMIDIControllerInput (int32 busIndex, 
///                         int16 channel, CtrlNumber midiCC)
/// {
///     // if we are not in doMIDILearn (triggered by a UI button for example) 
///     // or wrong channel then return
///     if (!doMIDILearn || busIndex != 0 || channel != 0 || midiLearnParamID == InvalidParamID)
///         return kResultFalse;
///
///     // adapt our internal MIDICC -> parameterID mapping
///     midiCCMapping[midiCC] = midiLearnParamID;
///
///     // new mapping then inform the host that our MIDI assignment has changed
///     if (auto componentHandler = getComponentHandler ())
///     {
///         componentHandler->restartComponent (kMidiCCAssignmentChanged);
///     }
///     return kResultTrue;
/// }
/// </code>
/// </example>
/// </remarks>
[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("6b2449cc-4197-40b5-ab3c-79dac5fe5c86")]
partial interface IMidiLearn
{
    /// <summary>
    /// Called on live input MIDI-CC change associated to a given bus index and MIDI channel.
    /// </summary>
    /// <param name="busIndex">The bus index.</param>
    /// <param name="channel">The MIDI channel.</param>
    /// <param name="midiCC">The MIDI control change number.</param>
    [PreserveSig]
    [return: MarshalUsing(typeof(VstBoolMarshaller))]
    bool onLiveMIDIControllerInput(int busIndex, short channel, ControllerNumbers midiCC);
}
