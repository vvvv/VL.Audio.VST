using System.Collections.Concurrent;
using System.Collections.Immutable;
using VL.Core.Import;
using VST3.Hosting;

namespace VL.Audio.VST.Internal;

sealed class EffectHostFactory : ProcessNodeFactory
{
    private static readonly ConcurrentDictionary<string, ImmutableArray<ClassInfo>> s_classInfos = new();

    public override IEnumerable<Node> GetNodes()
    {
        return GetNodes(Module.GetModulePaths());
    }

    public override IEnumerable<Node> GetNodesForPath(string path)
    {
        var vstPath = Path.Combine(path, "vst3");
        if (Directory.Exists(vstPath))
            return GetNodes(Module.GetModulePaths(vstPath));

        return base.GetNodesForPath(path);
    }

    public static ImmutableArray<ClassInfo> GetClassInfos(string path)
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

    ImmutableArray<Node> GetNodes(IEnumerable<string> modules)
    {
        var nodes = ImmutableArray.CreateBuilder<Node>();

        foreach (var p in modules)
        {
            foreach (var info in GetClassInfos(p))
            {
                if (info.Category != ClassInfo.VstAudioEffectClass)
                    continue;

                var node = GetNode(p, info);
                nodes.Add(node);
            }
        }

        return nodes.ToImmutable();
    }

    Node GetNode(string modulePath, ClassInfo info)
    {
        const string mainCategory = "Audio.VST";
        const string effectCategoryName = "Effect";
        const string instrumentCategoryName = "Instrument";

        var subCategory = "Other";
        var tags = info.SubCategories;
        if (info.SubCategories.Contains("Fx", StringComparer.OrdinalIgnoreCase))
        {
            subCategory = effectCategoryName;
            tags = tags.Remove("Fx", StringComparer.OrdinalIgnoreCase);
        }
        else if (info.SubCategories.Contains("Instrument", StringComparer.OrdinalIgnoreCase))
        {
            subCategory = instrumentCategoryName;
            tags = tags.Remove("Instrument", StringComparer.OrdinalIgnoreCase);
        }

        if (!string.IsNullOrEmpty(info.Vendor) && !tags.Contains(info.Vendor))
            tags = tags.Add(info.Vendor);

        // TODO: This information gets persisted in generated code - the module path might not be correct on target machine!!
        var effectInfo = new EffectNodeInfo(info.Name, $"{mainCategory}.{subCategory}", modulePath, info.ID);
        return new Node(new()
        {
            Name = effectInfo.Name,
            Category = effectInfo.Category,
            Tags = string.Join(',', tags),
            Summary = $"By {info.Vendor}, Version {info.Version}, Sdk {info.SdkVersion}",
            FragmentSelection = FragmentSelection.Explicit,
            HasStateOutput = true,
            StateOutputNotVisibleByDefault = true,
        }, effectInfo.ToString());
    }
}
