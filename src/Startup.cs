using Sanford.Multimedia.Midi;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VL.Audio.VST;
using VL.Core;
using VL.Core.CompilerServices;
using VL.Lib.Reactive;
using VST3;
using VST3.Hosting;

[assembly:AssemblyInitializer(typeof(Startup))]

namespace VL.Audio.VST;

public sealed class Startup : AssemblyInitializer<Startup>
{
    private static readonly ConcurrentDictionary<string, ImmutableArray<ClassInfo>> s_classInfos = new();

    public override void Configure(AppHost appHost)
    {
        appHost.RegisterNodeFactory("VL.Audio.VST.Nodes", f =>
        {
            var nodes = GetNodes(f, Module.GetModulePaths());
            return NodeBuilding.NewFactoryImpl(nodes, forPath: path =>
            {
                var vstPath = Path.Combine(path, "vst3");
                if (!Directory.Exists(vstPath))
                    return null;

                return f =>
                {
                    var nodes = GetNodes(f, Module.GetModulePaths(vstPath));
                    return NodeBuilding.NewFactoryImpl(nodes);
                };
            });
        });

        appHost.Factory.RegisterSerializer<PluginState, PluginState.Serializer>(PluginState.Serializer.Instance);

        base.Configure(appHost);
    }

    ImmutableArray<ClassInfo> GetClassInfos(string path)
    {
        return s_classInfos.GetOrAdd(path, p =>
        {
            var builder = ImmutableArray.CreateBuilder<ClassInfo>();
            if (Module.TryCreate(p, out var module))
            {
                using (module)
                {
                    builder.AddRange(module.Factory.ClassInfos);
                }
            }
            return builder.ToImmutable();
        });
    }

    ImmutableArray<IVLNodeDescription> GetNodes(IVLNodeDescriptionFactory f, IEnumerable<string> modules)
    {
        var nodes = ImmutableArray.CreateBuilder<IVLNodeDescription>();

        foreach (var p in modules)
        {
            foreach (var info in GetClassInfos(p))
            {
                if (info.Category != ClassInfo.VstAudioEffectClass)
                    continue;

                var nodeInfo = GetNodeDescription(f, p, info);
                nodes.Add(nodeInfo);
            }
        }

        return nodes.ToImmutable();
    }

    IVLNodeDescription GetNodeDescription(IVLNodeDescriptionFactory nodeDescriptionFactory, string modulePath, ClassInfo info)
    {
        const string mainCategory = "Audio.VST";

        var subCategory =
            info.SubCategories.Contains("Fx", StringComparer.OrdinalIgnoreCase) ? "Effect" :
            info.SubCategories.Contains("Instrument", StringComparer.OrdinalIgnoreCase) ? "Instrument" :
            "Other";

        return nodeDescriptionFactory.NewNodeDescription(info.Name, $"{mainCategory}.{subCategory}", fragmented: false, invalidated: null, tags: string.Join(',', info.SubCategories), init: ctx =>
        {
            var inputs = new List<IVLPinDescription>()
            {
                ctx.Pin(EffectHost.StateInputPinName, typeof(IChannel<PluginState>)),
                ctx.Pin("Input", typeof(IEnumerable<AudioSignal>)),
                ctx.Pin("Midi In", typeof(IObservable<IMidiMessage>)),
                ctx.Pin("Parameters", typeof(IReadOnlyDictionary<string, float>)),
                ctx.Pin("Channel Prefix", typeof(string), null),
                ctx.Pin("Show Editor", typeof(bool)),
                ctx.Pin("Apply", typeof(bool), defaultValue: true)
            };
            var outputs = new List<IVLPinDescription>()
            {
                ctx.Pin("Output", typeof(IReadOnlyList<AudioSignal>)),
                //ctx.Pin("Parameters", typeof(IReadOnlyDictionary<string, float>))
            };

            return ctx.Node(inputs, outputs, c =>
            {
                return new EffectHost(c.NodeContext, c.NodeDescription, modulePath, info);
            });
        });
    }
}
