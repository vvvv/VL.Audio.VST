namespace VST3;

/// <summary>
/// Flags used for <see cref="IComponentHandler.restartComponent"/>.
/// </summary>
[Flags]
public enum RestartFlags : int
{
    /// <summary>
    /// The Component should be reloaded.
    /// The host has to unload completely the plug-in (controller/processor) and reload it.
    /// [SDK 3.0.0]
    /// </summary>
    kReloadComponent = 1 << 0,

    /// <summary>
    /// Input / Output Bus configuration has changed.
    /// The plug-in informs the host that either the bus configuration or the bus count has changed.
    /// The host has to deactivate the plug-in, asks the plug-in for its wanted new bus configurations,
    /// adapts its processing graph and reactivate the plug-in.
    /// [SDK 3.0.0]
    /// </summary>
    kIoChanged = 1 << 1,

    /// <summary>
    /// Multiple parameter values have changed (as result of a program change for example).
    /// The host invalidates all caches of parameter values and asks the edit controller for the current values.
    /// [SDK 3.0.0]
    /// </summary>
    kParamValuesChanged = 1 << 2,

    /// <summary>
    /// Latency has changed.
    /// The plug informs the host that its latency has changed, getLatencySamples should return the new latency after setActive (true) was called.
    /// The host has to deactivate and reactivate the plug-in, then afterwards the host could ask for the current latency (getLatencySamples).
    /// See <see cref="IAudioProcessor.getLatencySamples"/>.
    /// [SDK 3.0.0]
    /// </summary>
    kLatencyChanged = 1 << 3,

    /// <summary>
    /// Parameter titles, default values or flags (ParameterFlags) have changed.
    /// The host invalidates all caches of parameter infos and asks the edit controller for the current infos.
    /// [SDK 3.0.0]
    /// </summary>
    kParamTitlesChanged = 1 << 4,

    /// <summary>
    /// MIDI Controllers and/or Program Changes Assignments have changed.
    /// The plug-in informs the host that its MIDI-CC mapping has changed (for example after a MIDI learn or new loaded preset)
    /// or if the stepCount or UnitID of a ProgramChange parameter has changed.
    /// The host has to rebuild the MIDI-CC => parameter mapping (getMidiControllerAssignment)
    /// and reread program changes parameters (stepCount and associated unitID).
    /// [SDK 3.0.1]
    /// </summary>
    kMidiCCAssignmentChanged = 1 << 5,

    /// <summary>
    /// Note Expression has changed (info, count, PhysicalUIMapping, ...).
    /// Either the note expression type info, the count of note expressions or the physical UI mapping has changed.
    /// The host invalidates all caches of note expression infos and asks the edit controller for the current ones.
    /// See <see cref="INoteExpressionController"/>, NoteExpressionTypeInfo and <see cref="INoteExpressionPhysicalUIMapping"/>.
    /// [SDK 3.5.0]
    /// </summary>
    kNoteExpressionChanged = 1 << 6,

    /// <summary>
    /// Input / Output bus titles have changed.
    /// The host invalidates all caches of bus titles and asks the edit controller for the current titles.
    /// [SDK 3.5.0]
    /// </summary>
    kIoTitlesChanged = 1 << 7,

    /// <summary>
    /// Prefetch support has changed.
    /// The plug-in informs the host that its PrefetchSupport has changed.
    /// The host has to deactivate the plug-in, calls <see cref="IPrefetchableSupport.getPrefetchableSupport"/>
    /// and reactivate the plug-in.
    /// See <see cref="IPrefetchableSupport"/>.
    /// [SDK 3.6.1]
    /// </summary>
    kPrefetchableSupportChanged = 1 << 8,

    /// <summary>
    /// RoutingInfo has changed.
    /// The plug-in informs the host that its internal routing (relation of an event-input-channel to
    /// an audio-output-bus) has changed. The host asks the plug-in for the new routing with 
    /// <see cref="IComponent.getRoutingInfo"/>, \ref vst3Routing.
    /// See <see cref="IComponent"/>.
    /// [SDK 3.6.6]
    /// </summary>
    kRoutingInfoChanged = 1 << 9,

    /// <summary>
    /// Key switches has changed (info, count).
    /// Either the Key switches info, the count of Key switches has changed.
    /// The host invalidates all caches of Key switches infos and asks the edit controller
    /// (<see cref="IKeyswitchController"/>) for the current ones.
    /// See <see cref="IKeyswitchController"/>.
    /// [SDK 3.7.3]
    /// </summary>
    kKeyswitchChanged = 1 << 10,

    /// <summary>
    /// Mapping of ParamID has changed.
    /// The Plug-in informs the host that its parameters ID has changed. This has to be called by the
    /// edit controller in the method setComponentState or setState (during projects loading) when the
    /// plug-in detects that the given state was associated to an older version of the plug-in, or to a
    /// plug-in to replace (for ex. migrating VST2 => VST3), with a different set of parameter IDs, then
    /// the host could remap any used parameters like automation by asking the <see cref="IRemapParamID"/> interface
    /// (which extends <see cref="IEditController"/>).
    /// See <see cref="IRemapParamID"/>.
    /// [SDK 3.7.11]
    /// </summary>
    kParamIDMappingChanged = 1 << 11
}
