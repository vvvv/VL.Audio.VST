using Stride.Core.Mathematics;
using VL.Core;
using VL.Core.Import;
using VL.Lib.Collections;
using VL.Lib.Reactive;

namespace VL.Audio.VST;

public static class EffectHostNodes
{
    /// <inheritdoc cref="EffectHost.ShowUI(Optional{RectangleF})"/>
    public static EffectHost? ShowUI(this EffectHost? effectHost, Optional<RectangleF> bounds)
    {
        effectHost?.ShowUI(bounds);
        return effectHost;
    }

    /// <inheritdoc cref="EffectHost.HideUI"/>
    public static EffectHost? HideUI(this EffectHost? effectHost)
    {
        effectHost?.HideUI();
        return effectHost;
    }

    /// <inheritdoc cref="EffectHost.GetChannel"/>
    public static IChannel? GetChannel(this EffectHost? effectHost, string parameterName)
    {
        if (effectHost is null)
            return null;
        if (string.IsNullOrEmpty(parameterName))
            return null;

        return effectHost.GetChannel(parameterName);
    }

    /// <inheritdoc cref="EffectHost.GetParameterNames"/>
    public static Spread<string> GetParameterNames(this EffectHost? effectHost)
    {
        if (effectHost is null)
            return Spread<string>.Empty;
        return effectHost.GetParameterNames();
    }
}
