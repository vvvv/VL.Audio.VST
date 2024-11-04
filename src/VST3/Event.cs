using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace VST3;

//------------------------------------------------------------------------
/** Event 
\ingroup vstEventGrp
Structure representing a single Event of different types associated to a specific event (\ref kEvent) bus.
*/
unsafe struct Event
{
    int busIndex;				/// event bus index
	int sampleOffset;			/// sample frames related to the current block start sample position
	double ppqPosition;	        /// position in project
	EventFlags flags;           /// combination of \ref EventFlags

    /** Event Flags - used for Event::flags */
    [Flags]
    enum EventFlags : ushort
    {
        /// <summary>
        /// indicates that the event is played live (directly from keyboard)
        /// </summary>
        kIsLive = 1 << 0,

        /// <summary>
        /// reserved for user (for internal use)
        /// </summary>
        kUserReserved1 = 1 << 14,
        /// <summary>
        /// reserved for user (for internal use)
        /// </summary>
		kUserReserved2 = 1 << 15
	};

    /**  Event Types - used for Event::type */
    public enum EventTypes : ushort
    {
        kNoteOnEvent = 0,
		kNoteOffEvent = 1,
		kDataEvent = 2,
		kPolyPressureEvent = 3,
		kNoteExpressionValueEvent = 4,
		kNoteExpressionTextEvent = 5,
		kChordEvent = 6,
		kScaleEvent = 7,
		kLegacyMIDICCOutEvent = 65535
	};

    EventTypes type;

    [StructLayout(LayoutKind.Explicit)]
    struct Union
    {
        [FieldOffset(0)]
        NoteOnEvent noteOn;
        [FieldOffset(0)]
        NoteOffEvent noteOff;
        [FieldOffset(0)]
        DataEvent data;
        [FieldOffset(0)]
        PolyPressureEvent polyPressure;
        //NoteExpressionValueEvent noteExpressionValue;
        //NoteExpressionTextEvent noteExpressionText;
        [FieldOffset(0)]
        ChordEvent chord;
        [FieldOffset(0)]
        ScaleEvent scale;
        [FieldOffset(0)]
        LegacyMIDICCOutEvent midiCCOut;					
    }

    Union value;

	public static Event New<TEvent>(TEvent e, int busIndex, int sampleOffset, double ppqPosition, bool isLive) where TEvent : unmanaged, IEvent
	{
		return new Event()
		{
			busIndex = busIndex,
			sampleOffset = sampleOffset,
			ppqPosition = ppqPosition,
			flags = isLive ? EventFlags.kIsLive : default,
			type = TEvent.Type,
            value = Unsafe.As<TEvent, Union>(ref e)
		};
    }

	public EventTypes Type => type;

	public object Value
	{
        get
        {
            switch (type)
            {
                case EventTypes.kNoteOnEvent:
                    return GetValue<NoteOnEvent>();
                case EventTypes.kNoteOffEvent:
                    return GetValue<NoteOffEvent>();
                case EventTypes.kDataEvent:
                    return GetValue<DataEvent>();
                case EventTypes.kPolyPressureEvent:
                    return GetValue<PolyPressureEvent>();
                case EventTypes.kNoteExpressionValueEvent:
                    throw new NotImplementedException();
                case EventTypes.kNoteExpressionTextEvent:
                    throw new NotImplementedException();
                case EventTypes.kChordEvent:
                    return GetValue<ChordEvent>();
                case EventTypes.kScaleEvent:
                    return GetValue<ScaleEvent>();
                case EventTypes.kLegacyMIDICCOutEvent:
                    return GetValue<LegacyMIDICCOutEvent>();
                default:
                    throw new NotImplementedException();
            }
        }
	}

	public T GetValue<T>() where T : unmanaged, IEvent
	{
		if (T.Type != type)
			throw new ArgumentException("Type mismatch");

        return Unsafe.As<Union, T>(ref value);
    }
}

interface IEvent
{
    static abstract Event.EventTypes Type { get; }
}

/// <summary>
/// Note-on event specific data. Used in <see cref="Event"/> (union).
/// Pitch uses the twelve-tone equal temperament tuning (12-TET).
/// </summary>
public struct NoteOnEvent : IEvent
{
    private short channel;
    private short pitch;
    private float tuning;
    private float velocity;
    private int length;
    private int noteId;

