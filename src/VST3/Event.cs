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

//------------------------------------------------------------------------
/** Note-on event specific data. Used in \ref Event (union)
\ingroup vstEventGrp
Pitch uses the twelve-tone equal temperament tuning (12-TET).
 */
struct NoteOnEvent : IEvent
{
    short channel;		/// channel index in event bus
	short pitch;		/// range [0, 127] = [C-2, G8] with A3=440Hz (12-TET: twelve-tone equal temperament)
	float tuning;		/// 1.f = +1 cent, -1.f = -1 cent
	float velocity;		/// range [0.0, 1.0]
	int length;		    /// in sample frames (optional, Note Off has to follow in any case!)
	int noteId;         /// note identifier (if not available then -1)

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
};

//------------------------------------------------------------------------
/** Note-off event specific data. Used in \ref Event (union)
\ingroup vstEventGrp 
*/
struct NoteOffEvent : IEvent
{
    short channel;		/// channel index in event bus
	short pitch;		/// range [0, 127] = [C-2, G8] with A3=440Hz (12-TET)
	float velocity;		/// range [0.0, 1.0]
	int noteId;		/// associated noteOn identifier (if not available then -1)
	float tuning;

    public NoteOffEvent(short channel, short pitch, float velocity, int noteId, float tuning)
    {
        this.channel = channel;
        this.pitch = pitch;
        this.velocity = velocity;
        this.noteId = noteId;
        this.tuning = tuning;
    }

    /// 1.f = +1 cent, -1.f = -1 cent

    static Event.EventTypes IEvent.Type => Event.EventTypes.kNoteOffEvent;
};

//------------------------------------------------------------------------
/** Data event specific data. Used in \ref Event (union)
\ingroup vstEventGrp 
*/
unsafe struct DataEvent : IEvent
{
    uint size;		/// size in bytes of the data block bytes
	uint type;		/// type of this data block (see \ref DataTypes)
	byte* bytes; /// pointer to the data block

    /** Value for DataEvent::type */
    enum DataTypes
    {
        kMidiSysEx = 0	/// for MIDI system exclusive message
	};

    static Event.EventTypes IEvent.Type => Event.EventTypes.kDataEvent;
};

//------------------------------------------------------------------------
/** PolyPressure event specific data. Used in \ref Event (union)
\ingroup vstEventGrp
*/
struct PolyPressureEvent : IEvent
{
    short channel;		/// channel index in event bus
	short pitch;		/// range [0, 127] = [C-2, G8] with A3=440Hz
	float pressure;		/// range [0.0, 1.0]
	int noteId;     /// event should be applied to the noteId (if not -1)

    static Event.EventTypes IEvent.Type => Event.EventTypes.kPolyPressureEvent;
};

//------------------------------------------------------------------------
/** Chord event specific data. Used in \ref Event (union)
\ingroup vstEventGrp 
*/
unsafe struct ChordEvent : IEvent
{
    short root;			/// range [0, 127] = [C-2, G8] with A3=440Hz
	short bassNote;		/// range [0, 127] = [C-2, G8] with A3=440Hz
	short mask;			/// root is bit 0
	ushort textLen;     /// the number of characters (TChar) between the beginning of text and the terminating
                        /// null character (without including the terminating null character itself)
    char* text; /// UTF-16, null terminated Hosts Chord Name

    static Event.EventTypes IEvent.Type => Event.EventTypes.kChordEvent;
};

//------------------------------------------------------------------------
/** Scale event specific data. Used in \ref Event (union)
\ingroup vstEventGrp 
*/
unsafe struct ScaleEvent : IEvent
{
    short root;			/// range [0, 127] = root Note/Transpose Factor
	short mask;			/// Bit 0 =  C,  Bit 1 = C#, ... (0x5ab5 = Major Scale)
	ushort textLen;     /// the number of characters (TChar) between the beginning of text and the terminating
                        /// null character (without including the terminating null character itself)
    char* text; /// UTF-16, null terminated, Hosts Scale Name

    static Event.EventTypes IEvent.Type => Event.EventTypes.kScaleEvent;
};

//------------------------------------------------------------------------
/** Legacy MIDI CC Out event specific data. Used in \ref Event (union)
\ingroup vstEventGrp
- [released: 3.6.12]

This kind of event is reserved for generating MIDI CC as output event for kEvent Bus during the process call.
 */
struct LegacyMIDICCOutEvent : IEvent
{
    byte controlNumber;/// see enum ControllerNumbers [0, 255]
	sbyte channel;		/// channel index in event bus [0, 15]
	sbyte value;			/// value of Controller [0, 127]
	sbyte value2;       /// [0, 127] used for pitch bend (kPitchBend) and polyPressure (kCtrlPolyPressure)

    static Event.EventTypes IEvent.Type => Event.EventTypes.kLegacyMIDICCOutEvent;
};