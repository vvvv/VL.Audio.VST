using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace VST3;

/// <summary>
/// Extended host callback interface for an edit controller: Vst::IComponentHandler2
/// </summary>
/// <remarks>
/// <para>One part handles:</para>
/// <list type="bullet">
/// <item>Setting dirty state of the plug-in</item>
/// <item>Requesting the host to open the editor</item>
/// </list>
/// <para>The other part handles parameter group editing from the plug-in UI. It wraps a set of <see cref="IComponentHandler.beginEdit"/>, <see cref="IComponentHandler.performEdit"/>, and <see cref="IComponentHandler.endEdit"/> functions which should use the same timestamp in the host when writing automation. This allows for better synchronizing of multiple parameter changes at once.</para>
/// </remarks>
/// <example>
/// <code>
/// // we are in the editcontroller...
/// // in case of multiple switch buttons (with associated ParamID 1 and 3)
/// // on mouse down :
/// hostHandler2.startGroupEdit();
/// hostHandler.beginEdit(1);
/// hostHandler.beginEdit(3);
/// hostHandler.performEdit(1, 1.0);
/// hostHandler.performEdit(3, 0.0); // the opposite of paramID 1 for example
/// // on mouse up :
/// hostHandler.endEdit(1);
/// hostHandler.endEdit(3);
/// hostHandler2.finishGroupEdit();
/// // in case of multiple faders (with associated ParamID 1 and 3)
/// // on mouse down :
/// hostHandler2.startGroupEdit();
/// hostHandler.beginEdit(1);
/// hostHandler.beginEdit(3);
/// hostHandler2.finishGroupEdit();
/// // on mouse move :
/// hostHandler2.startGroupEdit();
/// hostHandler.performEdit(1, x); // x the wanted value
/// hostHandler.performEdit(3, x);
/// hostHandler2.finishGroupEdit();
/// // on mouse up :
/// hostHandler2.startGroupEdit();
/// hostHandler.endEdit(1);
/// hostHandler.endEdit(3);
/// hostHandler2.finishGroupEdit();
/// </code>
/// </example>
/// <seealso cref="IComponentHandler"/>
/// <seealso cref="IEditController"/>
[GeneratedComInterface]
[Guid("F040B4B3-A360-45EC-ABCD-C045B4D5A2CC")]
partial interface IComponentHandler2
{
    /// <summary>
    /// Tells host that the plug-in is dirty (something besides parameters has changed since last save),
    /// if true the host should apply a save before quitting.
    /// </summary>
    /// <param name="state">The dirty state.</param>
    /// <returns>A result indicating success or failure.</returns>
    void setDirty([MarshalAs(UnmanagedType.U1)] bool state);

    /// <summary>
    /// Tells host that it should open the plug-in editor the next time it's possible.
    /// You should use this instead of showing an alert and blocking the program flow (especially on loading projects).
    /// </summary>
    /// <param name="name">The name of the view type.</param>
    /// <returns>A result indicating success or failure.</returns>
    void requestOpenEditor([MarshalAs(UnmanagedType.LPStr)] string name = "editor");

    /// <summary>
    /// Starts the group editing (call before a <see cref="IComponentHandler.beginEdit"/>),
    /// the host will keep the current timestamp at this call and will use it for all <see cref="IComponentHandler.beginEdit"/>,
    /// <see cref="IComponentHandler.performEdit"/>, and <see cref="IComponentHandler.endEdit"/> calls until a <see cref="FinishGroupEdit"/>.
    /// </summary>
    /// <returns>A result indicating success or failure.</returns>
    void startGroupEdit();

    /// <summary>
    /// Finishes the group editing started by a <see cref="startGroupEdit"/> (call after a <see cref="IComponentHandler.endEdit"/>).
    /// </summary>
    /// <returns>A result indicating success or failure.</returns>
    void finishGroupEdit();
}