    /// <summary>
    /// Channel index in event bus.
    /// </summary>
    public short Channel => channel;

    /// <summary>
    /// Range [0, 127] = [C-2, G8] with A3=440Hz (12-TET: twelve-tone equal temperament).
    /// </summary>
    public short Pitch => pitch;

    /// <summary>
    /// 1.f = +1 cent, -1.f = -1 cent.
    /// </summary>
    public float Tuning => tuning;

    /// <summary>
    /// Range [0.0, 1.0].
    /// </summary>
    public float Velocity => velocity;

    /// <summary>
    /// In sample frames (optional, Note Off has to follow in any case!).
    /// </summary>
    public int Length => length;

    /// <summary>
    /// Note identifier (if not available then -1).
    /// </summary>
    public int NoteId => noteId;

    public NoteOnEvent(short channel, short pitch, float tuning, float velocity, int length, int noteId)
    {
        this.channel = channel;
        this.pitch = pitch;
        this.tuning = tuning;
        this.velocity = velocity;
        this.length = length;
        this.noteId = noteId;
    }

    static Event.EventTypes IEvent.Type => Event.EventTypes.kNoteOnEvent;
}

//------------------------------------------------------------------------
/** Note-off event specific data. Used in \ref Event (union)
\ingroup vstEventGrp 
*/
public struct NoteOffEvent : IEvent
{
    private short channel;
    private short pitch;
    private float velocity;
    private int noteId;
    private float tuning;

    /// <summary>
    /// Channel index in event bus.
    /// </summary>
    public short Channel => channel;

    /// <summary>
    /// Range [0, 127] = [C-2, G8] with A3=440Hz (12-TET).
    /// </summary>
    public short Pitch => pitch;

    /// <summary>
    /// Range [0.0, 1.0].
    /// </summary>
    public float Velocity => velocity;

    /// <summary>
    /// Associated noteOn identifier (if not available then -1).
    /// </summary>
    public int NoteId => noteId;

    /// <summary>
    /// 1.f = +1 cent, -1.f = -1 cent.
    /// </summary>
    public float Tuning => tuning;

    public NoteOffEvent(short channel, short pitch, float velocity, int noteId, float tuning)
    {
        this.channel = channel;
        this.pitch = pitch;
        this.velocity = velocity;
        this.noteId = noteId;
        this.tuning = tuning;
    }

    static Event.EventTypes IEvent.Type => Event.EventTypes.kNoteOffEvent;
}

//------------------------------------------------------------------------
/** Data event specific data. Used in \ref Event (union)
\ingroup vstEventGrp 
*/
public unsafe struct DataEvent : IEvent
{
    private uint size;
    private DataTypes type;
    private byte* bytes;

    /// <summary>
    /// Size in bytes of the data block bytes.
    /// </summary>
    public uint Size => size;

    /// <summary>
    /// Type of this data block (see <see cref="DataTypes"/>).
    /// </summary>
    public DataTypes Type => type;

    /// <summary>
    /// Pointer to the data block.
    /// </summary>
    public byte* Bytes => bytes;

    public ReadOnlySpan<byte> DataBlock => new(bytes, (int)size);

    /** Value for DataEvent::type */
    public enum DataTypes : uint
    {
        kMidiSysEx = 0  /// for MIDI system exclusive message
    };

    public DataEvent(uint size, DataTypes type, byte* bytes)
    {
        this.size = size;
        this.type = type;
        this.bytes = bytes;
    }

    static Event.EventTypes IEvent.Type => Event.EventTypes.kDataEvent;
}

//------------------------------------------------------------------------
/** PolyPressure event specific data. Used in \ref Event (union)
\ingroup vstEventGrp
*/
public struct PolyPressureEvent : IEvent
{
    private short channel;
    private short pitch;
    private float pressure;
    private int noteId;

    /// <summary>
    /// Channel index in event bus.
    /// </summary>
    public short Channel => channel;

    /// <summary>
    /// Range [0, 127] = [C-2, G8] with A3=440Hz.
    /// </summary>
    public short Pitch => pitch;

    /// <summary>
    /// Range [0.0, 1.0].
    /// </summary>
    public float Pressure => pressure;

