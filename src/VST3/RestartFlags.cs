namespace VST3;

//------------------------------------------------------------------------
/** Flags used for IComponentHandler::restartComponent */
[Flags]
enum RestartFlags : int
{
    /** The Component should be reloaded
	 *	The host has to unload completely the plug-in (controller/processor) and reload it.
	 *	[SDK 3.0.0] */
    kReloadComponent = 1 << 0,

    /** Input / Output Bus configuration has changed
	 *	The plug-in informs the host that either the bus configuration or the bus count has changed.
	 *  The host has to deactivate the plug-in, asks the plug-in for its wanted new bus configurations,
	 *  adapts its processing graph and reactivate the plug-in.
	 *	[SDK 3.0.0] */
    kIoChanged = 1 << 1,

    /** Multiple parameter values have changed  (as result of a program change for example)
	 *	The host invalidates all caches of parameter values and asks the edit controller for the current values.
	 *	[SDK 3.0.0] */
    kParamValuesChanged = 1 << 2,

    /** Latency has changed
	 *  The plug informs the host that its latency has changed, getLatencySamples should return the new latency after setActive (true) was called
	 *  The host has to deactivate and reactivate the plug-in, then afterwards the host could ask for the current latency (getLatencySamples)
	 *	See IAudioProcessor::getLatencySamples
	 *	[SDK 3.0.0] */
    kLatencyChanged = 1 << 3,

    /** Parameter titles, default values or flags (ParameterFlags) have changed
	 *	The host invalidates all caches of parameter infos and asks the edit controller for the current infos.
	 *	[SDK 3.0.0] */
    kParamTitlesChanged = 1 << 4,

    /** MIDI Controllers and/or Program Changes Assignments have changed
	 *	The plug-in informs the host that its MIDI-CC mapping has changed (for example after a MIDI learn or new loaded preset) 
	 *  or if the stepCount or UnitID of a ProgramChange parameter has changed.
	 *  The host has to rebuild the MIDI-CC => parameter mapping (getMidiControllerAssignment)
	 *  and reread program changes parameters (stepCount and associated unitID)
	 *	[SDK 3.0.1] */
    kMidiCCAssignmentChanged = 1 << 5,

    /** Note Expression has changed (info, count, PhysicalUIMapping, ...)
	 *  Either the note expression type info, the count of note expressions or the physical UI mapping has changed.
	 *	The host invalidates all caches of note expression infos and asks the edit controller for the current ones.
	 *  See INoteExpressionController, NoteExpressionTypeInfo and INoteExpressionPhysicalUIMapping
	 *	[SDK 3.5.0] */
    kNoteExpressionChanged = 1 << 6,

    /** Input / Output bus titles have changed
	 *	The host invalidates all caches of bus titles and asks the edit controller for the current titles.
	 *	[SDK 3.5.0] */
    kIoTitlesChanged = 1 << 7,

    /** Prefetch support has changed
	 *	The plug-in informs the host that its PrefetchSupport has changed
	 *  The host has to deactivate the plug-in, calls IPrefetchableSupport::getPrefetchableSupport 
	 *  and reactivate the plug-in.
	 *	See IPrefetchableSupport
	 *	[SDK 3.6.1] */
    kPrefetchableSupportChanged = 1 << 8,

    /** RoutingInfo has changed
	 *	The plug-in informs the host that its internal routing (relation of an event-input-channel to
	 *  an audio-output-bus) has changed. The host asks the plug-in for the new routing with 
	 *  IComponent::getRoutingInfo, \ref vst3Routing
	 *	See IComponent
	 *	[SDK 3.6.6] */
    kRoutingInfoChanged = 1 << 9,

    /** Key switches has changed (info, count)
	 *  Either the Key switches info, the count of Key switches has changed.
	 *	The host invalidates all caches of Key switches infos and asks the edit controller
	 *  (IKeyswitchController) for the current ones.
	 *  See IKeyswitchController
	 *	[SDK 3.7.3] */
    kKeyswitchChanged = 1 << 10,

    /** Mapping of ParamID has changed
	 *  The Plug-in informs the host that its parameters ID has changed. This has to be called by the
	 *  edit controller in the method setComponentState or setState (during projects loading) when the
	 *  plug-in detects that the given state was associated to an older version of the plug-in, or to a
	 *  plug-in to replace (for ex. migrating VST2 => VST3), with a different set of parameter IDs, then
	 *  the host could remap any used parameters like automation by asking the IRemapParamID interface
	 *  (which extends IEditController).
	 *  See IRemapParamID
	 *  [SDK 3.7.11] */
    kParamIDMappingChanged = 1 << 11
};
