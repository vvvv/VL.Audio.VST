namespace VST3;

enum IoModes
{
    /// <summary>
    /// 1:1 Input / Output. Only used for Instruments. See \ref vst3IoMode
    /// </summary>
    kSimple = 0,
    /// <summary>
    /// n:m Input / Output. Only used for Instruments.
    /// </summary>
	kAdvanced,
    /// <summary>
    /// plug-in used in an offline processing context
    /// </summary>
    kOfflineProcessing
};
