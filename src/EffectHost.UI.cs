using Microsoft.Extensions.Logging;
using System.Drawing;
using System.Windows.Forms;
using VST3.Hosting;
using VST3;
using System.Runtime.InteropServices.Marshalling;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;

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

            window = new Window(this, view);

            // High DPI support
            var scaleSupport = view as IPlugViewContentScaleSupport;
            window.DpiChanged += (s, e) =>
            {
                scaleSupport?.setContentScaleFactor(e.DeviceDpiNew / 96f);
            };

            bounds = boundsPin.Value?.Value ?? default;
            if (!bounds.IsEmpty)
            {
                window.StartPosition = FormStartPosition.Manual;
                SetWindowBounds(bounds);
            }
            else
            {
                try
                {
                    var plugViewSize = view.getSize();
                    window.ClientSize = new Size(plugViewSize.Width, plugViewSize.Height);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Failed to get initial view size");
                }
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
        window.Activate();
    }

    void SaveCurrentWindowBounds()
    {
        if (window is null || window.IsDisposed)
            return;

        var position = window.DesktopLocation;
        var bounds = new Stride.Core.Mathematics.RectangleF(position.X, position.Y, window.ClientSize.Width, window.ClientSize.Height);
        SaveToChannelOrPin(boundsPin.Value, BoundsInputPinName, bounds);
    }

    private void SetWindowBounds(Stride.Core.Mathematics.RectangleF bounds)
    {
        if (window is null || window.IsDisposed)
            return;

        window.Location = new Point((int)bounds.X, (int)bounds.Y);
        window.ClientSize = new Size((int)bounds.Width, (int)bounds.Height);
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
        private readonly EffectHost effectHost;
        private readonly IPlugView view;
        private readonly IPlugViewContentScaleSupport? scaleSupport;
        private readonly SingleAssignmentDisposable boundsSubscription = new();

        public Window(EffectHost effectHost, IPlugView view)
        {
            this.effectHost = effectHost;
            this.view = view;
            this.scaleSupport = view as IPlugViewContentScaleSupport;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            view.setFrame(this);
            view.attached(Handle, "HWND");
            scaleSupport?.setContentScaleFactor(DeviceDpi / 96f);

            boundsSubscription.Disposable = Observable.Merge(
                Observable.FromEventPattern(this, nameof(ClientSizeChanged)),
                Observable.FromEventPattern(this, nameof(LocationChanged)))
                .Throttle(TimeSpan.FromSeconds(0.25))
                .ObserveOn(SynchronizationContext.Current!)
                .Subscribe(_ => effectHost.SaveCurrentWindowBounds());

            base.OnHandleCreated(e);
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            boundsSubscription.Dispose();
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