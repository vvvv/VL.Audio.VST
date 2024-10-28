using System.Collections.Immutable;

namespace VST3.Hosting;

internal record ClassInfo(
    Guid ID,
    int Cardinality,
    string Category,
    string Name,
    string Vendor,
    string Version,
    string SdkVersion,
    ImmutableArray<string> SubCategories,
    uint ClassFlags = 0
    )
{
    public const string VstAudioEffectClass = "Audio Module Class";

    public static implicit operator ClassInfo(PClassInfo info)
    {
        return new ClassInfo(
            info.Cid,
            info.Cardinality,
            info.Category ?? string.Empty,
            info.Name ?? string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            ImmutableArray<string>.Empty);
    }

    public static implicit operator ClassInfo(PClassInfo2 info)
    {
        return new ClassInfo(
            info.Cid,
            info.Cardinality,
            info.Category ?? string.Empty,
            info.Name ?? string.Empty,
            info.Vendor ?? string.Empty,
            info.Version ?? string.Empty,
            info.SdkVersion ?? string.Empty,
            ParseSubCategories(info.SubCategories ?? string.Empty),
            info.ClassFlags);
    }

    public static implicit operator ClassInfo(PClassInfoW info)
    {
        return new ClassInfo(
            info.Cid, 
            info.Cardinality, 
            info.Category ?? string.Empty, 
            info.Name ?? string.Empty, 
            info.Vendor ?? string.Empty, 
            info.Version ?? string.Empty, 
            info.SdkVersion ?? string.Empty, 
            ParseSubCategories(info.SubCategories ?? string.Empty), 
            info.ClassFlags);
    }

    private static ImmutableArray<string> ParseSubCategories(string str)
    {
        return str.Split('|').ToImmutableArray();
    }
}
