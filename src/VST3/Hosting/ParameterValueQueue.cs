using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace VST3.Hosting;

[GeneratedComClass]
sealed partial class ParameterValueQueue : IParamValueQueue
{
    public double value;

    private readonly List<ParameterQueueValue> values = new();

    public ParameterValueQueue(uint paramId)
    {
        ParamId = paramId;
    }

    public uint ParamId { get; set; }

    public void Clear() => values.Clear();

    public int addPoint(int sampleOffset, double value)
    {
        var values = CollectionsMarshal.AsSpan(this.values);

        var destIndex = values.Length;
        for (var i = 0; i < values.Length; i++)
        {
            ref var v = ref values[i];
            if (v.sampleOffset == sampleOffset)
            {
                v.value = value;
                return i;
            }
            if (v.sampleOffset > sampleOffset)
            {
                destIndex = i;
                break;
            }
        }

        var queueValue = new ParameterQueueValue() { value = value, sampleOffset = sampleOffset };
        if (destIndex == values.Length)
            this.values.Add(queueValue);
        else
            this.values.Insert(destIndex, queueValue);

        this.value = value;

        return destIndex;
    }

    public uint getParameterId() => ParamId;

    public void getPoint(int index, out int sampleOffset, out double value)
    {
        var values = CollectionsMarshal.AsSpan(this.values);
        ref var queueValue = ref values[index];
        sampleOffset = queueValue.sampleOffset;
        value = queueValue.value;
    }

    public int getPointCount()
    {
        return values.Count;
    }

    private struct ParameterQueueValue
    {
        public double value;
        public int sampleOffset;
    }
}
