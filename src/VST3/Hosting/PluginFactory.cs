using System.Collections.Immutable;
using VL.Audio.VST.Internal;

namespace VST3.Hosting;

internal class PluginFactory : IDisposable
{
    private readonly IPluginFactory factory;

    private FactoryInfo? factoryInfo;
    private int? classCount;
    private ImmutableArray<ClassInfo> classInfos;

    public PluginFactory(IPluginFactory factory)
    {
        this.factory = factory;
    }

    public FactoryInfo FactoryInfo => factoryInfo ??= factory.getFactoryInfo();

    public int ClassCount => classCount ??= factory.countClasses();

    public ImmutableArray<ClassInfo> ClassInfos
    {
        get
        {
            return !classInfos.IsDefault ? classInfos : (classInfos = Compute());

            ImmutableArray<ClassInfo> Compute()
            {
                var result = ImmutableArray.CreateBuilder<ClassInfo>(ClassCount);

                var f3 = factory as IPluginFactory3;
                var f2 = factory as IPluginFactory2;
                for (int i = 0; i < ClassCount; i++)
                {
                    ClassInfo? classInfo = default;

                    if (f3 is not null && f3.getClassInfoUnicode(i, out var ci3) == 0)
                        classInfo = ci3;
                    else if (f2 is not null && f2.getClassInfo2(i, out var ci2) == 0)
                        classInfo = ci2;
                    else if (factory.getClassInfo(i, out var ci) == 0)
                        classInfo = ci;

                    if (classInfo is null)
                        continue;

                    if (classInfo.Vendor.Length == 0)
                        classInfo = classInfo with { Vendor = FactoryInfo.Vendor };

                    result.Add(classInfo);
                }

                return result.ToImmutable();
            }
        }
    }

    public T CreateInstance<T>(Guid cid) where T : class
    {
        var obj = factory.createInstance(cid, typeof(T).GUID);
        return VstWrappers.Instance.CreateObjectForComInstance<T>(obj);
    }

    public void Dispose()
    {
        factory.ReleaseComObject();
    }
}
