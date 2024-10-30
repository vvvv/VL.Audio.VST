using System.Runtime.CompilerServices;
using VL.Audio.VST;
using VST3;
using VST3.Hosting;

namespace VSTHost
{
    public partial class Form1 : Form
    {
        IPlugView? view;
        private PlugProvider? plugProvider;
        private Module? module;

        public Form1()
        {
            InitializeComponent();
        }

        public string PluginPath { get; set; }

        protected override void OnLoad(EventArgs e)
        {

            base.OnLoad(e);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            //foreach (var p in Module.GetModulePaths())
            //{
            //    var m = Module.Create(p);
            //}

            //var module = Module.Create("C:\\Program Files\\Common Files\\VST3\\Vintage.vst3");
            if (!Module.TryCreate(PluginPath, out module))
                return;

            var pluginFactory = module.Factory;
            foreach (var info in pluginFactory.ClassInfos)
            {
                if (info.Category != ClassInfo.VstAudioEffectClass)
                    continue;

                var context = new HostApp();
                // This crashes for JUCE based plugins :(
                //var plugProvider1 = Task.Run(() => PlugProvider.Create(pluginFactory, info, context)).Result;
                plugProvider = PlugProvider.Create(pluginFactory, info, context);
                CreateView();
                break;
            }
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            DestroyPlugin();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            VstWrappers.Instance.ReleaseRemainingComObjects();

            module?.Dispose();
            module = null;

            base.OnHandleDestroyed(e);
        }

        private void DestroyPlugin()
        {
            view?.removed();
            view?.ReleaseComObject();
            view = null;

            plugProvider?.Dispose();
            plugProvider = null;
        }

        protected override void OnPreviewKeyDown(PreviewKeyDownEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                if (view != null)
                {
                    view.removed();
                    view = null;
                }
                else
                {
                    CreateView();
                }
                GC.Collect();
            }

            base.OnPreviewKeyDown(e);
        }

        private void CreateView()
        {
            view = plugProvider.Controller.createView("editor");
            var plugFrame = new PlugFrame((r) => ClientSize = new Size(r.Width, r.Height));
            view.setFrame(plugFrame);
            view.attached(Handle, "HWND");
        }
    }
}
