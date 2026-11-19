using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using VST3;
using VST3.Hosting;

namespace VL.Audio.VST.Tests.Fakes;

/// <summary>
/// A VST3 plugin written in C#: the factory of one <see cref="FakeComponent"/> and one <see cref="FakeController"/>.
/// The host reaches it only through native COM pointers (see <see cref="ComRoundTrip"/>), so its calls run through
/// the same vtables as with a real plugin. Tests script it through <see cref="Component"/> and <see cref="Controller"/>.
/// </summary>
[GeneratedComClass]
internal sealed partial class FakePlugin : IPluginFactory
{
    public static readonly Guid ClassId = new("6C1F0E52-6A4D-4C1B-9D57-3A0E6A9F1B01");
    public static readonly Guid ControllerClassId = new("6C1F0E52-6A4D-4C1B-9D57-3A0E6A9F1B02");

    public FakeComponent Component { get; } = new();

    public FakeController Controller { get; } = new();

    PFactoryInfo IPluginFactory.getFactoryInfo() => default;

    int IPluginFactory.countClasses() => 0;

    int IPluginFactory.getClassInfo(int index, out PClassInfo info)
    {
        info = default;
        return ResultCodes.InvalidArgument;
    }

    nint IPluginFactory.createInstance(in Guid cid, in Guid iid)
    {
        object instance = cid == ClassId ? Component
            : cid == ControllerClassId ? Controller
            : throw new ArgumentException($"Unknown class {cid}");

        var ptr = instance.GetComPtr(iid);
        if (ptr == default)
            throw new InvalidCastException($"{instance.GetType().Name} does not implement {iid}");
        return ptr;
    }
}

/// <summary>
/// The processor part of <see cref="FakePlugin"/>: keeps its state as bytes and records what <c>process</c> receives.
/// </summary>
[GeneratedComClass]
internal sealed partial class FakeComponent : IComponent, IAudioProcessor, IConnectionPoint
{
    public bool IsInitialized { get; private set; }
    public bool IsTerminated { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsProcessing { get; private set; }
    public IConnectionPoint? ConnectedTo { get; private set; }

    public byte[] State { get; set; } = [];

    public List<Event> ReceivedEvents { get; } = new();
    public List<(uint Id, int SampleOffset, double Value)> ReceivedParameterChanges { get; } = new();

    void IPluginBase.initialize(IHostApplication context) => IsInitialized = true;

    void IPluginBase.terminate() => IsTerminated = true;

    void IComponent.getControllerClassId(ref Guid classId) => classId = FakePlugin.ControllerClassId;

    void IComponent.setIoMode(IoModes mode) { }

    int IComponent.getBusCount(MediaTypes type, BusDirections dir) => 0;

    BusInfo IComponent.getBusInfo(MediaTypes type, BusDirections dir, int index) => throw new NotImplementedException();

    void IComponent.getRoutingInfo(in RoutingInfo inInfo, out RoutingInfo outInfo) => throw new NotImplementedException();

    void IComponent.activateBus(MediaTypes type, BusDirections dir, int index, bool state) { }

    void IComponent.setActive(bool state) => IsActive = state;

    void IComponent.setState(IBStream state) => State = ComRoundTrip.ReadAll(state);

    void IComponent.getState(IBStream state) => ComRoundTrip.WriteAll(state, State);

    int IAudioProcessor.setBusArrangements(SpeakerArrangement[] inputs, int numIns, SpeakerArrangement[] outputs, int numOuts)
        => ResultCodes.False;

    SpeakerArrangement IAudioProcessor.getBusArrangement(BusDirections dir, int index) => throw new NotImplementedException();

    int IAudioProcessor.canProcessSampleSize(int symbolicSampleSize)
        => symbolicSampleSize == (int)SymbolicSampleSizes.Sample32 ? ResultCodes.True : ResultCodes.False;

    uint IAudioProcessor.getLatencySamples() => 0;

    void IAudioProcessor.setupProcessing(in ProcessSetup setup) { }

    int IAudioProcessor.setProcessing(bool state)
    {
        IsProcessing = state;
        return ResultCodes.True;
    }

    void IAudioProcessor.process(in ProcessData data)
    {
        // The host hands over raw pointers, read them like a native plugin would: through their vtables
        if (data.InputEvents != default)
        {
            var events = ComRoundTrip.Wrap<IEventList>(data.InputEvents);
            try
            {
                for (int i = 0; i < events.getEventCount(); i++)
                    ReceivedEvents.Add(events.getEvent(i));
            }
            finally
            {
                events.ReleaseComObject();
            }
        }

        if (data.InputParameterChanges != default)
        {
            var changes = ComRoundTrip.Wrap<IParameterChanges>(data.InputParameterChanges);
            try
            {
                for (int i = 0; i < changes.getParameterCount(); i++)
                {
                    var queue = changes.getParameterData(i);
                    if (queue is null)
                        continue;
                    for (int j = 0; j < queue.getPointCount(); j++)
                    {
                        queue.getPoint(j, out var offset, out var value);
                        ReceivedParameterChanges.Add((queue.getParameterId(), offset, value));
                    }
                }
            }
            finally
            {
                changes.ReleaseComObject();
            }
        }
    }

    uint IAudioProcessor.getTailSamples() => 0;

    void IConnectionPoint.connect(IConnectionPoint other) => ConnectedTo = other;

    void IConnectionPoint.disconnect(IConnectionPoint other) => ConnectedTo = null;

    void IConnectionPoint.notify(IMessage message) { }
}

/// <summary>
/// The controller part of <see cref="FakePlugin"/>: reports <see cref="Parameters"/> and <see cref="Units"/>, which a
/// test changes before it calls <see cref="RestartComponent"/>.
/// </summary>
[GeneratedComClass]
internal sealed partial class FakeController : IEditController, IUnitInfo, IConnectionPoint
{
    private readonly Dictionary<uint, double> values = new();

