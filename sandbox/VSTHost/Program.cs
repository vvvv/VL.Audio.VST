using VL.Audio;
using VST3.Hosting;
using VST3;
using Timer = System.Threading.Timer;

namespace VSTHost
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();

            Task.Run(() =>
            {
                if (!Module.TryCreate(@"C:\Program Files\Common Files\VST3\Kontakt 8.vst3", out var module))
                    return;

                var pluginFactory = module.Factory;
                foreach (var info in pluginFactory.ClassInfos)
                {
                    if (info.Category != ClassInfo.VstAudioEffectClass)
                        continue;
                }

                module.Dispose();
            }).Wait();

            //var otherThread = new Thread(() =>
            //{
            //    SynchronizationContext s = default;
            //    var timer = new Timer(_ =>
            //    {
            //        if (s != null)
            //            s.Send(_ =>
            //            {
            //                Application.DoEvents();
            //            }, null);
            //    }, null, TimeSpan.FromMilliseconds(100), period: TimeSpan.FromMilliseconds(60));

            //    var form = new Form1() { PluginPath = @"C:\Program Files\Common Files\VST3\Kontakt 8.vst3" };
            //    form.HandleCreated += (_, _) =>
            //    {
            //        s = SynchronizationContext.Current;
            //    };
            //    Application.Run(form);
            //});
            //otherThread.SetApartmentState(ApartmentState.STA);
            //otherThread.Start();

            Application.Run(new Form1() { PluginPath = @"C:\Program Files\Common Files\VST3\Kontakt 8.vst3" });
        }
    }
}