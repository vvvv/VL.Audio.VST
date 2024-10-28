using System.Runtime.InteropServices.Marshalling;
using System.Runtime.InteropServices;

namespace VST3;

//------------------------------------------------------------------------
/**  Basic interface to a plug-in component: IPluginBase
\ingroup pluginBase
- [plug imp]
- initialize/terminate the plug-in component

The host uses this interface to initialize and to terminate the plug-in component.
The context that is passed to the initialize method contains any interface to the
host that the plug-in will need to work. These interfaces can vary from category to category.
A list of supported host context interfaces should be included in the documentation
of a specific category. 
*/
[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("22888DDB-156E-45AE-8358-B34808190625")]
partial interface IPluginBase
{
    //------------------------------------------------------------------------
    /** The host passes a number of interfaces as context to initialize the plug-in class.
		\param context, passed by the host, is mandatory and should implement IHostApplication
		@note Extensive memory allocations etc. should be performed in this method rather than in
	   the class' constructor! If the method does NOT return kResultOk, the object is released
	   immediately. In this case terminate is not called! */
    void initialize(IHostApplication context);

    /** This function is called before the plug-in is unloaded and can be used for
	    cleanups. You have to release all references to any host application interfaces. */
    void terminate();
};
