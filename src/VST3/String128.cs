namespace VST3;

unsafe struct String128
{
    fixed char value[128];

    public override string ToString()
    {
        fixed (char* value = this.value)
            return new string(value);
    }
}
