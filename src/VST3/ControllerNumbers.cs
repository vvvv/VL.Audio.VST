namespace VST3;

/// <summary>
/// Controller Numbers (MIDI)
/// </summary>
enum ControllerNumbers : short
{
    /// <summary>Bank Select MSB</summary>
    kCtrlBankSelectMSB = 0,
    /// <summary>Modulation Wheel</summary>
    kCtrlModWheel = 1,
    /// <summary>Breath controller</summary>
    kCtrlBreath = 2,
    /// <summary>Foot Controller</summary>
    kCtrlFoot = 4,
    /// <summary>Portamento Time</summary>
    kCtrlPortaTime = 5,
    /// <summary>Data Entry MSB</summary>
    kCtrlDataEntryMSB = 6,
    /// <summary>Channel Volume (formerly Main Volume)</summary>
    kCtrlVolume = 7,
    /// <summary>Balance</summary>
    kCtrlBalance = 8,
    /// <summary>Pan</summary>
    kCtrlPan = 10,
    /// <summary>Expression</summary>
    kCtrlExpression = 11,
    /// <summary>Effect Control 1</summary>
    kCtrlEffect1 = 12,
    /// <summary>Effect Control 2</summary>
    kCtrlEffect2 = 13,
    /// <summary>General Purpose Controller #1</summary>
    kCtrlGPC1 = 16,
    /// <summary>General Purpose Controller #2</summary>
    kCtrlGPC2 = 17,
    /// <summary>General Purpose Controller #3</summary>
    kCtrlGPC3 = 18,
    /// <summary>General Purpose Controller #4</summary>
    kCtrlGPC4 = 19,
    /// <summary>Bank Select LSB</summary>
    kCtrlBankSelectLSB = 32,
    /// <summary>Data Entry LSB</summary>
    kCtrlDataEntryLSB = 38,
    /// <summary>Damper Pedal On/Off (Sustain)</summary>
    kCtrlSustainOnOff = 64,
    /// <summary>Portamento On/Off</summary>
    kCtrlPortaOnOff = 65,
    /// <summary>Sustenuto On/Off</summary>
    kCtrlSustenutoOnOff = 66,
    /// <summary>Soft Pedal On/Off</summary>
    kCtrlSoftPedalOnOff = 67,
    /// <summary>Legato Footswitch On/Off</summary>
    kCtrlLegatoFootSwOnOff = 68,
    /// <summary>Hold 2 On/Off</summary>
    kCtrlHold2OnOff = 69,
    /// <summary>Sound Variation</summary>
    kCtrlSoundVariation = 70,
    /// <summary>Filter Cutoff (Timbre/Harmonic Intensity)</summary>
    kCtrlFilterCutoff = 71,
    /// <summary>Release Time</summary>
    kCtrlReleaseTime = 72,
    /// <summary>Attack Time</summary>
    kCtrlAttackTime = 73,
    /// <summary>Filter Resonance (Brightness)</summary>
    kCtrlFilterResonance = 74,
    /// <summary>Decay Time</summary>
    kCtrlDecayTime = 75,
    /// <summary>Vibrato Rate</summary>
    kCtrlVibratoRate = 76,
    /// <summary>Vibrato Depth</summary>
    kCtrlVibratoDepth = 77,
    /// <summary>Vibrato Delay</summary>
    kCtrlVibratoDelay = 78,
    /// <summary>undefined</summary>
    kCtrlSoundCtrler10 = 79,
    /// <summary>General Purpose Controller #5</summary>
    kCtrlGPC5 = 80,
    /// <summary>General Purpose Controller #6</summary>
    kCtrlGPC6 = 81,
    /// <summary>General Purpose Controller #7</summary>
    kCtrlGPC7 = 82,
    /// <summary>General Purpose Controller #8</summary>
    kCtrlGPC8 = 83,
    /// <summary>Portamento Control</summary>
    kCtrlPortaControl = 84,
    /// <summary>Effect 1 Depth (Reverb Send Level)</summary>
    kCtrlEff1Depth = 91,
    /// <summary>Effect 2 Depth (Tremolo Level)</summary>
    kCtrlEff2Depth = 92,
    /// <summary>Effect 3 Depth (Chorus Send Level)</summary>
    kCtrlEff3Depth = 93,
    /// <summary>Effect 4 Depth (Delay/Variation/Detune Level)</summary>
    kCtrlEff4Depth = 94,
    /// <summary>Effect 5 Depth (Phaser Level)</summary>
    kCtrlEff5Depth = 95,
    /// <summary>Data Increment (+1)</summary>
    kCtrlDataIncrement = 96,
    /// <summary>Data Decrement (-1)</summary>
    kCtrlDataDecrement = 97,
    /// <summary>NRPN Select LSB</summary>
    kCtrlNRPNSelectLSB = 98,
    /// <summary>NRPN Select MSB</summary>
    kCtrlNRPNSelectMSB = 99,
    /// <summary>RPN Select LSB</summary>
    kCtrlRPNSelectLSB = 100,
    /// <summary>RPN Select MSB</summary>
    kCtrlRPNSelectMSB = 101,
    /// <summary>All Sounds Off</summary>
    kCtrlAllSoundsOff = 120,
    /// <summary>Reset All Controllers</summary>
    kCtrlResetAllCtrlers = 121,
    /// <summary>Local Control On/Off</summary>
    kCtrlLocalCtrlOnOff = 122,
    /// <summary>All Notes Off</summary>
    kCtrlAllNotesOff = 123,
    /// <summary>Omni Mode Off + All Notes Off</summary>
    kCtrlOmniModeOff = 124,
    /// <summary>Omni Mode On + All Notes Off</summary>
    kCtrlOmniModeOn = 125,
    /// <summary>Poly Mode On/Off + All Sounds Off</summary>
    kCtrlPolyModeOnOff = 126,
    /// <summary>Poly Mode On</summary>
    kCtrlPolyModeOn = 127,
    /// <summary>After Touch (associated to Channel Pressure)</summary>
    kAfterTouch = 128,
    /// <summary>Pitch Bend Change</summary>
    kPitchBend = 129,
    /// <summary>Count of Controller Number</summary>
    kCountCtrlNumber,
    /// <summary>Program Change (use LegacyMIDICCOutEvent.value only)</summary>
    kCtrlProgramChange = 130,
    /// <summary>Polyphonic Key Pressure (use LegacyMIDICCOutEvent.value for pitch and LegacyMIDICCOutEvent.value2 for pressure)</summary>
    kCtrlPolyPressure = 131,
    /// <summary>Quarter Frame (use LegacyMIDICCOutEvent.value only)</summary>
    kCtrlQuarterFrame = 132
}