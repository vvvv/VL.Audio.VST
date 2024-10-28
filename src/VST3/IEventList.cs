using System.Runtime.InteropServices.Marshalling;
using System.Runtime.InteropServices;

namespace VST3;

//------------------------------------------------------------------------
/** List of events to process: Vst::IEventList
\ingroup vstIHost vst300
- [host imp]
- [released: 3.0.0]
- [mandatory]

\see ProcessData, Event
*/
[GeneratedComInterface]
[Guid("3A2C4214-3463-49FE-B2C4-F397B9695A44")]
partial interface IEventList
{
    public static readonly Guid Guid = typeof(IEventList).GUID;

    /** Returns the count of events. */
    [PreserveSig] int getEventCount();

	/** Gets parameter by index. */
	Event getEvent(int index);

	/** Adds a new event. */
	void addEvent(in Event e /*in*/);
};
