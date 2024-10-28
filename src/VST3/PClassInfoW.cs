using System.Runtime.InteropServices.Marshalling;

namespace VST3;

//------------------------------------------------------------------------
/** Unicode Version of Basic Information about a class provided by the plug-in
*/
unsafe struct PClassInfoW
{
    /** Class ID 16 Byte class GUID */
    Guid cid;

    /** Cardinality of the class, set to kManyInstances (see \ref PClassInfo::ClassCardinality) */
    int cardinality;

    /** Class category, host uses this to categorize interfaces */
    fixed byte category[PClassInfo.kCategorySize];

    /** Class name, visible to the user */
    fixed ushort name[PClassInfo.kNameSize];

    const int kVendorSize = 64;
    const int kVersionSize = 64;
    const int kSubCategoriesSize = 128;

    /** flags used for a specific category, must be defined where category is defined */
    uint classFlags;

    /** module specific subcategories, can be more than one, logically added by the OR operator */
    fixed byte subCategories[kSubCategoriesSize];

    /** overwrite vendor information from factory info */
    fixed ushort vendor[kVendorSize];

    /** Version string (e.g. "1.0.0.512" with Major.Minor.Subversion.Build) */
    fixed ushort version[kVersionSize];

    /** SDK version used to build this class (e.g. "VST 3.0") */
    fixed ushort sdkVersion[kVersionSize];

    public Guid Cid => cid;

    public int Cardinality => cardinality;

    public uint ClassFlags => classFlags;

    public string? Name
    {
        get
        {
            fixed (ushort* s = this.name)
                return Utf16StringMarshaller.ConvertToManaged(s);
        }
    }

    public string? Category
    {
        get
        {
            fixed (byte* s = this.category)
                return AnsiStringMarshaller.ConvertToManaged(s);
        }
    }

    public string? SubCategories
    {
        get
        {
            fixed (byte* s = this.subCategories)
                return AnsiStringMarshaller.ConvertToManaged(s);
        }
    }

    public string? Vendor
    {
        get
        {
            fixed (ushort* s = this.vendor)
                return Utf16StringMarshaller.ConvertToManaged(s);
        }
    }

    public string? Version
    {
        get
        {
            fixed (ushort* s = this.version)
                return Utf16StringMarshaller.ConvertToManaged(s);
        }
    }

    public string? SdkVersion
    {
        get
        {
            fixed (ushort* s = this.sdkVersion)
                return Utf16StringMarshaller.ConvertToManaged(s);
        }
    }
};
