using System.Runtime.InteropServices.Marshalling;
using System.Runtime.InteropServices;

namespace VST3;

/// <summary>
/// Host callback interface for an edit controller: Vst::IPlugInterfaceSupport
/// </summary>
/// <remarks>
/// <para>Allows a plug-in to ask the host if a given plug-in interface is supported/used by the host.</para>
/// <para>It is implemented by the hostContext given when the component is initialized.</para>
/// </remarks>
/// <example>
/// <code>
/// // Example usage:
/// // tresult PLUGIN_API MyPluginController::initialize (FUnknown* context)
/// // {
/// //     // ...
/// //     FUnknownPtr&lt;IPlugInterfaceSupport&gt; plugInterfaceSupport (context);
/// //     if (plugInterfaceSupport)
/// //     {
/// //         if (plugInterfaceSupport.isPlugInterfaceSupported (IMidiMapping.iid) == kResultTrue)
/// //             // IMidiMapping is used by the host
/// //     }
/// //     // ...
/// // }
/// </code>
/// </example>
/// <seealso cref="IPluginBase"/>
[GeneratedComInterface]
[Guid("4FB58B9E-9EAA-4E0F-AB36-1C1CCCB56FEA")]
partial interface IPlugInterfaceSupport
{
    /// <summary>
    /// Returns kResultTrue if the associated interface to the given _iid is supported/used by the host.
    /// </summary>
    /// <param name="_iid">The interface ID.</param>
    /// <returns>A result indicating success or failure.</returns>
    [PreserveSig] [return: MarshalUsing(typeof(VstBoolMarshaller))] bool isPlugInterfaceSupported(Guid _iid);
}