using System.Reactive.Linq;
using VL.Core;
using VL.Lib.Collections;

namespace VL.Audio.VST.Internal;

public sealed class DynamicTypeInfo : IVLTypeInfo
{
    private Spread<IVLPropertyInfo>? properties;

    public required string Name { get; init; }
    public required string Category { get; init; }
    public required Func<DynamicTypeInfo, IEnumerable<IVLPropertyInfo>> LoadProperties { get; init; }

    public Func<NodeContext, object>? CreateInstance { get; init; }

    public Func<object>? GetDefaultValue { get; init; }

    public string FullName => $"{Category}.{Name}";

    public UniqueId Id => default;

    public Type ClrType { get; init; } = typeof(object);

    public bool IsPatched => false;

    public bool IsClass => true;

    public bool IsRecord => false;

    public bool IsImmutable => false;

    public bool IsInterface => false;

    public Spread<IVLPropertyInfo> Properties => properties ??= LoadProperties(this).ToSpread();

    Spread<IVLPropertyInfo> IVLTypeInfo.AllProperties => Properties;

    object? IVLTypeInfo.CreateInstance(NodeContext context) => CreateInstance?.Invoke(context);

    public Spread<Attribute> Attributes { get; init; } = Spread<Attribute>.Empty;

    object? IVLTypeInfo.GetDefaultValue() => GetDefaultValue?.Invoke();

    public IVLPropertyInfo? GetProperty(string name)
    {
        return Properties.FirstOrDefault(p => p.OriginalName == name);
    }

    public IVLTypeInfo MakeGenericType(IReadOnlyList<IVLTypeInfo> arguments)
    {
        throw new NotImplementedException();
    }

    public string ToString(bool includeCategory) => includeCategory ? FullName : Name;
}
