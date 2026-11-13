using System.ComponentModel;
using System.Reactive.Linq;
using VL.Audio.VST.Internal;
using VL.Core;
using VL.Core.EditorAttributes;
using VL.Core.Import;
using VL.Lib.Collections;
using VST3;

namespace VL.Audio.VST;

partial class EffectHost : IVLObject, INotifyPropertyChanged
{
    private DynamicTypeInfo? type;
    private IDisposable? propertyChangedSubscription;
    private event PropertyChangedEventHandler? propertyChanged;

    IVLTypeInfo IVLObject.Type => type ??= new DynamicTypeInfo()
    {
        Name = info.Name, 
        Category = info.Category, 
        LoadProperties = typeInfo => LoadProperties(typeInfo)
    };

    // Needs to be a public event - using explicit implementation crashes Observable.FromEventPattern
    [Smell(SymbolSmell.Internal)]
    public event PropertyChangedEventHandler? PropertyChanged
    {
        add => propertyChanged += value;
        remove => propertyChanged -= value;
    }

    private IEnumerable<IVLPropertyInfo> LoadProperties(DynamicTypeInfo typeInfo)
    {
        if (controller is null)
            yield break;

        var typeRegistry = nodeContext.AppHost.TypeRegistry;
        foreach (var p in LoadPropertiesForUnit(typeInfo, Constants.kRootUnitId))
            yield return p;

        propertyChangedSubscription = ParameterChanged
            .Where(x => x.parameter.UnitId == Constants.kRootUnitId && ExposeAsProperty(in x.parameter))
            .Subscribe(x => propertyChanged?.Invoke(this, new PropertyChangedEventArgs(ToSegment(x.parameter.Title))));

        IEnumerable<IVLPropertyInfo> LoadPropertiesForUnit(IVLTypeInfo declaringType, int unitId)
        {
            foreach (var unitInfo in units.Values)
            {
                if (unitInfo.ParentUnitId != unitId)
                    continue;

                var unitTypeInfo = new DynamicTypeInfo()
                {
                    Name = ToSegment(unitInfo.Name),
                    Category = declaringType.FullName,
                    LoadProperties = unitTypeInfo => LoadPropertiesForUnit(unitTypeInfo, unitInfo.Id)
                };
                var unit = new DynamicObject()
                {
                    Type = unitTypeInfo,
                    PropertyChangedSource = ParameterChanged
                        .Where(e => e.parameter.UnitId == unitInfo.Id && ExposeAsProperty(in e.parameter))
                        .Select(e => new PropertyChangedEventArgs(ToSegment(e.parameter.Title)))
                };
                yield return new DynamicPropertyInfo()
                {
                    DeclaringType = declaringType,
                    Name = ToSegment(unitInfo.Name),
                    Type = unitTypeInfo,
                    GetValue = _ => unit,
                };
            }

            foreach (var p in controller.GetParameters())
            {
                if (p.UnitId != unitId || !ExposeAsProperty(in p))
                    continue;

                var attributes = GetAttributesForParameter(in p);
                if (TryGetPinName(p.ID, out _))
                    attributes = attributes.Add(new CanBePublishedAttribute(true));
                else
                    attributes = attributes.Add(new CanBePublishedAttribute(false));

                yield return new DynamicPropertyInfo()
                {
                    DeclaringType = declaringType,
                    Name = ToSegment(p.Title),
                    Type = typeRegistry.GetTypeInfo(p.GetPinType()),
                    GetValue = i =>
                    {
                        return p.GetCurrentValue(controller);
                    },
                    WithValue = (i, v) =>
                    {
                        if (!p.Flags.HasFlag(ParameterInfo.ParameterFlags.kIsReadOnly))
                            SetParameter(p.ID, p.Normalize(v));
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

    Spread<Attribute> GetAttributesForParameter(in ParameterInfo p)
    {
        var attributes = Spread<Attribute>.Empty;
        if (p.Flags.HasFlag(ParameterInfo.ParameterFlags.kIsReadOnly))
            attributes = attributes.Add(new System.ComponentModel.ReadOnlyAttribute(isReadOnly: true));
        attributes = attributes.Add(new System.ComponentModel.DefaultValueAttribute(p.GetDefaultValue()));
        return attributes;
    }
}
