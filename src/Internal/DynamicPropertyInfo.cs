using VL.Core;
using VL.Lib.Collections;

namespace VL.Audio.VST.Internal;

public sealed class DynamicPropertyInfo : IVLPropertyInfo
{
    public required IVLTypeInfo DeclaringType { get; init; }
    public required string Name { get; init; }
    public required IVLTypeInfo Type { get; init; }
    public required Func<object, object> GetValue { get; init; }
    public Func<object, object?, object>? WithValue { get; init; }

    public uint Id { get; init; }
    public string NameForTextualCode => Name;
    public string OriginalName => Name;
    public bool IsManaged { get; init; }
    public bool ShouldBeSerialized { get; init; }
    public Spread<Attribute> Attributes { get; init; } = Spread<Attribute>.Empty;
    object IVLPropertyInfo.GetValue(object instance) => GetValue(instance);
    object IVLPropertyInfo.WithValue(object instance, object? value) => WithValue?.Invoke(instance, value) ?? instance;
}
