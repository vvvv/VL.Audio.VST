using System.Runtime.InteropServices.Marshalling;

namespace VST3.Hosting;

[GeneratedComClass]
sealed partial class PlugFrame : IPlugFrame
{
    private readonly Action<IPlugView, ViewRect> resizeView;

    public PlugFrame(Action<IPlugView, ViewRect> resizeView)
    {
        this.resizeView = resizeView;
    }

    void IPlugFrame.resizeView(IPlugView view, in ViewRect newSize) => resizeView(view, newSize);
}
