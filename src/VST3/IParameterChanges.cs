using System.Runtime.InteropServices.Marshalling;
using System.Runtime.InteropServices;

namespace VST3;

//----------------------------------------------------------------------
/** All parameter changes of a processing block: Vst::IParameterChanges
\ingroup vstIHost vst300
- [host imp]
- [released: 3.0.0]
- [mandatory]

This interface is used to transmit any changes to be applied to parameters
in the current processing block. A change can be caused by GUI interaction as
well as automation. They are transmitted as a list of queues (\ref IParamValueQueue)
containing only queues for parameters that actually did change.
See \ref IParamValueQueue, \ref ProcessData
*/
[GeneratedComInterface]
[Guid("A4779663-0BB6-4A56-B443-84A8466FEB9D")]
partial interface IParameterChanges
{
    public static readonly Guid Guid = typeof(IParameterChanges).GUID;

	/** Returns count of Parameter changes in the list. */
	[PreserveSig] int getParameterCount();

	/** Returns the queue at a given index. */
	[PreserveSig] IParamValueQueue? getParameterData(int index);

    /** Adds a new parameter queue with a given ID at the end of the list,
	returns it and its index in the parameter changes list. */
    [PreserveSig] IParamValueQueue addParameterData(in ParamID id, out int index /*out*/);
};
