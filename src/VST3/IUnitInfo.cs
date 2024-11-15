using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using ProgramListID = int;
using UnitID = int;

namespace VST3;

/// <summary>
/// Edit controller extension to describe the plug-in structure: Vst::IUnitInfo
/// </summary>
/// <remarks>
/// <para>IUnitInfo describes the internal structure of the plug-in.</para>
/// <list type="bullet">
/// <item>The root unit is the component itself, so getUnitCount must return 1 at least.</item>
/// <item>The root unit id has to be 0 (kRootUnitId).</item>
/// <item>Each unit can reference one program list - this reference must not change.</item>
/// <item>Each unit, using a program list, references one program of the list.</item>
/// </list>
/// </remarks>
/// <seealso cref="IUnitHandler"/>
[GeneratedComInterface]
[Guid("3d4bd6b5-913a-4fd2-a886-e768a5eb92c1")]
partial interface IUnitInfo
{
    /// <summary>
    /// Returns the flat count of units.
    /// </summary>
    [PreserveSig]
    int getUnitCount();

    /// <summary>
    /// Gets UnitInfo for a given index in the flat list of unit.
    /// </summary>
    /// <param name="unitIndex">The index of the unit.</param>
    /// <returns>The UnitInfo for the given index.</returns>
    UnitInfo getUnitInfo(int unitIndex);

    /// <summary>
    /// Gets the count of Program List.
    /// </summary>
    [PreserveSig]
    int getProgramListCount();

    /// <summary>
    /// Gets for a given index the Program List Info.
    /// </summary>
    /// <param name="listIndex">The index of the program list.</param>
    /// <returns>The ProgramListInfo for the given index.</returns>
    ProgramListInfo getProgramListInfo(int listIndex);

    /// <summary>
    /// Gets for a given program list ID and program index its program name.
    /// </summary>
    /// <param name="listId">The program list ID.</param>
    /// <param name="programIndex">The program index.</param>
    /// <param name="name">The program name.</param>
    void getProgramName(ProgramListID listId, int programIndex, String128 name);

    /// <summary>
    /// Gets for a given program list ID, program index and attributeId the associated attribute value.
    /// </summary>
    /// <param name="listId">The program list ID.</param>
    /// <param name="programIndex">The program index.</param>
    /// <param name="attributeId">The attribute ID.</param>
    /// <param name="attributeValue">The attribute value.</param>
    void getProgramInfo(ProgramListID listId, int programIndex, [MarshalAs(UnmanagedType.LPStr)] string attributeId, String128 attributeValue);

    /// <summary>
    /// Returns kResultTrue if the given program index of a given program list ID supports PitchNames.
    /// </summary>
    /// <param name="listId">The program list ID.</param>
    /// <param name="programIndex">The program index.</param>
    /// <returns>True if the given program index supports PitchNames, otherwise false.</returns>
    [PreserveSig]
    [return: MarshalUsing(typeof(VstBoolMarshaller))]
    bool hasProgramPitchNames(ProgramListID listId, int programIndex);

    /// <summary>
    /// Gets the PitchName for a given program list ID, program index and pitch.
    /// If PitchNames are changed the plug-in should inform the host with IUnitHandler::notifyProgramListChange.
    /// </summary>
    /// <param name="listId">The program list ID.</param>
    /// <param name="programIndex">The program index.</param>
    /// <param name="midiPitch">The MIDI pitch.</param>
    /// <param name="name">The pitch name.</param>
    void getProgramPitchName(ProgramListID listId, int programIndex, short midiPitch, String128 name);

    /// <summary>
    /// Gets the current selected unit.
    /// </summary>
    [PreserveSig]
    UnitID getSelectedUnit();

    /// <summary>
    /// Sets a new selected unit.
    /// </summary>
    /// <param name="unitId">The unit ID.</param>
    void selectUnit(UnitID unitId);

