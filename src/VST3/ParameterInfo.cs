namespace VST3;

//------------------------------------------------------------------------
/** Controller Parameter Info.
 *	A parameter info describes a parameter of the controller.
 *	The id must always be the same for a parameter as this uniquely identifies the parameter.
 */
struct ParameterInfo
{
    ParamID id;				/// unique identifier of this parameter (named tag too)
	String128 title;		/// parameter title (e.g. "Volume")
	String128 shortTitle;	/// parameter shortTitle (e.g. "Vol")
	String128 units;		/// parameter unit (e.g. "dB")
	int stepCount;        /// number of discrete steps (0: continuous, 1: toggle, discrete value otherwise 
                          /// (corresponding to max - min, for example: 127 for a min = 0 and a max = 127) - see \ref vst3ParameterIntro)
    double defaultNormalizedValue;	/// default normalized value [0,1] (in case of discrete value: defaultNormalizedValue = defDiscreteValue / stepCount)
	int unitId;          /// id of unit this parameter belongs to (see \ref vst3Units)

    ParameterFlags flags;			/// ParameterFlags (see below)

    [Flags]
    public enum ParameterFlags : int
    {
        kNoFlags = 0,		/// no flags wanted
		kCanAutomate = 1 << 0,	/// parameter can be automated
		kIsReadOnly = 1 << 1,	/// parameter cannot be changed from outside the plug-in (implies that kCanAutomate is NOT set)
		kIsWrapAround = 1 << 2,	/// attempts to set the parameter value out of the limits will result in a wrap around [SDK 3.0.2]
		kIsList = 1 << 3,	/// parameter should be displayed as list in generic editor or automation editing [SDK 3.1.0]
		kIsHidden = 1 << 4, /// parameter should be NOT displayed and cannot be changed from outside the plug-in 
                            /// (implies that kCanAutomate is NOT set and kIsReadOnly is set) [SDK 3.7.0]

        kIsProgramChange = 1 << 15, /// parameter is a program change (unitId gives info about associated unit 
                                    /// - see \ref vst3ProgramLists)
        kIsBypass = 1 << 16 /// special bypass parameter (only one allowed): plug-in can handle bypass
                            /// (highly recommended to export a bypass parameter for effect plug-in)
    }

    public ParamID ID => id;
    public string Title => title.ToString();
    public string ShortTitle => shortTitle.ToString();
    public string Units => units.ToString();
    public int StepCount => stepCount;
    public double DefaultNormalizedValue => defaultNormalizedValue;
    public int UnitId => unitId;
    public ParameterFlags Flags => flags;
};
