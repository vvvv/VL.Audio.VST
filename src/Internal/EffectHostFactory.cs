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

    public static string? ResolveModulePath(string moduleName, IEnumerable<string> searchPaths)
    {
        var paths = searchPaths
            .SelectMany(p => Module.GetModulePaths(Path.Combine(p, "vst3")))
            .Concat(Module.GetModulePaths());
        foreach (var m in paths)
            if (string.Equals(Path.GetFileName(m), moduleName, StringComparison.OrdinalIgnoreCase))
                return m;
        return null;
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

                var moduleName = Path.GetFileName(p);
                var node = GetNode(moduleName, info);
                nodes.Add(node);
            }
        }

        return nodes.ToImmutable();
    }

    Node GetNode(string moduleName, ClassInfo info)
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

        var effectInfo = new EffectNodeInfo(info.Name, $"{mainCategory}.{subCategory}", moduleName, info.ID);
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
