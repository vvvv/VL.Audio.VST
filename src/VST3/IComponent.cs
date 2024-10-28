using System.Runtime.InteropServices.Marshalling;
using System.Runtime.InteropServices;

namespace VST3;

//------------------------------------------------------------------------
// IComponent Interface
//------------------------------------------------------------------------
/** Component base interface: Vst::IComponent
\ingroup vstIPlug vst300
- [plug imp]
- [released: 3.0.0]
- [mandatory]

This is the basic interface for a VST component and must always be supported.
It contains the common parts of any kind of processing class. The parts that
are specific to a media type are defined in a separate interface. An implementation
component must provide both the specific interface and IComponent.
\see IPluginBase
*/
[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("E831FF31-F2D5-4301-928E-BBEE25697802")]
partial interface IComponent : IPluginBase
{
    //------------------------------------------------------------------------
    /** Called before initializing the component to get information about the controller class. */
    void getControllerClassId(ref Guid classId);

    /** Called before 'initialize' to set the component usage (optional). See \ref IoModes */
    void setIoMode(IoModes mode);

    /** Called after the plug-in is initialized. See \ref MediaTypes, BusDirections */
    [PreserveSig] int getBusCount(MediaTypes type, BusDirections dir);

    /** Called after the plug-in is initialized. See \ref MediaTypes, BusDirections */
    BusInfo getBusInfo(MediaTypes type, BusDirections dir, int index);

    /** Retrieves routing information (to be implemented when more than one regular input or output bus exists).
	    The inInfo always refers to an input bus while the returned outInfo must refer to an output bus! */
    void getRoutingInfo(in RoutingInfo inInfo, out RoutingInfo outInfo /*out*/);

    /** Called upon (de-)activating a bus in the host application. The plug-in should only processed
	   an activated bus, the host could provide less see \ref AudioBusBuffers in the process call
	   (see \ref IAudioProcessor::process) if last busses are not activated. An already activated bus 
	   does not need to be reactivated after a IAudioProcessor::setBusArrangements call. */
    void activateBus(MediaTypes type, BusDirections dir, int index, [MarshalAs(UnmanagedType.U1)] bool state);

    /** Activates / deactivates the component. */
    void setActive([MarshalAs(UnmanagedType.U1)] bool state);

    /** Sets complete state of component. */
    void setState(IBStream state);

    /** Retrieves complete state of component. */
    void getState(IBStream state);
}
