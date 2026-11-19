namespace VST3;

/// <summary>
/// MIDI values to normalized VST values, as the VST3 SDK converts them (public.sdk/source/vst/utility/midiconvert.h,
/// VST3 SDK 3.8.1, MIT): the value is upsampled to 32 bits so that the center maps to exactly 0.5 (64 for 7 bits,
/// 8192 for 14 bits) and the maximum to exactly 1.
/// </summary>
static class MidiConvert
{
    /// <summary>
    /// A 7-bit value (velocity, pressure, controller value) in [0, 1].
    /// </summary>
    public static double Midi7BitToNormalized(int value) => IntegralToNormalized(Upsample7To32Bit((byte)(value & 0x7F)));

    /// <summary>
    /// A 14-bit value (pitch bend, LSB | MSB &lt;&lt; 7) in [0, 1].
    /// </summary>
    public static double Midi14BitToNormalized(int value) => IntegralToNormalized(Upsample14To32Bit((ushort)(value & 0x3FFF)));

    private static uint Upsample7To32Bit(byte v)
    {
        var result = (uint)v << 25;
        if (v > 64)
        {
            var bits = (uint)(v & 0x3F);
            bits |= bits << 6; // 12 bits
            result |= (bits << 13) | (bits << 1) | (bits >> 11);
        }
        return result;
    }

    private static uint Upsample14To32Bit(ushort v)
    {
        var result = (uint)v << 18;
        if (v > 8192)
        {
            var bits = (uint)(v & 0x1FFF); // 13 bits
            result |= (bits << 5) | (bits >> 8);
        }
        return result;
    }

    private static double IntegralToNormalized(uint value)
    {
        const uint max = uint.MaxValue;
        const uint center = (max >> 1) + 1;

        if (value <= center)
            return value / (double)center / 2.0;
        return value / (double)max;
    }
}