    /// <summary>
    /// Gets the according unit if there is an unambiguous relation between a channel or a bus and a unit.
    /// This method mainly is intended to find out which unit is related to a given MIDI input channel.
    /// </summary>
    /// <param name="type">The media type.</param>
    /// <param name="dir">The bus direction.</param>
    /// <param name="busIndex">The bus index.</param>
    /// <param name="channel">The channel.</param>
    /// <param name="unitID">The unit ID.</param>
    /// <returns>True if there is an unambiguous relation, otherwise false.</returns>
    [PreserveSig]
    [return: MarshalUsing(typeof(VstBoolMarshaller))]
    bool getUnitByBus(MediaTypes type, BusDirections dir, int busIndex, int channel, out UnitID unitID);

    /// <summary>
    /// Receives a preset data stream.
    /// - If the component supports program list data (IProgramListData), the destination of the data
    /// stream is the program specified by list-Id and program index (first and second parameter).
    /// - If the component supports unit data (IUnitData), the destination is the unit specified by the first
    /// parameter - in this case parameter programIndex is &lt; 0).
    /// </summary>
    /// <param name="listOrUnitId">The list or unit ID.</param>
    /// <param name="programIndex">The program index.</param>
    /// <param name="data">The data stream.</param>
    void setUnitProgramData(int listOrUnitId, int programIndex, IBStream data);
}

/// <summary>
/// Host callback for unit support: Vst::IUnitHandler
/// </summary>
/// <remarks>
/// Host callback interface, used with IUnitInfo.
/// Retrieve via queryInterface from IComponentHandler.
/// </remarks>
/// <seealso cref="IUnitInfo"/>
[GeneratedComInterface]
[Guid("4b5147f8-4654-486b-8dab-30ba163a3c56")]
partial interface IUnitHandler
{
    /// <summary>
    /// Notify host when a module is selected in plug-in GUI.
    /// </summary>
    /// <param name="unitId">The unit ID.</param>
    void notifyUnitSelection(UnitID unitId);

    /// <summary>
    /// Tell host that the plug-in controller changed a program list (rename, load, PitchName changes).
    /// </summary>
    /// <param name="listId">The specified program list ID to inform.</param>
    /// <param name="programIndex">When kAllProgramInvalid, all program information is invalid, otherwise only the program of given index.</param>
    void notifyProgramListChange(ProgramListID listId, int programIndex);
}

/// <summary>
/// Basic Unit Description.
/// </summary>
/// <seealso cref="IUnitInfo"/>
struct UnitInfo
{
    private UnitID id;
    private UnitID parentUnitId;
    private String128 name;
    private ProgramListID programListId;

    /// <summary>
    /// Unit identifier.
    /// </summary>
    public UnitID Id => id;

    /// <summary>
    /// Identifier of parent unit (kNoParentUnitId: does not apply, this unit is the root).
    /// </summary>
    public UnitID ParentUnitId => parentUnitId;

    /// <summary>
    /// Name, optional for the root component, required otherwise.
    /// </summary>
    public string Name => name.ToString();

    /// <summary>
    /// ID of program list used in unit (kNoProgramListId = no programs used in this unit).
    /// </summary>
    public ProgramListID ProgramListId => programListId;
}

/// <summary>
/// Basic Program List Description.
/// </summary>
/// <seealso cref="IUnitInfo"/>
struct ProgramListInfo
{
    private ProgramListID id;
    private String128 name;
    private int programCount;

    /// <summary>
    /// Program list identifier.
    /// </summary>
    public ProgramListID Id => id;

    /// <summary>
    /// Name of program list.
    /// </summary>
    public string Name => name.ToString();

    /// <summary>
    /// Number of programs in this list.
    /// </summary>
    public int ProgramCount => programCount;
}

static partial class Constants
{
    /// <summary>
    /// Special UnitIDs for UnitInfo
    /// </summary>

    /// <summary>
    /// Identifier for the top level unit (root).
    /// </summary>
    public const UnitID kRootUnitId = 0;

    /// <summary>
    /// Used for the root unit which does not have a parent.
    /// </summary>
    public const UnitID kNoParentUnitId = -1;

    /// <summary>
    /// No programs are used in the unit.
    /// </summary>
    public const ProgramListID kNoProgramListId = -1;

    /// <summary>
    /// Special programIndex value for IUnitHandler::notifyProgramListChange
    /// all program information is invalid.
    /// </summary>
    public const int kAllProgramInvalid = -1;
}
