using System.ComponentModel;
using System.Reactive.Linq;
using VL.Core;

namespace VL.Audio.VST.Internal;

// TODO: vvvv crashes if not public
public sealed class DynamicObject : IVLObject, INotifyPropertyChanged
{
    private PropertyChangedEventHandler? propertyChangedEventHandler;
    private IDisposable? propertyChangedSubscription;
    private int refCount;

    public required IVLTypeInfo Type { get; init; }
    public AppHost AppHost { get; init; } = AppHost.Current;
    public NodeContext Context { get; init; } = NodeContext.CurrentRoot;
    public uint Identity { get; init; }
    public IObservable<PropertyChangedEventArgs>? PropertyChangedSource { get; init; }

    // Needs to be a public event - using explicit implementation crashes Observable.FromEventPattern
    public event PropertyChangedEventHandler? PropertyChanged
    {
        add
        {
            propertyChangedEventHandler += value;

            if (Interlocked.Increment(ref refCount) == 1)
                propertyChangedSubscription = PropertyChangedSource?.Subscribe(e => propertyChangedEventHandler?.Invoke(this, e));
        }

        remove
        {
            propertyChangedEventHandler -= value;

            if (Interlocked.Decrement(ref refCount) == 0)
                propertyChangedSubscription?.Dispose();
        }
    }

    IVLObject IVLObject.With(IReadOnlyDictionary<string, object> values)
    {
        object that = this;
        foreach (var (key, value) in values)
        {
            var property = Type.GetProperty(key);
            if (property is null)
                throw new Exception($"Property '{key}' not found on type '{Type}'");
            that = property.WithValue(that, value);
        }
        return (IVLObject)that;
    }

    object? IVLObject.ReadProperty(string key)
    {
        var property = Type.GetProperty(key);
        if (property is null)
            throw new Exception($"Property '{key}' not found on type '{Type}'");
        return property.GetValue(this);
    }
}
