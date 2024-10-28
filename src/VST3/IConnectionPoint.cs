using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace VST3;

//------------------------------------------------------------------------
/** Connect a component with another one: Vst::IConnectionPoint
\ingroup vstIPlug vst300
- [plug imp]
- [host imp]
- [released: 3.0.0]
- [mandatory]

This interface is used for the communication of separate components.
Note that some hosts will place a proxy object between the components so that they are not directly connected.

\see \ref vst3Communication
*/
[GeneratedComInterface]
[Guid("70A4156F-6E6E-4026-9891-48BFAA60D8D1")]
partial interface IConnectionPoint
{
	/** Connects this instance with another connection point. */
	void connect(IConnectionPoint other);

	/** Disconnects a given connection point from this. */
	void disconnect(IConnectionPoint other);

	/** Called when a message has been sent from the connection point to this. */
	void notify(IMessage message);
}
