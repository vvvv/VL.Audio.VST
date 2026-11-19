namespace VST3;

unsafe struct String128
{
    fixed char value[128];

    /// <summary>
    /// The string, cut to 127 characters and null terminated.
    /// </summary>
    public static String128 From(string s)
    {
        var result = default(String128);
        var length = Math.Min(s.Length, 127);
        s.AsSpan(0, length).CopyTo(new Span<char>(result.value, 128));
        return result;
    }

    public override string ToString()
    {
        fixed (char* value = this.value)
            return new string(value);
    }
}
