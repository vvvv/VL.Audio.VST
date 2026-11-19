using System.Runtime.CompilerServices;
using VST3;

namespace VL.Audio.VST.Tests;

/// <summary>
/// The structs the host shares with plugins must have the size of their C++ counterparts.
/// </summary>
/// <remarks>
/// Ported from the VST3 SDK 3.8.1 (MIT), public.sdk/source/vst/testsuite/vststructsizecheck.h: the expected value is
/// the Platform64Size of its SMTG_TYPE_SIZE_CHECK (Type, Platform64Size, MacOS32Size, Win32Size, Linux32Size). The
/// library disables runtime marshalling, so these structs cross the COM boundary as they are in memory: the size
/// that counts is the managed one (Unsafe.SizeOf), not Marshal.SizeOf. Structs of the header that do not exist in
/// src/VST3 (IContextMenuItem, NoteExpressionValueDescription, NoteExpressionValueEvent, NoteExpressionTextEvent,
/// NoteExpressionTypeInfo, KeyswitchInfo, RepresentationInfo) are left out.
/// </remarks>
public class StructSizeTests
{
    // ipluginbase.h
    [TestCase(typeof(PFactoryInfo), 452)]
    [TestCase(typeof(PClassInfo), 116)]
    [TestCase(typeof(PClassInfo2), 440)]
    [TestCase(typeof(PClassInfoW), 696)]
    // ivstaudioprocessor.h
    [TestCase(typeof(ProcessSetup), 24)]
    [TestCase(typeof(AudioBusBuffers), 24)]
    [TestCase(typeof(ProcessData), 80)]
    // ivstcomponent.h
    [TestCase(typeof(BusInfo), 276)]
    [TestCase(typeof(RoutingInfo), 12)]
    // ivsteditcontroller.h
    [TestCase(typeof(ParameterInfo), 792)]
    // ivstevents.h
    [TestCase(typeof(NoteOnEvent), 20)]
    [TestCase(typeof(NoteOffEvent), 16)]
    [TestCase(typeof(DataEvent), 16)]
    [TestCase(typeof(PolyPressureEvent), 12)]
    [TestCase(typeof(ChordEvent), 16)]
    [TestCase(typeof(ScaleEvent), 16)]
    [TestCase(typeof(LegacyMIDICCOutEvent), 4)]
    [TestCase(typeof(Event), 48)]
    // ivstprocesscontext.h
    [TestCase(typeof(FrameRate), 8)]
    [TestCase(typeof(Chord), 4)]
    [TestCase(typeof(ProcessContext), 112)]
    // ivstunits.h
    [TestCase(typeof(UnitInfo), 268)]
    [TestCase(typeof(ProgramListInfo), 264)]
    public void SizeMatchesTheSdk(Type type, int platform64Size)
    {
        Assert.That(RuntimeHelpers.SizeOf(type.TypeHandle), Is.EqualTo(platform64Size), type.Name);
    }
}
