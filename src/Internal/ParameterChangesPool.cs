using Microsoft.Extensions.ObjectPool;
using VL.Lib.Reactive;
using VST3.Hosting;

namespace VL.Audio.VST.Internal;

sealed class ParameterChangesPool : DefaultObjectPool<ParameterChanges>
{
    public static readonly ParameterChangesPool Default = new();

    public ParameterChangesPool() : base(new Policy())
    {
    }

    sealed class Policy : PooledObjectPolicy<ParameterChanges>
    {
        public override ParameterChanges Create() => new ParameterChanges();

        public override bool Return(ParameterChanges obj)
        {
            obj.Clear();
            return true;
        }
    }
}
