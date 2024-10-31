using Microsoft.Extensions.Logging;
using System.Drawing;
using System.Windows.Forms;
using VST3.Hosting;
using VST3;
using System.Runtime.InteropServices.Marshalling;

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

            window = new Window(view);

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

            try
            {
                if (!view.canResize())
                {
                    window.FormBorderStyle = FormBorderStyle.FixedDialog;
                    window.MaximizeBox = false;
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to check if view can resize");
            }
        }

        window.Visible = true;
    }

    private void HideEditor()
    {
        window?.Close();
        window?.Dispose();
        window = null;
    }

    [GeneratedComClass]
    sealed partial class Window : Form, IPlugFrame
    {
        private readonly IPlugView view;
        private readonly IPlugViewContentScaleSupport? scaleSupport;

        public Window(IPlugView view)
        {
            this.view = view;
            this.scaleSupport = view as IPlugViewContentScaleSupport;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            view.setFrame(this);
            view.attached(Handle, "HWND");
            scaleSupport?.setContentScaleFactor(DeviceDpi / 96f);

            base.OnHandleCreated(e);
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            view.removed();
            view.ReleaseComObject();

            base.OnHandleDestroyed(e);
        }

        protected override void OnDpiChanged(DpiChangedEventArgs e)
        {
            scaleSupport?.setContentScaleFactor(e.DeviceDpiNew / 96f);

            base.OnDpiChanged(e);
        }

        protected override void OnClientSizeChanged(EventArgs e)
        {
            var r = ClientRectangle;
            view.onSize(new ViewRect() { Left = r.Left, Right = r.Right, Top = r.Top, Bottom = r.Bottom });

            base.OnClientSizeChanged(e);
        }

        protected override void OnResizeEnd(EventArgs e)
        {
            // TODO: Doing this inside of Resize doesn't work properly when dragging either horizontally or vertically
            var r = ClientRectangle;
            var rectangle = new ViewRect() { Left = r.Left, Top = r.Top, Right = r.Right, Bottom = r.Bottom };
            if (view.checkSizeConstraint(ref rectangle))
            {
                ClientSize = new Size(rectangle.Width, rectangle.Height);
            }

            base.OnResizeEnd(e);
        }

        void IPlugFrame.resizeView(nint view, in ViewRect newSize)
        {
            ClientSize = new Size(newSize.Width, newSize.Height);
        }
    }
}