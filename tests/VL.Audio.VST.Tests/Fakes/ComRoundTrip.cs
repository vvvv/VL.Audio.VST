using System.Runtime.InteropServices;
using VST3;
using VST3.Hosting;

namespace VL.Audio.VST.Tests.Fakes;

/// <summary>
/// Hands managed fakes to the host as native COM pointers, so the host talks to them through the same vtables as to
/// a real plugin.
/// </summary>
internal static class ComRoundTrip
{
    /// <summary>
    /// The factory of the fake as the host sees one from GetPluginFactory: a wrapper of a native pointer.
    /// </summary>
    public static PluginFactory CreateFactory(FakePlugin plugin)
    {
        var ptr = VstWrappers.Instance.GetOrCreateComInterfaceForObject(plugin, CreateComInterfaceFlags.None);
        return new PluginFactory(VstWrappers.Instance.CreateObjectForComInstance<IPluginFactory>(ptr));
    }

    /// <summary>
    /// A wrapper of a pointer the host passed (in ProcessData for example); release it with ReleaseComObject.
    /// </summary>
    public static T Wrap<T>(nint ptr) where T : class
    {
        return (T)VstWrappers.Instance.GetOrCreateObjectForComInstance(ptr, CreateObjectFlags.UniqueInstance);
    }

    public static unsafe byte[] ReadAll(IBStream stream)
    {
        var result = new MemoryStream();
        var buffer = new byte[256];
        fixed (byte* p = buffer)
        {
            while (true)
            {
                int read = 0;
                stream.read((nint)p, buffer.Length, &read);
                if (read <= 0)
                    break;
                result.Write(buffer, 0, read);
            }
        }
        return result.ToArray();
    }

    public static unsafe void WriteAll(IBStream stream, byte[] bytes)
    {
        fixed (byte* p = bytes)
        {
            int written = 0;
            stream.write((nint)p, bytes.Length, &written);
        }
    }
}
