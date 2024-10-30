using System.Runtime.InteropServices.Marshalling;

namespace VST3.Hosting;

[GeneratedComClass]
sealed partial class PlugFrame : IPlugFrame
{
    private readonly Action<ViewRect> resizeView;

    public PlugFrame(Action<ViewRect> resizeView)
    {
        this.resizeView = resizeView;
    }

    void IPlugFrame.resizeView(nint view, in ViewRect newSize) => resizeView(newSize);
}
