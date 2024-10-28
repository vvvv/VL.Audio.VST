using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace VST3;

internal sealed class VstWrappers : StrategyBasedComWrappers
{
    public static readonly VstWrappers Instance = new VstWrappers();

    private readonly ThreadLocal<FUnknownStrategy> unknownStrategy = new(() => new FUnknownStrategy(), trackAllValues: true);

    public T CreateObjectForComInstance<T>(nint externalComObject) where T : class
    {
        try
        {
            return (T)Instance.GetOrCreateObjectForComInstance(externalComObject, CreateObjectFlags.UniqueInstance);
        }
        finally
        {
            Marshal.Release(externalComObject);
        }
    }

    public void ReleaseRemainingComObjects()
    {
        var currentThread = Thread.CurrentThread;
        foreach (var strategy in unknownStrategy.Values)
        {
            if (strategy.Thread == currentThread)
                strategy.ReleaseRemainingComObjects();
        }
    }

    protected override IIUnknownStrategy GetOrCreateIUnknownStrategy()
    {
        var synchronizationContext = SynchronizationContext.Current;
        if (synchronizationContext is null)
            return base.GetOrCreateIUnknownStrategy();

        return unknownStrategy.Value!;
    }

    unsafe sealed class FUnknownStrategy : IIUnknownStrategy
    {
        private readonly ConcurrentBag<nint> toBeReleased = new();
        private readonly Thread thread;
        private readonly SynchronizationContext? synchronizationContext;

        public FUnknownStrategy()
        {
            thread = Thread.CurrentThread;
            synchronizationContext = SynchronizationContext.Current;
        }

        public Thread Thread => thread;

        public void ReleaseRemainingComObjects()
        {
            Debug.Assert(Thread.CurrentThread == thread);

            foreach (var ptr in toBeReleased)
            {
                Marshal.Release(ptr);
            }

            toBeReleased.Clear();
        }

        void* IIUnknownStrategy.CreateInstancePointer(void* unknown)
        {
            Marshal.AddRef((nint)unknown);
            return unknown;
        }

        int IIUnknownStrategy.QueryInterface(void* thisPtr, in Guid handle, out void* ppObj)
        {
            int hr = Marshal.QueryInterface((nint)thisPtr, in handle, out nint ppv);
            if (hr < 0)
            {
                ppObj = null;
            }
            else
            {
                ppObj = (void*)ppv;

                // Subsequent code assumes a HRESULT < 0 in case the cast fails
                // It seems not all implementations stick to that agreement leading to an access violation
                // We can prevent that by injecting a negative result should we observe null
                if (ppObj is null)
                {
                    hr = -1;
                }
            }
            return hr;
        }

        int IIUnknownStrategy.Release(void* thisPtr)
        {
            var instancePtr = (nint)thisPtr;

            if (SynchronizationContext.Current == synchronizationContext)
            {
                return Marshal.Release(instancePtr);
            }
            else
            {
                // We need to release the instance on the correct thread
                toBeReleased.Add(instancePtr);

                synchronizationContext?.Post(_ => ReleaseRemainingComObjects(), null);

                return 0;
            }
        }
    }
}
