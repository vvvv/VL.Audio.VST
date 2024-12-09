using VL.Core;
using VST3.Hosting;

namespace VL.Audio.VST.Internal
{
    internal record class EffectNodeInfo(IVLNodeDescription NodeDescription, string ModulePath, ClassInfo ClassInfo);
}
