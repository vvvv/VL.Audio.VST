using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using VL.Core;
using VL.Core.Reactive;
using VL.Lib.Collections;
using VST3;

namespace VL.Audio.VST;

partial class EffectHost : IVLObject, INotifyPropertyChanged
{
    private DynamicTypeInfo? type;
    private IDisposable? propertyChangedSubscription;

    IVLTypeInfo IVLObject.Type => type ??= new DynamicTypeInfo()
    {
        Name = info.NodeDescription.Name, 
        Category = info.NodeDescription.Category, 
        LoadProperties = typeInfo => LoadProperties(typeInfo, this)
    };

    public event PropertyChangedEventHandler? PropertyChanged;

    private IEnumerable<IVLPropertyInfo> LoadProperties(DynamicTypeInfo typeInfo, EffectHost effectHost)
    {
        var controller = effectHost.controller;
        if (controller is null)
            yield break;

        var typeRegistry = effectHost.nodeContext.AppHost.TypeRegistry;

        var hasRootUnit = units.Any(x => x.Value.Id == Constants.kRootUnitId);
        foreach (var p in LoadPropertiesForUnit(typeInfo, Constants.kRootUnitId, exposeParameters: !hasRootUnit))
            yield return p;

        if (!hasRootUnit)
        {
            propertyChangedSubscription = ParameterChanged
                .Where(x => x.parameter.UnitId == Constants.kRootUnitId && ExposeAsProperty(in x.parameter))
                .Subscribe(x => PropertyChanged?.Invoke(effectHost, new PropertyChangedEventArgs(x.parameter.Title)));
        }

        IEnumerable<IVLPropertyInfo> LoadPropertiesForUnit(IVLTypeInfo declaringType, int unitId, bool exposeParameters = true)
        {
            foreach (var unitInfo in units.Values)
            {
                if (unitInfo.ParentUnitId != unitId)
                    continue;

                var unitTypeInfo = new DynamicTypeInfo()
                {
                    Name = unitInfo.Name,
                    Category = declaringType.FullName,
                    LoadProperties = unitTypeInfo => LoadPropertiesForUnit(unitTypeInfo, unitInfo.Id)
                };
                var unit = new DynamicObject()
                {
                    Type = unitTypeInfo,
                    PropertyChangedSource = effectHost.ParameterChanged
                        .Where(e => e.parameter.UnitId == unitId && ExposeAsProperty(in e.parameter))
                        .Select(e => new PropertyChangedEventArgs(e.parameter.Title))
                };
                yield return new DynamicPropertyInfo()
                {
                    DeclaringType = declaringType,
                    Name = unitInfo.Name,
                    Type = unitTypeInfo,
                    GetValue = _ => unit,
                };
            }

            if (!exposeParameters)
                yield break;

            foreach (var p in controller.GetParameters())
            {
                if (p.UnitId != unitId || !ExposeAsProperty(in p))
                    continue;

                var attributes = Spread<Attribute>.Empty;
                if (p.Flags.HasFlag(ParameterInfo.ParameterFlags.kIsReadOnly))
                    attributes = attributes.Add(new System.ComponentModel.ReadOnlyAttribute(isReadOnly: true));
                attributes = attributes.Add(new System.ComponentModel.DefaultValueAttribute(p.GetDefaultValue()));

                yield return new DynamicPropertyInfo()
                {
                    DeclaringType = declaringType,
                    Name = p.Title,
                    Type = typeRegistry.GetTypeInfo(p.GetPinType()),
                    GetValue = i =>
                    {
                        return p.GetCurrentValue(effectHost.controller!);
                    },
                    WithValue = (i, v) =>
                    {
                        if (!p.Flags.HasFlag(ParameterInfo.ParameterFlags.kIsReadOnly))
                            effectHost.SetParameter(p.ID, p.Normalize(v));
                        return i;
                    },
                    Attributes = attributes
                };
            }
        }
    }

    static bool ExposeAsProperty(in ParameterInfo p)
    {
        return
            !p.Flags.HasFlag(ParameterInfo.ParameterFlags.kIsHidden) &&
            !p.Flags.HasFlag(ParameterInfo.ParameterFlags.kIsList) &&
            !p.Flags.HasFlag(ParameterInfo.ParameterFlags.kIsBypass);
    }
}

// TODO: vvvv crashes if not public
public sealed class DynamicObject : IVLObject, INotifyPropertyChanged
{
    private PropertyChangedEventHandler? propertyChangedEventHandler;
    private IDisposable? propertyChangedSubscription;
    private int refCount;

    public required IVLTypeInfo Type { get; init; }
    public AppHost AppHost { get; init; } = AppHost.Current;
    public NodeContext Context { get; init; } = NodeContext.CurrentRoot;
    public uint Identity { get; init; }
    public IObservable<PropertyChangedEventArgs>? PropertyChangedSource { get; init; }

    // Needs to be a public event - using explicit implementation crashes Observable.FromEventPattern
    public event PropertyChangedEventHandler? PropertyChanged
    {
        add
        {
            propertyChangedEventHandler += value;

            if (Interlocked.Increment(ref refCount) == 1)
                propertyChangedSubscription = PropertyChangedSource?.Subscribe(e => propertyChangedEventHandler?.Invoke(this, e));
        }

        remove
        {
            propertyChangedEventHandler -= value;

            if (Interlocked.Decrement(ref refCount) == 0)
                propertyChangedSubscription?.Dispose();
        }
    }

    IVLObject IVLObject.With(IReadOnlyDictionary<string, object> values)
    {
        IVLObject that = this;
        foreach (var (key, value) in values)
        {
            var property = Type.GetProperty(key);
            if (property is null)
                throw new Exception($"Property '{key}' not found on type '{Type}'");
            that = property.WithValue(that, value);
        }
        return that;
    }

    object IVLObject.ReadProperty(string key)
    {
        var property = Type.GetProperty(key);
        if (property is null)
            throw new Exception($"Property '{key}' not found on type '{Type}'");
        return property.GetValue(this);
    }
}

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

public sealed class DynamicPropertyInfo : IVLPropertyInfo
{
    public required IVLTypeInfo DeclaringType { get; init; }
    public required string Name { get; init; }
    public required IVLTypeInfo Type { get; init; }
    public required Func<IVLObject, object> GetValue { get; init; }
    public Func<IVLObject, object, IVLObject>? WithValue { get; init; }

    public uint Id { get; init; }
    public string NameForTextualCode => Name;
    public string OriginalName => Name;
    public bool IsManaged { get; init; }
    public bool ShouldBeSerialized { get; init; }
    public Spread<Attribute> Attributes { get; init; } = Spread<Attribute>.Empty;
    object IVLPropertyInfo.GetValue(IVLObject instance) => GetValue(instance);
    IVLObject IVLPropertyInfo.WithValue(IVLObject instance, object value) => WithValue?.Invoke(instance, value) ?? instance;
}
