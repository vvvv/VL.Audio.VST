using Microsoft.Extensions.Logging;
using System.Windows.Forms;
using VST3;
using System.Runtime.InteropServices.Marshalling;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using VL.Lang.PublicAPI;
using VST3.Hosting;
using System.Drawing;
using RectangleF = Stride.Core.Mathematics.RectangleF;
using VL.Core;

namespace VL.Audio.VST;

partial class EffectHost
{
    private Window? window;

    /// <summary>
    /// Shows the UI of the plugin (if available).
    /// </summary>
    /// <remarks>
    /// In case the window was already created via the context menu, it will get re-created to ensure that its bounds are no longer saved to the pin.
    /// </remarks>
    /// <param name="bounds">The bounds of the window in pixel. Width and height will only get applied if the plugin allows it.</param>
    public void ShowUI(Optional<RectangleF> bounds)
    {
        // Called from user code, don't track window state
        ShowUI(bounds.ToNullable(), formWindowState: FormWindowState.Normal, trackWindowState: false);
    }

    private void ShowUI(RectangleF? bounds, FormWindowState? formWindowState, bool trackWindowState)
    {
        if (controller is null)
            return;

        // Ensure tracking state matches the requested state
        if (window != null && window.IsTracking != trackWindowState)
        {
            HideUI();
        }

        if (window is null || window.IsDisposed)
        {
            var view = controller.createView("editor");
            if (view is null || view.isPlatformTypeSupported("HWND") != 0)
                return;

            window = new Window(this, view, trackWindowState);

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

        if (bounds.HasValue)
        {
            var b = bounds.Value;
            window.StartPosition = FormStartPosition.Manual;
            window.Location = new Point((int)b.X, (int)b.Y);
            if (window.View.canResize() && b.Width > 0 && b.Height > 0)
                window.Size = new Size((int)b.Width, (int)b.Height);
        }

        if (formWindowState.HasValue)
            window.WindowState = formWindowState.Value;

        window.Visible = true;

        if (formWindowState != FormWindowState.Minimized)
            window.Activate();
    }

    /// <summary>
    /// Hides the UI of the plugin.
    /// </summary>
    public void HideUI()
    {
        window?.Dispose();
        window = null;
    }

    void SaveWindowState(WindowState windowState)
    {
        // Save in a field to ensure that an upcoming call to ShowUI will use the previous bounds
        this.windowState = windowState;

        if (windowStateChannel is null)
            return;

        SaveToChannelOrPin(windowStateChannel, IDevSession.Current?.CurrentSolution, WindowStatePinName, windowState)?.Confirm(JustWriteToThePin);
    }

    [GeneratedComClass]
    sealed partial class Window : Form, IPlugFrame
    {
        private readonly EffectHost effectHost;
        private readonly IPlugView view;
        private readonly IPlugViewContentScaleSupport? scaleSupport;
        private readonly SingleAssignmentDisposable boundsSubscription = new();
        private readonly bool trackWindowState;

        public Window(EffectHost effectHost, IPlugView view, bool trackWindowState)
        {
            this.effectHost = effectHost;
            this.view = view;
            this.trackWindowState = trackWindowState;
            this.scaleSupport = view as IPlugViewContentScaleSupport;
        }

        public IPlugView View => view;

        public bool IsTracking => trackWindowState;

        public WindowState GetWindowState()
        {
            var r = WindowState == FormWindowState.Normal ? new Rectangle(Location, Size) : RestoreBounds;
            return new WindowState(WindowState.ToWindowVisibility(), new RectangleF(r.X, r.Y, r.Width, r.Height));
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            view.setFrame(this);
            view.attached(Handle, "HWND");
            scaleSupport?.setContentScaleFactor(DeviceDpi / 96f);

            if (trackWindowState)
            {
                var windowState = GetWindowState();
                effectHost.SaveWindowState(windowState);

                boundsSubscription.Disposable = Observable.Merge(
                    Observable.FromEventPattern(this, nameof(ClientSizeChanged)),
                    Observable.FromEventPattern(this, nameof(LocationChanged)))
                    .Throttle(TimeSpan.FromSeconds(0.25))
                    .ObserveOn(SynchronizationContext.Current!)
                    .Subscribe(_ => effectHost.SaveWindowState(GetWindowState()));
            }

            base.OnHandleCreated(e);
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            boundsSubscription.Dispose();
            view.removed();
            view.ReleaseComObject();

            base.OnHandleDestroyed(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            if (trackWindowState)
            {
                var windowState = GetWindowState();
                effectHost.SaveWindowState(windowState with { Visibility = WindowVisibility.Hidden });
            }

            base.OnClosed(e);
        }

        protected override void OnDpiChanged(DpiChangedEventArgs e)
        {
            scaleSupport?.setContentScaleFactor(e.DeviceDpiNew / 96f);

            base.OnDpiChanged(e);
        }

        protected override void OnClientSizeChanged(EventArgs e)
        {
            // Client size is empty when the window is minimized. Don't send that to the view, as it breaks some.
            var clientSize = ClientSize;
            if (!clientSize.IsEmpty)
                view.onSize(new ViewRect() { Left = 0, Right = clientSize.Width, Top = 0, Bottom = clientSize.Height });

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