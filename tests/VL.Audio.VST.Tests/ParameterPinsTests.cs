using System.Collections.Immutable;
using VL.Audio.VST.Internal;
using VST3;

namespace VL.Audio.VST.Tests;

public class ParameterPinsTests
{
    private const uint GainId = 7;

    private static ParameterInfo P(uint id, string title, int unitId = Constants.kRootUnitId, int stepCount = 0)
        => new(id, title, stepCount: stepCount, unitId: unitId);

    private static UnitInfo U(int id, int parentId, string name) => new(id, parentId, name);

    private static ParameterPins Create(params ParameterInfo[] parameters)
    {
        var pins = new ParameterPins();
        pins.SetParameters(parameters, []);
        return pins;
    }

    private static ImmutableDictionary<string, uint> Ids(params (string Name, uint Id)[] ids)
        => ids.ToImmutableDictionary(x => x.Name, x => x.Id);

    [Test]
    public void FirstSyncBeforeTheParametersArePublished_KeepsThePinItsIdAndWarns()
    {
        var pins = Create();
        pins.SavedIds = Ids(("Gain", GainId));

        var state = pins.Sync(new Dictionary<string, object> { ["Gain"] = 0.5f });

        Assert.That(state.Pins, Is.EqualTo(new[] { ("Gain", typeof(float)) }));
        Assert.That(state.SavedIds, Is.EquivalentTo(Ids(("Gain", GainId))));
        Assert.That(state.Warning, Is.EqualTo("No parameter found for pins: \"Gain\""));
    }

    [Test]
    public void UnresolvedPin_KeepsTheTypeOfItsValue()
    {
        var pins = Create();

        var state = pins.Sync(new Dictionary<string, object> { ["Mode"] = 3, ["On"] = true });

        Assert.That(state.Pins, Is.EqualTo(new[] { ("Mode", typeof(int)), ("On", typeof(bool)) }));
        Assert.That(state.SavedIds, Is.Empty);
    }

    [Test]
    public void ParametersArriveLater_SamePinsAndNoWarning()
    {
        var pins = Create();
        pins.SavedIds = Ids(("Gain", GainId));
        var values = new Dictionary<string, object> { ["Gain"] = 0.5f };
        pins.SavedIds = pins.Sync(values).SavedIds;

        pins.SetParameters([P(GainId, "Gain")], []);
        var state = pins.Sync(values);

        Assert.That(state.Pins, Is.EqualTo(new[] { ("Gain", typeof(float)) }));
        Assert.That(state.SavedIds, Is.EquivalentTo(Ids(("Gain", GainId))));
        Assert.That(state.Warning, Is.Null);
    }

    [Test]
    public void ResolvedPin_TakesTheTypeOfItsParameter()
    {
        var pins = Create(P(1, "Toggle", stepCount: 1), P(2, "Mode", stepCount: 4), P(3, "Level"));

        var state = pins.Sync(new Dictionary<string, object> { ["Toggle"] = 0f, ["Mode"] = 0f, ["Level"] = 0 });

        Assert.That(state.Pins, Is.EqualTo(new[] { ("Toggle", typeof(bool)), ("Mode", typeof(int)), ("Level", typeof(float)) }));
        Assert.That(state.SavedIds, Is.EquivalentTo(Ids(("Toggle", 1u), ("Mode", 2u), ("Level", 3u))));
    }

    [Test]
    public void RenamedParameter_PinKeepsItsNameAndResolvesById()
    {
        var pins = Create(P(GainId, "Volume"));
        pins.SavedIds = Ids(("Gain", GainId));

        var state = pins.Sync(new Dictionary<string, object> { ["Gain"] = 0.5f });

        Assert.That(pins.TryResolve("Gain", out uint id), Is.True);
        Assert.That(id, Is.EqualTo(GainId));
        Assert.That(state.Pins, Is.EqualTo(new[] { ("Gain", typeof(float)) }));
        Assert.That(state.SavedIds, Is.EquivalentTo(Ids(("Gain", GainId))));
        Assert.That(state.Warning, Is.Null);
    }

    [Test]
    public void SavedIdComesBeforeName()
    {
        // The pin "Gain" was learned on ID 7, which is now called "Volume"; another parameter took the name "Gain"
        var pins = Create(P(GainId, "Volume"), P(8, "Gain"));
        pins.SavedIds = Ids(("Gain", GainId));

        Assert.That(pins.TryResolve("Gain", out uint id), Is.True);
        Assert.That(id, Is.EqualTo(GainId));
    }

    [Test]
    public void SavedIdOfAMissingParameter_FallsBackToTheName()
    {
        var pins = Create(P(8, "Gain"));
        pins.SavedIds = Ids(("Gain", GainId));

        var state = pins.Sync(new Dictionary<string, object> { ["Gain"] = 0.5f });

        Assert.That(state.SavedIds, Is.EquivalentTo(Ids(("Gain", 8u))));
        Assert.That(state.Warning, Is.Null);
    }

