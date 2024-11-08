// See https://aka.ms/new-console-template for more information
using NAudio.Wave;
using Sanford.Multimedia.Midi;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using VL.Lib.Reactive;
using VST3;

namespace VL.Audio.VST;

static class Utils
{
    public static void ReleaseComObject(this object obj)
    {
        if (obj is ComObject com && IsUniqueInstance(com))
        {
            // See https://github.com/dotnet/runtime/issues/96901
            GC.SuppressFinalize(com);
            com.FinalRelease();
        }

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_UniqueInstance")]
        extern static bool IsUniqueInstance(ComObject comObject);
    }

    public static nint GetComPtr(this object? obj, in Guid guid)
    {
        if (obj is null)
            return default;

        var pUnk = VstWrappers.Instance.GetOrCreateComInterfaceForObject(obj, CreateComInterfaceFlags.None);
        Marshal.QueryInterface(pUnk, in guid, out var pInt);
        Marshal.Release(pUnk);
        return pInt;
    }

    public static void SetProcessing_IgnoreNotImplementedException(this IAudioProcessor processor, bool state)
    {
        try
        {
            processor.setProcessing(state);
        }
        catch (NotImplementedException)
        {
            // https://forums.steinberg.net/t/iaudioprocessor-setprocessing-fails-in-many-plugins/785558
        }
    }

    public static void IgnoreNotImplementedException<T>(this T obj, Action<T> action)
    {
        try
        {
            action(obj);
        }
        catch (NotImplementedException)
        {
        }
    }

    public static SymbolicSampleSizes GetSymbolicSampleSizes(WaveFormat waveFormat)
    {
        if (waveFormat.BitsPerSample == 32)
            return SymbolicSampleSizes.Sample32;
        if (waveFormat.BitsPerSample == 64)
            return SymbolicSampleSizes.Sample64;
        throw new NotImplementedException();
    }

    public static Type GetPinType(this ParameterInfo parameterInfo)
    {
        return parameterInfo.GetStepCount() switch
        {
            0 => typeof(float),
            1 => typeof(bool),
            _ => typeof(int)
        };
    }

    public static object GetValueAsObject(this ParameterInfo parameterInfo, double normalizedValue)
    {
        //var plain = controller.normalizedParamToPlain(parameterInfo.ID, normalizedValue);
        return parameterInfo.GetStepCount() switch
        {
            0 => (float)normalizedValue,
            1 => normalizedValue >= 0.5,
            _ => ToDiscrete(normalizedValue, parameterInfo.GetStepCount())
        };
    }

    public static object GetDefaultValue(this ParameterInfo parameterInfo) => GetValueAsObject(parameterInfo, parameterInfo.DefaultNormalizedValue);

    public static object GetCurrentValue(this ParameterInfo parameterInfo, IEditController controller)
    {
        var normalized = controller.getParamNormalized(parameterInfo.ID);
        return parameterInfo.GetValueAsObject(normalized);
    }

    public static double Normalize(this ParameterInfo parameterInfo, object value)
    {
        return parameterInfo.GetStepCount() switch
        {
            0 => value is float f ? f : default,
            1 => value is bool b ? b ? 1d : 0d : default,
            _ => value is int i ? ToDiscrete(i, parameterInfo.GetStepCount()) : default
        };
    }

    public static int GetStepCount(this ParameterInfo parameterInfo) => Math.Max(0, parameterInfo.StepCount);

    public static double Normalize(int discrete, int stepCount) => discrete / (double)stepCount;

    public static int ToDiscrete(double normalized, int stepCount) => (int)Math.Min(stepCount, normalized * (stepCount + 1));

    public static bool ExposeAsPin(this ParameterInfo parameterInfo)
    {
        if (parameterInfo.Flags.HasFlag(ParameterInfo.ParameterFlags.kIsHidden))
            return false;

        return parameterInfo.Flags.HasFlag(ParameterInfo.ParameterFlags.kCanAutomate);
    }

    public static IEnumerable<ParameterInfo> GetParameters(this IEditController controller)
    {
        var count = controller.getParameterCount();
        for (var i = 0; i < count; i++)
        {
            var info = controller.getParameterInfo(i);
            yield return info;
        }
    }

    public static IEnumerable<BusInfo> GetBusInfos(this IComponent component, MediaTypes mediaTypes, BusDirections dir)
    {
        var count = component.getBusCount(mediaTypes, dir);
        for (var i = 0; i < count; i++)
        {
            var info = component.getBusInfo(mediaTypes, dir, i);
            yield return info;
        }
    }

    public static IEnumerable<UnitInfo> GetUnitInfos(this IUnitInfo controller)
    {
        var count = controller.getUnitCount();
        for (var i = 0; i < count; i++)
        {
            var info = controller.getUnitInfo(i);
            yield return info;
        }
    }

    public static IDisposable SubscribeAndRunOnce<T>(this IChannel<T> channel, Action<T?> action)
    {
        action(channel.Value);
        return channel.Subscribe(action);
    }

    public static bool IsNoteOn(this ChannelMessage message) => message.Command == ChannelCommand.NoteOn && message.Data2 > 0;
    public static bool IsNoteOff(this ChannelMessage message) => message.Command == ChannelCommand.NoteOff || message.Command == ChannelCommand.NoteOn && message.Data2 == 0;
}

