using System.Runtime.InteropServices.Marshalling;
using System.Runtime.InteropServices;

namespace VST3;

/// <summary>
/// Extension for IPlugView to find view parameters (lookup value under mouse support): Vst::IParameterFinder
/// </summary>
/// <remarks>
/// <para>
/// It is highly recommended to implement this interface.
/// A host can implement important functionality when a plug-in supports this interface.
/// </para>
/// <para>
/// For example, all Steinberg hosts require this interface in order to support the "AI Knob".
/// </para>
/// </remarks>
/// <seealso cref="IPlugView"/>
[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("0F618302-215D-4587-A512-073C77B9D383")]
partial interface IParameterFinder
{
    /// <summary>
    /// Find out which parameter in plug-in view is at given position (relative to plug-in view).
    /// </summary>
    /// <param name="xPos">The x position relative to the plug-in view.</param>
    /// <param name="yPos">The y position relative to the plug-in view.</param>
    /// <param name="resultTag">The resulting parameter ID.</param>
    /// <returns>True if a parameter is found at the given position, otherwise false.</returns>
    [PreserveSig]
    [return: MarshalUsing(typeof(VstBoolMarshaller))]
    bool findParameter(int xPos, int yPos, out ParamID resultTag);
}
