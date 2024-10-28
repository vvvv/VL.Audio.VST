using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace VST3;

/// <summary>
/// MIDI Mapping interface: Vst::IMidiMapping
/// \ingroup vstIPlug vst301
/// - [plug imp]
/// - [extends IEditController]
/// - [released: 3.0.1]
/// - [optional]
///
/// MIDI controllers are not transmitted directly to a VST component. MIDI as hardware protocol has
/// restrictions that can be avoided in software. Controller data in particular come along with unclear
/// and often ignored semantics. On top of this they can interfere with regular parameter automation and
/// the host is unaware of what happens in the plug-in when passing MIDI controllers directly.
///
/// So any functionality that is to be controlled by MIDI controllers must be exported as regular parameter.
/// The host will transform incoming MIDI controller data using this interface and transmit them as regular
/// parameter change. This allows the host to automate them in the same way as other parameters.
/// CtrlNumber can be a typical MIDI controller value extended to some others values like pitchbend or
/// aftertouch (see \ref ControllerNumbers).
/// If the mapping has changed, the plug-in must call IComponentHandler::restartComponent (kMidiCCAssignmentChanged)
/// to inform the host about this change.
/// </summary>
/// <example>
/// <code>
/// //--------------------------------------
/// // in myeditcontroller.h
/// class MyEditController: public EditControllerEx1, public IMidiMapping
/// {
///     //...
///     //---IMidiMapping---------------------------
///     tresult PLUGIN_API getMidiControllerAssignment (int32 busIndex, int16 channel, CtrlNumber midiControllerNumber, ParamID&amp; id) SMTG_OVERRIDE;
///     //---Interface---------
///     OBJ_METHODS (MyEditController, EditControllerEx1)
///     DEFINE_INTERFACES
///         DEF_INTERFACE (IMidiMapping)
///     END_DEFINE_INTERFACES (MyEditController)
///     REFCOUNT_METHODS (MyEditController)
/// };
///
/// //--------------------------------------
/// // in myeditcontroller.cpp
/// tresult PLUGIN_API MyEditController::getMidiControllerAssignment (int32 busIndex, int16 midiChannel, CtrlNumber midiControllerNumber, ParamID&amp; tag)
/// {
///     // for my first Event bus and for MIDI channel 0 and for MIDI CC Volume only
///     if (busIndex == 0 &amp;&amp; midiChannel == 0 &amp;&amp; midiControllerNumber == kCtrlVolume)
///     {
///         tag = kGainId;
///         return kResultTrue;
///     }
///     return kResultFalse;
/// }
/// </code>
/// </example>
[GeneratedComInterface]
[Guid("df0ff9f7-49b7-4669-b63a-b7327adbf5e5")]
partial interface IMidiMapping
{
    /// <summary>
    /// Gets an (preferred) associated ParamID for a given Input Event Bus index, channel and MIDI Controller.
    /// </summary>
    /// <param name="busIndex">index of Input Event Bus</param>
    /// <param name="channel">channel of the bus</param>
    /// <param name="midiControllerNumber">see \ref ControllerNumbers for expected values (could be bigger than 127)</param>
    /// <param name="id">return the associated ParamID to the given midiControllerNumber</param>
    /// <returns>True if the assignment is successful, otherwise false</returns>
    [return: MarshalAs(UnmanagedType.Bool)]
    bool getMidiControllerAssignment(int busIndex, short channel, ControllerNumbers midiControllerNumber, out ParamID id);
};
