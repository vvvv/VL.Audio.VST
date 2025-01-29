// See https://aka.ms/new-console-template for more information
using NAudio.Wave;
using Sanford.Multimedia.Midi;
using VL.Lib.Reactive;
using VST3;

namespace VL.Audio.VST;

static class Utils
{
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
        return parameterInfo.StepCount switch
        {
            0 => typeof(float),
            1 => typeof(bool),
            _ => typeof(int)
        };
    }

    public static object GetValueAsObject(this ParameterInfo parameterInfo, double normalizedValue)
    {
        //var plain = controller.normalizedParamToPlain(parameterInfo.ID, normalizedValue);
        return parameterInfo.StepCount switch
        {
            0 => (float)normalizedValue,
            1 => normalizedValue >= 0.5,
            _ => ToDiscrete(normalizedValue, parameterInfo.StepCount)
        };
    }

    public static object GetDefaultValue(this ParameterInfo parameterInfo) => parameterInfo.GetValueAsObject(parameterInfo.DefaultNormalizedValue);

    public static object GetCurrentValue(this ParameterInfo parameterInfo, IEditController controller)
    {
        var normalized = controller.getParamNormalized(parameterInfo.ID);
        return parameterInfo.GetValueAsObject(normalized);
    }

    public static double Normalize(this ParameterInfo parameterInfo, object? value)
    {
        return parameterInfo.StepCount switch
        {
            0 => value is float f ? f : default,
            1 => value is bool b ? b ? 1d : 0d : default,
            _ => value is int i ? Normalize(i, parameterInfo.StepCount) : default
        };
    }

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

