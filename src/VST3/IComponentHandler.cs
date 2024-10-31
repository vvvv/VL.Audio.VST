using System.Runtime.InteropServices.Marshalling;
using System.Runtime.InteropServices;

namespace VST3;

/// <summary>
/// Host callback interface for an edit controller: Vst::IComponentHandler
/// </summary>
/// <remarks>
/// <para>Allow transfer of parameter editing to component (processor) via host and support automation.</para>
/// <para>Cause the host to react on configuration changes (restartComponent).</para>
/// <para>See also: <see cref="IEditController"/></para>
/// </remarks>
[GeneratedComInterface]
[Guid("93A0BEA3-0BD0-45DB-8E89-0B0CC1E46AC6")]
partial interface IComponentHandler
{
    /// <summary>
    /// To be called before calling a performEdit (e.g. on mouse-click-down event).
    /// This must be called in the UI-Thread context!
    /// </summary>
    /// <param name="id">The parameter ID.</param>
    void beginEdit(ParamID id);

    /// <summary>
    /// Called between beginEdit and endEdit to inform the handler that a given parameter has a new value.
    /// This must be called in the UI-Thread context!
    /// </summary>
    /// <param name="id">The parameter ID.</param>
    /// <param name="valueNormalized">The new normalized value of the parameter.</param>
    void performEdit(ParamID id, double valueNormalized);

    /// <summary>
    /// To be called after calling a performEdit (e.g. on mouse-click-up event).
    /// This must be called in the UI-Thread context!
    /// </summary>
    /// <param name="id">The parameter ID.</param>
    void endEdit(ParamID id);

    /// <summary>
    /// Instructs host to restart the component. This must be called in the UI-Thread context!
    /// </summary>
    /// <param name="flags">A combination of <see cref="RestartFlags"/>.</param>
    void restartComponent(RestartFlags flags);
}
