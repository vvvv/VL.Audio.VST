using VL.Core;

namespace VL.Audio.VST.Internal;

internal sealed class Pin<T> : IVLPin<T>
{
    public T? Value { get; set; }
    object? IVLPin.Value { get => Value; set => Value = (T?)value; }
}
