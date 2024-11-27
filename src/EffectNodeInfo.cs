using VL.Core;
using VST3.Hosting;

namespace VL.Audio.VST
{
    internal record class EffectNodeInfo(IVLNodeDescription NodeDescription, string ModulePath, ClassInfo ClassInfo);
}
