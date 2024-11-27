using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using VL.Core;
using VL.Core.Reactive;
using VL.Lib.Collections;
using VST3;

namespace VL.Audio.VST;

partial class EffectHost : IVLObject
{
    private TypeInfo? type;

    IVLTypeInfo IVLObject.Type => type ??= new TypeInfo()
    {
        Name = info.NodeDescription.Name, 
        Category = info.NodeDescription.Category, 
        LoadProperties = typeInfo => LoadProperties(typeInfo, this)
    };

    private IEnumerable<IVLPropertyInfo> LoadProperties(TypeInfo typeInfo, EffectHost effectHost)
    {
        var controller = effectHost.controller;
        if (controller is null)
            yield break;

        var typeRegistry = effectHost.nodeContext.AppHost.TypeRegistry;

        var unitController = controller as IUnitInfo;
        var units = unitController?.GetUnitInfos().ToImmutableArray() ?? ImmutableArray<UnitInfo>.Empty;
        foreach (var p in LoadPropertiesForUnit(Constants.kRootUnitId, units))
            yield return p;

        IEnumerable<IVLPropertyInfo> LoadPropertiesForUnit(int unitId, ImmutableArray<UnitInfo> units)
        {
            foreach (var unitInfo in units)
            {
                if (unitInfo.ParentUnitId != unitId)
                    continue;

                var unitTypeInfo = new TypeInfo()
                {
                    Name = unitInfo.Name,
                    Category = typeInfo.FullName,
                    LoadProperties = unitTypeInfo => LoadPropertiesForUnit(unitInfo.Id, units)
                };
                var unit = new DynamicObject()
                {
                    Type = unitTypeInfo,
                };
                yield return new PropertyInfo()
                {
                    DeclaringType = typeInfo,
                    Name = unitInfo.Name,
                    Type = unitTypeInfo,
                    GetValue = _ => unit,
                };
            }

            foreach (var p in controller.GetParameters())
            {
                if (p.UnitId != unitId)
                    continue;
                if (p.Flags.HasFlag(ParameterInfo.ParameterFlags.kIsBypass))
                    continue;
                if (p.Flags.HasFlag(ParameterInfo.ParameterFlags.kIsHidden))
                    continue;
                if (p.Flags.HasFlag(ParameterInfo.ParameterFlags.kIsList))
                    continue;
                if (!p.Flags.HasFlag(ParameterInfo.ParameterFlags.kCanAutomate))
                    continue;

                var attributes = Spread<Attribute>.Empty;
                if (p.Flags.HasFlag(ParameterInfo.ParameterFlags.kIsReadOnly))
                    attributes = attributes.Add(new System.ComponentModel.ReadOnlyAttribute(isReadOnly: true));
                attributes = attributes.Add(new System.ComponentModel.DefaultValueAttribute(p.GetDefaultValue()));

                yield return new PropertyInfo()
                {
                    DeclaringType = typeInfo,
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

    sealed class DynamicObject : IVLObject
    {
        public required IVLTypeInfo Type { get; init; }
        public AppHost AppHost { get; init; } = AppHost.Current;
        public NodeContext Context { get; init; } = NodeContext.CurrentRoot;
        public uint Identity { get; init; }

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

    sealed class TypeInfo : IVLTypeInfo
    {
        private Spread<IVLPropertyInfo>? properties;

        public required string Name { get; init; }
        public required string Category { get; init; }
        public required Func<TypeInfo, IEnumerable<IVLPropertyInfo>> LoadProperties { get; init; }

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

    sealed class PropertyInfo : IVLPropertyInfo
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
}
