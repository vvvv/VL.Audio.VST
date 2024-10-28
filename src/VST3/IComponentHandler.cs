using System.Runtime.InteropServices.Marshalling;
using System.Runtime.InteropServices;

namespace VST3;

//------------------------------------------------------------------------
/** Host callback interface for an edit controller: Vst::IComponentHandler
\ingroup vstIHost vst300
- [host imp]
- [released: 3.0.0]
- [mandatory]

Allow transfer of parameter editing to component (processor) via host and support automation.
Cause the host to react on configuration changes (restartComponent).

\see \ref IEditController
*/
[GeneratedComInterface]
[Guid("93A0BEA3-0BD0-45DB-8E89-0B0CC1E46AC6")]
partial interface IComponentHandler
{
    /** To be called before calling a performEdit (e.g. on mouse-click-down event).
	This must be called in the UI-Thread context!  */
    void beginEdit(ParamID id);

    /** Called between beginEdit and endEdit to inform the handler that a given parameter has a new
	 * value. This must be called in the UI-Thread context! */
    void performEdit(ParamID id, double valueNormalized);

    /** To be called after calling a performEdit (e.g. on mouse-click-up event).
	This must be called in the UI-Thread context! */
    void endEdit(ParamID id);

    /** Instructs host to restart the component. This must be called in the UI-Thread context!
	\param flags is a combination of RestartFlags */
    void restartComponent(RestartFlags flags);
};
