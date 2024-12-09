using System.Runtime.InteropServices.Marshalling;
using VL.Audio.VST;
using VL.Audio.VST.Internal;
using VST3;
using VST3.Hosting;
using Utils = VL.Audio.VST.Utils;

namespace VSTHost
{
    [GeneratedComClass]
    public unsafe partial class Form1 : Form, IComponentHandler
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

                var context = new HostApp([]);
                // This crashes for JUCE based plugins :(
                //var plugProvider1 = Task.Run(() => PlugProvider.Create(pluginFactory, info, context)).Result;
                plugProvider = PlugProvider.Create(pluginFactory, info, context);

                var component = plugProvider.Component;
                var processor = (IAudioProcessor)component;
                var controller = plugProvider.Controller;

                var outputSignal = new VL.Audio.BufferCallerSignal()
                {
                };

                var processContext = new ProcessContext()
                {

                };


                ProcessSetup processSetup;
                processor.setupProcessing(
                    processSetup = new ProcessSetup()
                    {
                        ProcessMode = ProcessModes.Realtime,
                        SymbolicSampleSize = Utils.GetSymbolicSampleSizes(outputSignal.WaveFormat),
                        MaxSamplesPerBlock = Math.Max(VL.Audio.AudioService.Engine.Settings.BufferSize, 4096),
                        SampleRate = VL.Audio.AudioService.Engine.Settings.SampleRate
                    });


                // Activate main buses
                component.activateBus(MediaTypes.kAudio, BusDirections.kOutput, 0, true);
                component.activateBus(MediaTypes.kEvent, BusDirections.kInput, 0, true);

                component.setActive(true);
                processor.setProcessing(true);

               
                if (controller != null)
                {
                    controller.setComponentHandler(this);

                    foreach (var p in controller.GetParameters())
                    {
                    }
                }

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
            try
            {
                view.getSize();
            }
            catch (Exception)
            {

            }
            var plugFrame = new PlugFrame((r) => ClientSize = new Size(r.Width, r.Height));
            view.setFrame(plugFrame);
            view.attached(Handle, "HWND");
        }

        void IComponentHandler.beginEdit(uint id)
        {
            
        }

        void IComponentHandler.performEdit(uint id, double valueNormalized)
        {
            
        }

        void IComponentHandler.endEdit(uint id)
        {
            
        }

        void IComponentHandler.restartComponent(RestartFlags flags)
        {
            
        }
    }
}
