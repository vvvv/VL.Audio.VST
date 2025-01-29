using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using VL.Lib.Collections;
using VL.Lib.Reactive;
using VST3;

namespace VL.Audio.VST
{
    partial class EffectHost
    {
        private readonly Dictionary<string, IChannel?> channels = new();
        private Spread<string>? parameterNames;

        /// <summary>
        /// Returns the names of all parameters that can be read/controlled.
        /// </summary>
        public Spread<string> GetParameterNames()
        {
            return parameterNames ??= LoadParameterNames();

            Spread<string> LoadParameterNames()
            {
                if (controller is null)
                    return Spread<string>.Empty;

                return controller.GetParameters()
                    .Where(p => ExposeAsProperty(in p))
                    .Select(p => GetParameterFullName(in p))
                    .ToSpread();
            }
        }

        /// <summary>
        /// Returns a channel for the given parameter name.
        /// </summary>
        /// <param name="parameterName">The name of the parameter. Use <see cref="GetParameterNames"/> for a list of possible values.</param>
        /// <returns>A channel bound to the given parameter. Null if the parameter can't be found or can't be bound.</returns>
        public IChannel? GetChannel(string parameterName)
        {
            if (!channels.TryGetValue(parameterName, out var channel))
                channels[parameterName] = channel = CreateChannel();

            return channel;

            IChannel? CreateChannel()
            {
                if (controller is null)
                    return null;

                foreach (var p in controller.GetParameters())
                {
                    if (!ExposeAsProperty(in p))
                        continue;

                    var name = GetParameterFullName(in p);
                    if (name != parameterName)
                        continue;

                    var channel = ChannelHelpers.CreateChannelOfType(p.GetPinType());
                    channel.Attributes().Value = GetAttributesForParameter(in p);
                    channel.Value = p.GetCurrentValue(controller);
                    if (!p.Flags.HasFlag(ParameterInfo.ParameterFlags.kIsReadOnly))
                        channel.Subscribe(v => SetParameter(p.ID, p.Normalize(v)));
                    ParameterChanged
                        .Where(x => x.parameter.ID == p.ID)
                        .Subscribe(x => channel.Value = x.parameter.GetValueAsObject(x.normalizedValue));

                    return channel;
                }

                return null;
            }
        }

    }
}
