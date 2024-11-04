using System.Runtime.InteropServices.Marshalling;

namespace VST3.Hosting;

[GeneratedComClass]
sealed partial class ParameterChanges : VstObject<IParameterChanges>, IParameterChanges
{
    private readonly List<ParameterValueQueue> queues = new();
    private int usedQueueCount;

    public ParameterValueQueue AddParameterData(in uint id, out int index)
    {
        for (var i = 0; i < usedQueueCount; i++)
        {
            var queue = queues[i];
            if (queue.ParamId == id)
            {
                index = i;
                return queue;
            }
        }

        ParameterValueQueue valueQueue;
        if (usedQueueCount < queues.Count)
        {
            valueQueue = queues[usedQueueCount];
            valueQueue.ParamId = id;
            valueQueue.Clear();
        }
        else
        {
            valueQueue = new ParameterValueQueue(id);
            queues.Add(valueQueue);
        }

        index = usedQueueCount;
        usedQueueCount++;
        return valueQueue;
    }

    public int GetParameterCount() => usedQueueCount;

    public ParameterValueQueue? GetParameterData(int index)
    {
        if (index >= 0 && index < usedQueueCount)
            return queues[index];
        return null;
    }

    public void Clear()
    {
        usedQueueCount = 0;
    }

    public IEnumerable<(uint, double)> GetLatestValues()
    {
        foreach (var queue in queues)
            yield return (queue.ParamId, queue.value);
    }

    IParamValueQueue IParameterChanges.addParameterData(in uint id, out int index) => AddParameterData(id, out index);
    int IParameterChanges.getParameterCount() => GetParameterCount();
    IParamValueQueue? IParameterChanges.getParameterData(int index) => GetParameterData(index);
}
