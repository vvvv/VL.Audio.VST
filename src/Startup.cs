﻿using Sanford.Multimedia.Midi;
using System;
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
    private readonly HostApp context = new HostApp();

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

    ImmutableArray<IVLNodeDescription> GetNodes(IVLNodeDescriptionFactory f, IEnumerable<string> modules)
    {
        var nodes = ImmutableArray.CreateBuilder<IVLNodeDescription>();

        foreach (var p in modules)
        {
            if (!Module.TryCreate(p, out var module))
                continue;

            var pluginFactory = module.Factory;
            foreach (var info in pluginFactory.ClassInfos)
            {
                if (info.Category != ClassInfo.VstAudioEffectClass)
                    continue;

                var nodeInfo = GetNodeDescription(f, pluginFactory, info);
                nodes.Add(nodeInfo);
            }
        }

        return nodes.ToImmutable();
    }

    IVLNodeDescription GetNodeDescription(IVLNodeDescriptionFactory nodeDescriptionFactory, PluginFactory factory, ClassInfo info)
    {
        return nodeDescriptionFactory.NewNodeDescription(info.Name, "Audio.VST", fragmented: false, ctx =>
        {
            var inputs = new List<IVLPinDescription>()
            {
                ctx.Pin(EffectHost.StateInputPinName, typeof(IChannel<PluginState>)),
                ctx.Pin("Input", typeof(IEnumerable<AudioSignal>)),
                ctx.Pin("Midi In", typeof(IObservable<IMidiMessage>)),
                ctx.Pin("Parameters", typeof(IReadOnlyDictionary<string, float>)),
                ctx.Pin("Channel Prefix", typeof(string), null),
                ctx.Pin("Show Editor", typeof(bool)),
                ctx.Pin("Bypass", typeof(bool))
            };
            var outputs = new List<IVLPinDescription>()
            {
                ctx.Pin("Output", typeof(IReadOnlyList<AudioSignal>)),
                //ctx.Pin("Parameters", typeof(IReadOnlyDictionary<string, float>))
            };

            return ctx.Node(inputs, outputs, c =>
            {
                return new EffectHost(c.NodeContext, c.NodeDescription, factory, info, context);
            });
        });
    }
}