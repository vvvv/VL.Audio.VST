using Microsoft.Extensions.Logging;
using System.Drawing;
using System.Windows.Forms;
using VST3.Hosting;
using VST3;

namespace VL.Audio.VST;
partial class EffectHost
{
    private Form? window;

    private void ShowEditor()
    {
        if (controller is null)
            return;

        if (window is null || window.IsDisposed)
        {
            var view = controller.createView("editor");
            if (view is null || view.isPlatformTypeSupported("HWND") != 0)
                return;

            window = new Form();

            // High DPI support
            var scaleSupport = view as IPlugViewContentScaleSupport;
            window.DpiChanged += (s, e) =>
            {
                scaleSupport?.setContentScaleFactor(e.DeviceDpiNew / 96f);
            };

            try
            {
                var plugViewSize = view.getSize();
                window.ClientSize = new Size(plugViewSize.Width, plugViewSize.Height);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to get initial view size");
            }

            window.HandleCreated += (s, e) =>
            {
                var plugFrame = new PlugFrame((r) => window.ClientSize = new Size(r.Width, r.Height));
                view.setFrame(plugFrame);
                view.attached(window.Handle, "HWND");
                scaleSupport?.setContentScaleFactor(window.DeviceDpi / 96f);
            };
            window.HandleDestroyed += (s, e) =>
            {
                view.removed();
                view.ReleaseComObject();
            };
            window.ClientSizeChanged += (s, e) =>
            {
                var r = window.ClientRectangle;
                view.onSize(new ViewRect() { left = r.Left, right = r.Right, top = r.Top, bottom = r.Bottom });
            };
        }

        window.Visible = true;
    }

    private void HideEditor()
    {
        window?.Close();
        window?.Dispose();
        window = null;
    }
}