    public List<ParameterInfo> Parameters { get; } = new();
    public List<UnitInfo> Units { get; } = new();

    public bool IsInitialized { get; private set; }
    public bool IsTerminated { get; private set; }
    public IComponentHandler? Handler { get; private set; }
    public IConnectionPoint? ConnectedTo { get; private set; }

    /// <summary>
    /// When false, <c>setComponentState</c> answers kNotImplemented like plugdata does.
    /// </summary>
    public bool ImplementsSetComponentState { get; set; }
    public byte[]? ReceivedComponentState { get; private set; }

    public void Rename(uint id, string title)
    {
        var i = Parameters.FindIndex(p => p.ID == id);
        var p = Parameters[i];
        Parameters[i] = new ParameterInfo(p.ID, title, p.StepCount, p.DefaultNormalizedValue, p.UnitId, p.Flags, p.ShortTitle, p.Units);
    }

    public void RestartComponent(RestartFlags flags)
    {
        if (Handler is null)
            throw new InvalidOperationException("No component handler set");
        Handler.restartComponent(flags);
    }

    void IPluginBase.initialize(IHostApplication context) => IsInitialized = true;

    void IPluginBase.terminate() => IsTerminated = true;

    void IEditController.setComponentState(IBStream state)
    {
        if (!ImplementsSetComponentState)
            throw new NotImplementedException();
        ReceivedComponentState = ComRoundTrip.ReadAll(state);
    }

    void IEditController.setState(IBStream state) => throw new NotImplementedException();

    void IEditController.getState(IBStream state) => throw new NotImplementedException();

    int IEditController.getParameterCount() => Parameters.Count;

    ParameterInfo IEditController.getParameterInfo(int paramIndex) => Parameters[paramIndex];

    String128 IEditController.getParamStringByValue(uint id, double valueNormalized) => String128.From(valueNormalized.ToString());

    void IEditController.getParamValueByString(uint id, string @string, out double valueNormalized) => throw new NotImplementedException();

    double IEditController.normalizedParamToPlain(uint id, double valueNormalized) => valueNormalized;

    double IEditController.plainParamToNormalized(uint id, double plainValue) => plainValue;

    double IEditController.getParamNormalized(uint id) => values.GetValueOrDefault(id);

    void IEditController.setParamNormalized(uint id, double value) => values[id] = value;

    void IEditController.setComponentHandler(IComponentHandler handler) => Handler = handler;

    IPlugView IEditController.createView(string name) => null!;

    int IUnitInfo.getUnitCount() => Units.Count;

    UnitInfo IUnitInfo.getUnitInfo(int unitIndex) => Units[unitIndex];

    int IUnitInfo.getProgramListCount() => 0;

    ProgramListInfo IUnitInfo.getProgramListInfo(int listIndex) => throw new NotImplementedException();

    void IUnitInfo.getProgramName(int listId, int programIndex, String128 name) => throw new NotImplementedException();

    void IUnitInfo.getProgramInfo(int listId, int programIndex, string attributeId, String128 attributeValue) => throw new NotImplementedException();

    bool IUnitInfo.hasProgramPitchNames(int listId, int programIndex) => false;

    void IUnitInfo.getProgramPitchName(int listId, int programIndex, short midiPitch, String128 name) => throw new NotImplementedException();

    int IUnitInfo.getSelectedUnit() => Constants.kRootUnitId;

    void IUnitInfo.selectUnit(int unitId) { }

    bool IUnitInfo.getUnitByBus(MediaTypes type, BusDirections dir, int busIndex, int channel, out int unitID)
    {
        unitID = Constants.kRootUnitId;
        return false;
    }

    void IUnitInfo.setUnitProgramData(int listOrUnitId, int programIndex, IBStream data) => throw new NotImplementedException();

    void IConnectionPoint.connect(IConnectionPoint other) => ConnectedTo = other;

    void IConnectionPoint.disconnect(IConnectionPoint other) => ConnectedTo = null;

    void IConnectionPoint.notify(IMessage message) { }
}

internal static class ResultCodes
{
    public const int True = 0;
    public const int False = 1;
    public const int InvalidArgument = unchecked((int)0x80070057);
}
