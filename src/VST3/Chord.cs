namespace VST3;

struct Chord
{
    /// <summary>
    /// key note in chord
    /// </summary>
    byte keyNote;

    /// <summary>
    /// lowest note in chord
    /// </summary>
    byte rootNote;

    /** Bitmask of a chord.
        1st bit set: minor second; 2nd bit set: major second, and so on. \n
        There is \b no bit for the keynote (root of the chord) because it is inherently always present. \n
        Examples:
        - XXXX 0000 0100 1000 (= 0x0048) -> major chord\n
        - XXXX 0000 0100 0100 (= 0x0044) -> minor chord\n
        - XXXX 0010 0100 0100 (= 0x0244) -> minor chord with minor seventh  */

    short chordMask;

    public enum Masks
    {
        /// <summary>
        /// mask for chordMask
        /// </summary>
        kChordMask = 0x0FFF,
        /// <summary>
        /// reserved for future use
        /// </summary>
		kReservedMask = 0xF000
    }
}
