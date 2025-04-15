namespace VL.Audio.VST.Internal;

internal record class EffectNodeInfo(string Name, string Category, string ModuleName, Guid Id)
{
    public static EffectNodeInfo Parse(string value)
    {
        var parts = value.Split('|');
        return new EffectNodeInfo(parts[0], parts[1], parts[3], Guid.Parse(parts[2]));
    }

    public override string ToString()
    {
        return $"{Name}|{Category}|{Id}|{ModuleName}";
    }
}
