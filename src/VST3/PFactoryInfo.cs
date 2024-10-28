using System.Runtime.InteropServices.Marshalling;

namespace VST3;

//------------------------------------------------------------------------
/** Basic Information about the class factory of the plug-in.
\ingroup pluginBase
*/
unsafe struct PFactoryInfo
{
    public enum FactoryFlags : int
    {
        /** Nothing */
        kNoFlags = 0,

        /// <summary>
        /// The number of exported classes can change each time the Module is loaded. If this flag
		/// is set, the host does not cache class information. This leads to a longer startup time
        /// because the host always has to load the Module to get the current class information.
        /// </summary>
        kClassesDiscardable = 1 << 0,

        /// <summary>
        /// This flag is deprecated, do not use anymore, resp. it will get ignored from
        /// Cubase/Nuendo 12 and later.
        /// </summary>
        kLicenseCheck = 1 << 1,

        /// <summary>
        /// Component will not be unloaded until process exit
        /// </summary>
        kComponentNonDiscardable = 1 << 3,

        /// <summary>
        /// Components have entirely unicode encoded strings (True for VST 3 plug-ins so far).
        /// </summary>
        kUnicode = 1 << 4
    };

    internal const int kURLSize = 256;
    internal const int kEmailSize = 128;
    internal const int kNameSize = 64;

    fixed byte vendor[kNameSize];	/// e.g. "Steinberg Media Technologies"
	fixed byte url[kURLSize];		/// e.g. "http://www.steinberg.de"
	fixed byte email[kEmailSize];	/// e.g. "info@steinberg.de"
	FactoryFlags flags;

    public string? Vendor
    {
        get
        {
            fixed (byte* s = this.vendor)
                return AnsiStringMarshaller.ConvertToManaged(s);
        }
    }

    public string? Url
    {
        get
        {
            fixed (byte* s = this.url)
                return AnsiStringMarshaller.ConvertToManaged(s);
        }
    }

    public string? Email
    {
        get
        {
            fixed (byte* s = this.email)
                return AnsiStringMarshaller.ConvertToManaged(s);
        }
    }

    public FactoryFlags Flags => flags;
}
