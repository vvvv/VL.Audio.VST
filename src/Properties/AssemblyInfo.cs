using System.Runtime.CompilerServices;
using VL.Core.Import;

[assembly: ImportAsIs(Namespace = "VL.Audio.VST", Category = "Audio.VST")]
[assembly: DisableRuntimeMarshalling]
[assembly: InternalsVisibleTo("VSTHost")]