    /// <summary>
    /// Event should be applied to the noteId (if not -1).
    /// </summary>
    public int NoteId => noteId;

    public PolyPressureEvent(short channel, short pitch, float pressure, int noteId)
    {
        this.channel = channel;
        this.pitch = pitch;
        this.pressure = pressure;
        this.noteId = noteId;
    }

    static Event.EventTypes IEvent.Type => Event.EventTypes.kPolyPressureEvent;
}

//------------------------------------------------------------------------
/** Chord event specific data. Used in \ref Event (union)
\ingroup vstEventGrp 
*/
public unsafe struct ChordEvent : IEvent
{
    private short root;
    private short bassNote;
    private short mask;
    private ushort textLen;
    private char* text;

    /// <summary>
    /// Range [0, 127] = [C-2, G8] with A3=440Hz.
    /// </summary>
    public short Root => root;

    /// <summary>
    /// Range [0, 127] = [C-2, G8] with A3=440Hz.
    /// </summary>
    public short BassNote => bassNote;

    /// <summary>
    /// Root is bit 0.
    /// </summary>
    public short Mask => mask;

    /// <summary>
    /// The number of characters (TChar) between the beginning of text and the terminating null character (without including the terminating null character itself).
    /// </summary>
    public ushort TextLen => textLen;

    /// <summary>
    /// UTF-16, null terminated Hosts Chord Name.
    /// </summary>
    public string Text => new string(text);

    public ChordEvent(short root, short bassNote, short mask, ushort textLen, char* text)
    {
        this.root = root;
        this.bassNote = bassNote;
        this.mask = mask;
        this.textLen = textLen;
        this.text = text;
    }

    static Event.EventTypes IEvent.Type => Event.EventTypes.kChordEvent;
}

//------------------------------------------------------------------------
/** Scale event specific data. Used in \ref Event (union)
\ingroup vstEventGrp 
*/
public unsafe struct ScaleEvent : IEvent
{
    private short root;
    private short mask;
    private ushort textLen;
    private char* text;

    /// <summary>
    /// Range [0, 127] = root Note/Transpose Factor.
    /// </summary>
    public short Root => root;

    /// <summary>
    /// Bit 0 =  C,  Bit 1 = C#, ... (0x5ab5 = Major Scale).
    /// </summary>
    public short Mask => mask;

    /// <summary>
    /// The number of characters (TChar) between the beginning of text and the terminating null character (without including the terminating null character itself).
    /// </summary>
    public ushort TextLen => textLen;

    /// <summary>
    /// UTF-16, null terminated, Hosts Scale Name.
    /// </summary>
    public char* Text => text;

    public ScaleEvent(short root, short mask, ushort textLen, char* text)
    {
        this.root = root;
        this.mask = mask;
        this.textLen = textLen;
        this.text = text;
    }

    static Event.EventTypes IEvent.Type => Event.EventTypes.kScaleEvent;
}

//------------------------------------------------------------------------
/** Legacy MIDI CC Out event specific data. Used in \ref Event (union)
\ingroup vstEventGrp
- [released: 3.6.12]

This kind of event is reserved for generating MIDI CC as output event for kEvent Bus during the process call.
*/
public struct LegacyMIDICCOutEvent : IEvent
{
    private byte controlNumber;
    private sbyte channel;
    private sbyte value;
    private sbyte value2;

    /// <summary>
    /// See enum ControllerNumbers [0, 255].
    /// </summary>
    public byte ControlNumber => controlNumber;

    /// <summary>
    /// Channel index in event bus [0, 15].
    /// </summary>
    public sbyte Channel => channel;

    /// <summary>
    /// Value of Controller [0, 127].
    /// </summary>
    public sbyte Value => value;

    /// <summary>
    /// [0, 127] used for pitch bend (kPitchBend) and polyPressure (kCtrlPolyPressure).
    /// </summary>
    public sbyte Value2 => value2;

    public LegacyMIDICCOutEvent(byte controlNumber, sbyte channel, sbyte value, sbyte value2)
    {
        this.controlNumber = controlNumber;
        this.channel = channel;
        this.value = value;
        this.value2 = value2;
    }

    static Event.EventTypes IEvent.Type => Event.EventTypes.kLegacyMIDICCOutEvent;
}
