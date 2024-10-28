using System.Runtime.InteropServices.Marshalling;

namespace VST3;

//------------------------------------------------------------------------
/**  Basic Information about a class provided by the plug-in.
\ingroup pluginBase
*/
unsafe struct PClassInfo
{
    //------------------------------------------------------------------------
    enum ClassCardinality
    {
        kManyInstances = 0x7FFFFFFF
    }

    internal const int kCategorySize = 32;
    internal const int kNameSize = 64;

    //------------------------------------------------------------------------
    /** Class ID 16 Byte class GUID */
    Guid cid;

    /** Cardinality of the class, set to kManyInstances (see \ref PClassInfo::ClassCardinality) */
    int cardinality;

    /** Class category, host uses this to categorize interfaces */
    fixed byte category[kCategorySize];

    /** Class name, visible to the user */
    fixed byte name[kNameSize];

    public Guid Cid => cid;

    public int Cardinality => cardinality;


    public string? Name
    {
        get
        {
            fixed (byte* name = this.name)
                return AnsiStringMarshaller.ConvertToManaged(name);
        }
    }

    public string? Category
    {
        get
        {
            fixed (byte* category = this.category)
                return AnsiStringMarshaller.ConvertToManaged(category);
        }
    }
};
