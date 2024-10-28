using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using ProgramListID = int;
using UnitID = int;

namespace VST3;

/** Edit controller extension to describe the plug-in structure: Vst::IUnitInfo
\ingroup vstIPlug vst300
- [plug imp]
- [extends IEditController]
- [released: 3.0.0]
- [optional]

IUnitInfo describes the internal structure of the plug-in.
- The root unit is the component itself, so getUnitCount must return 1 at least.
- The root unit id has to be 0 (kRootUnitId).
- Each unit can reference one program list - this reference must not change.
- Each unit, using a program list, references one program of the list.

\see \ref vst3Units, IUnitHandler
*/
[GeneratedComInterface]
[Guid("3d4bd6b5-913a-4fd2-a886-e768a5eb92c1")]
partial interface IUnitInfo
{
	/** Returns the flat count of units. */
	[PreserveSig] int getUnitCount();

    /** Gets UnitInfo for a given index in the flat list of unit. */
    UnitInfo getUnitInfo(int unitIndex);

	/** Component intern program structure. */
	/** Gets the count of Program List. */
	[PreserveSig] int getProgramListCount();

    /** Gets for a given index the Program List Info. */
    ProgramListInfo getProgramListInfo(int listIndex);

    /** Gets for a given program list ID and program index its program name. */
    void getProgramName(ProgramListID listId, int programIndex, String128 name /*out*/);

	/** Gets for a given program list ID, program index and attributeId the associated attribute value. */
	void getProgramInfo(ProgramListID listId, int programIndex,
		[MarshalAs(UnmanagedType.LPStr)] string attributeId /*in*/, String128 attributeValue /*out*/);

	/** Returns kResultTrue if the given program index of a given program list ID supports PitchNames. */
	[return: MarshalAs(UnmanagedType.Bool)] bool hasProgramPitchNames(ProgramListID listId, int programIndex);

	/** Gets the PitchName for a given program list ID, program index and pitch.
		If PitchNames are changed the plug-in should inform the host with IUnitHandler::notifyProgramListChange. */
	void getProgramPitchName(ProgramListID listId, int programIndex,
		short midiPitch, String128 name /*out*/);

	// units selection --------------------
	/** Gets the current selected unit. */
	[PreserveSig] UnitID getSelectedUnit();

	/** Sets a new selected unit. */
	void selectUnit(UnitID unitId);

    /** Gets the according unit if there is an unambiguous relation between a channel or a bus and a unit.
	    This method mainly is intended to find out which unit is related to a given MIDI input channel. */
    UnitID getUnitByBus(MediaTypes type, BusDirections dir, int busIndex, int channel);

	/** Receives a preset data stream.
	    - If the component supports program list data (IProgramListData), the destination of the data
		  stream is the program specified by list-Id and program index (first and second parameter)
		- If the component supports unit data (IUnitData), the destination is the unit specified by the first
		  parameter - in this case parameter programIndex is < 0). */
	void setUnitProgramData(int listOrUnitId, int programIndex, IBStream data);
};

//------------------------------------------------------------------------
/** Host callback for unit support: Vst::IUnitHandler
\ingroup vstIHost vst300
- [host imp]
- [extends IComponentHandler]
- [released: 3.0.0]
- [optional]

Host callback interface, used with IUnitInfo.
Retrieve via queryInterface from IComponentHandler.

\see \ref vst3Units, IUnitInfo
*/
[GeneratedComInterface]
[Guid("4b5147f8-4654-486b-8dab-30ba163a3c56")]
partial interface IUnitHandler
{
    /** Notify host when a module is selected in plug-in GUI. */
    void notifyUnitSelection(UnitID unitId);

	/** Tell host that the plug-in controller changed a program list (rename, load, PitchName changes).
	    \param listId is the specified program list ID to inform.
		\param programIndex : when kAllProgramInvalid, all program information is invalid, otherwise only the program of given index. */
	void notifyProgramListChange(ProgramListID listId, int programIndex);
};

//------------------------------------------------------------------------
/** Basic Unit Description.
\see IUnitInfo
*/
struct UnitInfo
{
    UnitID id;						/// unit identifier
	UnitID parentUnitId;			/// identifier of parent unit (kNoParentUnitId: does not apply, this unit is the root)
	String128 name;					/// name, optional for the root component, required otherwise
	ProgramListID programListId;    /// id of program list used in unit (kNoProgramListId = no programs used in this unit)
    public UnitID Id => id;
    public UnitID ParentUnitId => parentUnitId;
    public string Name => name.ToString();
    public ProgramListID ProgramCount => programListId;
};

//------------------------------------------------------------------------
/** Basic Program List Description.
\see IUnitInfo
*/
struct ProgramListInfo
{
    ProgramListID id;				
	String128 name;					
	int programCount;

    /// <summary>
    /// program list identifier
    /// </summary>
    public ProgramListID Id => id;

    /// <summary>
    /// name of program list
    /// </summary>
    public string Name => name.ToString();

    /// <summary>
    /// number of programs in this list
    /// </summary>
    public int ProgramCount => programCount;
};

static partial class Constants
{
    /** Special UnitIDs for UnitInfo */

    /// <summary>
    /// identifier for the top level unit (root)
    /// </summary>
    public const UnitID kRootUnitId = 0;

    /// <summary>
    /// used for the root unit which does not have a parent.
    /// </summary>
    public const UnitID kNoParentUnitId = -1;

    /// <summary>
    /// no programs are used in the unit.
    /// </summary>
    public const ProgramListID kNoProgramListId = -1;

    /// <summary>
	/// Special programIndex value for IUnitHandler::notifyProgramListChange
	/// all program information is invalid
	/// </summary>
    public const int kAllProgramInvalid = -1;
}