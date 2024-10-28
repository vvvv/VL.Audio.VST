namespace VST3.Hosting;

internal record FactoryInfo(
    string Vendor,
    string Url,
    string Email,
    PFactoryInfo.FactoryFlags Flags)
{
    public bool ClassesDiscardable => Flags.HasFlag(PFactoryInfo.FactoryFlags.kClassesDiscardable);

    public bool LicenseCheck => Flags.HasFlag(PFactoryInfo.FactoryFlags.kLicenseCheck);

    public bool ComponentNonDiscardable => Flags.HasFlag(PFactoryInfo.FactoryFlags.kComponentNonDiscardable);

    public static implicit operator FactoryInfo(PFactoryInfo info)
    {
        return new FactoryInfo(info.Vendor ?? string.Empty, info.Url ?? string.Empty, info.Email ?? string.Empty, info.Flags);
    }
}
