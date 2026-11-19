using System.Collections.Immutable;
using VST3;

namespace VL.Audio.VST.Internal;

/// <summary>
/// What the "Parameters" pin group must contain and which parameter IDs to save with it.
/// </summary>
/// <param name="Pins">Every existing pin stays; one without a parameter keeps the type of its value.</param>
/// <param name="SavedIds">The parameter ID of each pin; an unresolved pin keeps its saved ID.</param>
/// <param name="Warning">"No parameter found for pins: ..." or null when every pin has a parameter.</param>
internal sealed record PinGroupState(
    ImmutableArray<(string Name, Type Type)> Pins,
    ImmutableDictionary<string, uint> SavedIds,
    string? Warning);

/// <summary>
/// Binds the pins of the "Parameters" pin group to the parameters of a plugin: by saved ID first (stable across
/// renames), by full name second. Knows nothing of vvvv, only names, IDs and CLR types.
/// </summary>
internal sealed class ParameterPins
{
    private readonly Dictionary<uint, ParameterInfo> parameters = new();
    private readonly Dictionary<string, uint> lookup = new();
    private readonly Dictionary<int, UnitInfo> units = new();

    public IReadOnlyDictionary<uint, ParameterInfo> Parameters => parameters;

    public IReadOnlyDictionary<int, UnitInfo> Units => units;

    public ImmutableDictionary<string, uint> SavedIds { get; set; } = ImmutableDictionary<string, uint>.Empty;

    /// <summary>
    /// Sets the parameters and units the controller reports, replacing the previous ones.
    /// </summary>
    public void SetParameters(IEnumerable<ParameterInfo> parameters, IEnumerable<UnitInfo> units)
    {
        // Units first, the full names of the parameters need them
        this.units.Clear();
        foreach (var u in units)
            this.units[u.Id] = u;

        this.parameters.Clear();
        lookup.Clear();
        foreach (var p in parameters)
        {
            this.parameters[p.ID] = p;
            lookup[GetFullName(in p)] = p.ID;
        }
    }

    /// <summary>
    /// Makes a name usable as a segment of a value path (see ValuePath): "." and brackets have a special meaning there.
    /// </summary>
    public static string ToSegment(string name) => name.Replace('.', '·').Replace('[', '(').Replace(']', ')');

    /// <summary>
    /// The dotted path of the parameter through its units.
    /// </summary>
    public string GetFullName(in ParameterInfo p)
    {
        var unitName = GetUnitFullName(p.UnitId);
        if (string.IsNullOrEmpty(unitName))
            return ToSegment(p.Title);
        return $"{unitName}.{ToSegment(p.Title)}";
    }

    private string GetUnitFullName(int unitId)
    {
        if (unitId == Constants.kRootUnitId || !units.TryGetValue(unitId, out var info))
            return string.Empty;

        var parentName = GetUnitFullName(info.ParentUnitId);
        if (!string.IsNullOrEmpty(parentName))
            return $"{parentName}.{ToSegment(info.Name)}";
        return ToSegment(info.Name);
    }

    /// <summary>
    /// Resolves the name of a pin to a parameter: by saved ID first (stable across renames), by name second.
    /// </summary>
    public bool TryResolve(string pinName, out uint id)
    {
        if (SavedIds.TryGetValue(pinName, out id) && parameters.ContainsKey(id))
            return true;
        return lookup.TryGetValue(pinName, out id);
    }

    /// <summary>
    /// Resolves the name of a pin to the parameter it is bound to.
    /// </summary>
    public bool TryResolve(string pinName, out ParameterInfo parameter)
    {
        if (TryResolve(pinName, out uint id) && parameters.TryGetValue(id, out parameter))
            return true;
        parameter = default;
        return false;
    }

    /// <summary>
    /// Finds the pin bound to the parameter.
    /// </summary>
    public bool TryGetPinName(IEnumerable<string> pinNames, uint id, out string pinName)
    {
        foreach (var n in pinNames)
        {
            if (TryResolve(n, out uint i) && i == id)
            {
                pinName = n;
                return true;
            }
        }
        pinName = string.Empty;
        return false;
    }

    /// <summary>
    /// The pin group after a sync: the existing pins, with the types of their parameters.
    /// </summary>
    public PinGroupState Sync(IReadOnlyDictionary<string, object> pins)
    {
        return new PinGroupState(GetExistingPins(pins).ToImmutableArray(), GetSavedIds(pins.Keys), GetWarning(pins.Keys));
    }

    /// <summary>
    /// The pin group after learning the parameter: the existing pins and one for the parameter, unless one is bound
    /// to it already. Null if the parameter is unknown.
    /// </summary>
    public PinGroupState? Learn(IReadOnlyDictionary<string, object> pins, uint id, out string pinName)
    {
        if (!parameters.TryGetValue(id, out var parameter))
        {
            pinName = string.Empty;
            return null;
        }

        if (!TryGetPinName(pins.Keys, id, out pinName))
            pinName = GetFullName(in parameter);

        var result = GetExistingPins(pins);
        if (!pins.ContainsKey(pinName))
            result = result.Append((pinName, parameter.GetPinType()));

        var pinNames = pins.Keys.Append(pinName).Distinct().ToList();
        return new PinGroupState(result.ToImmutableArray(), GetSavedIds(pinNames), GetWarning(pinNames));
    }

    // Pins are only removed by the user: a pin without a parameter keeps its type and stays, as some plugins
    // (plugdata) publish their parameters or final titles only after the first sync
    private IEnumerable<(string Name, Type Type)> GetExistingPins(IReadOnlyDictionary<string, object> pins)
    {
        foreach (var (pinName, value) in pins)
        {
            if (TryResolve(pinName, out ParameterInfo p))
                yield return (pinName, p.GetPinType());
            else if (value is not null)
                yield return (pinName, value.GetType());
        }
    }

    private ImmutableDictionary<string, uint> GetSavedIds(IEnumerable<string> pinNames)
    {
        var builder = ImmutableDictionary.CreateBuilder<string, uint>();
        foreach (var n in pinNames)
        {
            // An unresolved pin keeps its saved ID, the parameter may still show up later
            if (TryResolve(n, out uint id) || SavedIds.TryGetValue(n, out id))
                builder[n] = id;
        }
        return builder.ToImmutable();
    }

    /// <summary>
    /// Lists the pins without a parameter, null if there are none. A pin bound by ID to a parameter of another name
    /// (renamed in the plugin) is fine: it keeps working under its saved name.
    /// </summary>
    public string? GetWarning(IEnumerable<string> pinNames)
    {
        var missing = pinNames
            .Where(n => !TryResolve(n, out uint _))
            .Select(n => $"\"{n}\"")
            .ToList();

        return missing.Count > 0 ? $"No parameter found for pins: {string.Join(", ", missing)}" : null;
    }
}