    [Test]
    public void ParameterGone_WarnsUntilThePinIsRemoved()
    {
        var pins = Create(P(GainId, "Gain"), P(8, "Pan"));
        var values = new Dictionary<string, object> { ["Gain"] = 0.5f, ["Pan"] = 0.5f };
        pins.SavedIds = pins.Sync(values).SavedIds;

        pins.SetParameters([P(8, "Pan")], []);
        var state = pins.Sync(values);

        Assert.That(state.Warning, Is.EqualTo("No parameter found for pins: \"Gain\""));
        Assert.That(state.Pins, Is.EqualTo(new[] { ("Gain", typeof(float)), ("Pan", typeof(float)) }));
        Assert.That(state.SavedIds, Is.EquivalentTo(Ids(("Gain", GainId), ("Pan", 8u))));

        values.Remove("Gain");
        Assert.That(pins.GetWarning(values.Keys), Is.Null);
        Assert.That(pins.Sync(values).Warning, Is.Null);
    }

    [Test]
    public void Warning_ListsEveryPinWithoutAParameter()
    {
        var pins = Create(P(1, "A"));

        Assert.That(pins.GetWarning(["A", "B", "C"]), Is.EqualTo("No parameter found for pins: \"B\", \"C\""));
    }

    [Test]
    public void Learn_AddsAPinUnderTheFullNameAndSavesItsId()
    {
        var pins = new ParameterPins();
        pins.SetParameters([P(GainId, "Gain", unitId: 1), P(8, "Pan")], [U(1, Constants.kRootUnitId, "Amp")]);
        var values = new Dictionary<string, object> { ["Pan"] = 0.5f };

        var state = pins.Learn(values, GainId, out var pinName);

        Assert.That(pinName, Is.EqualTo("Amp.Gain"));
        Assert.That(state, Is.Not.Null);
        Assert.That(state!.Pins, Is.EqualTo(new[] { ("Pan", typeof(float)), ("Amp.Gain", typeof(float)) }));
        Assert.That(state.SavedIds, Is.EquivalentTo(Ids(("Pan", 8u), ("Amp.Gain", GainId))));
        Assert.That(state.Warning, Is.Null);
    }

    [Test]
    public void LearnOfAPinnedParameter_KeepsTheExistingPin()
    {
        var pins = Create(P(GainId, "Volume"));
        pins.SavedIds = Ids(("Gain", GainId));
        var values = new Dictionary<string, object> { ["Gain"] = 0.5f };

        var state = pins.Learn(values, GainId, out var pinName);

        Assert.That(pinName, Is.EqualTo("Gain"));
        Assert.That(state!.Pins, Is.EqualTo(new[] { ("Gain", typeof(float)) }));
        Assert.That(state.SavedIds, Is.EquivalentTo(Ids(("Gain", GainId))));
    }

    [Test]
    public void LearnOfAnUnknownParameter_ReturnsNull()
    {
        var pins = Create(P(GainId, "Gain"));

        Assert.That(pins.Learn(new Dictionary<string, object>(), 99, out _), Is.Null);
    }

    [Test]
    public void FullName_FollowsNestedUnits()
    {
        var pins = new ParameterPins();
        pins.SetParameters([], [U(Constants.kRootUnitId, Constants.kNoParentUnitId, "Root"), U(1, Constants.kRootUnitId, "Osc"), U(2, 1, "Env")]);

        Assert.That(pins.GetFullName(P(1, "Attack", unitId: 2)), Is.EqualTo("Osc.Env.Attack"));
        Assert.That(pins.GetFullName(P(2, "Shape", unitId: 1)), Is.EqualTo("Osc.Shape"));
        Assert.That(pins.GetFullName(P(3, "Gain")), Is.EqualTo("Gain"));
        Assert.That(pins.GetFullName(P(4, "Lost", unitId: 42)), Is.EqualTo("Lost"));
    }

    [Test]
    public void FullName_ReplacesDotsAndBrackets()
    {
        var pins = new ParameterPins();
        pins.SetParameters([], [U(1, Constants.kRootUnitId, "Env.A")]);

        Assert.That(pins.GetFullName(P(1, "Attack [ms]", unitId: 1)), Is.EqualTo("Env·A.Attack (ms)"));
        Assert.That(ParameterPins.ToSegment("a.b[c]"), Is.EqualTo("a·b(c)"));
    }

    [Test]
    public void ParametersAreFoundByTheirFullName()
    {
        var pins = new ParameterPins();
        pins.SetParameters([P(5, "Cutoff", unitId: 1)], [U(1, Constants.kRootUnitId, "Filter")]);

        Assert.That(pins.TryResolve("Filter.Cutoff", out uint id), Is.True);
        Assert.That(id, Is.EqualTo(5u));
        Assert.That(pins.TryResolve("Cutoff", out uint _), Is.False);
    }
}
