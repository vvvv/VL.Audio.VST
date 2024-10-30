# VL.Audio.VST
Use VST3 instruments and effects and control them via input parameters or MIDI. 

<img src=".github/images/vst.png" width="15%" alt="VST Compatiblen" />

VST® is a trademark of [Steinberg Media Technologies GmbH](https://www.steinberg.net/), registered in Europe and other countries.

For use with vvvv, the visual live-programming environment for .NET: http://visualprogramming.net

<details> <summary>Implementation Status</summary>

Compare [VST3 Technical Documentation](https://steinbergmedia.github.io/vst3_dev_portal/pages/Technical+Documentation/Index.html)

| Implemented Interfaces | Status  |
| ------------- | ------------- |
| IAttributeList | done |
| IComponentHandler | parameter editing yes, restart calls ignored |
| IEventList | done |
| IUnitHandler | done |
| IHostApplication | done |
| IMessage | done |
| IParamValueQueue | done |
| IParameterChanges | done |
| IPlugFrame | done |

| Consumed Interfaces (from the plugin) | Status  |
| ------------- | ------------- |
| IComponent | state yes, bus count and info only main, IO mode no |
| IAudioProcessor | assuming stereo only |
| IEditController | done |
| IConnectionPoint | yes, using a connection proxy which ensures any notifications are done on main thread |
| IUnitInfo | only to read hierachy when creating channels for parameters |
| IProgramListData | no |
| IUnitData | no |
| IPlugView | sizing yes, keyboard handling no |

| Additional | Status  |
| ------------- | ------------- |
| Multiple Dynamic I/O Support | no |
| Silence flags | no |
| Parameter MIDI Mapping | yes |
| Parameter Finder | no |
| Audio Presentation Latency | no |
| Dirty State, Open Editor Request and UI Group Editing Support | no |
| KnobMode, Open Help & Open Aboutbox | no |
| Note Expression | no |
| Key Switch | no |
| Remote Presentation of Parameters | no |
| Context Menu | no |
| Enhanced Linked Parameters | no |
| iOS Inter-App Audio | no |
| Preset Meta-Information | no |
| Channel Context Info | no |
| Unit-Bus Assignment Change | no |
| Prefetchable | no |
| Automation State | no |
| PlugView Content Scaling | yes |
| Request Bus Activation | no |
| UI Snapshots | no |
| NoteExpression Physical UI Mapping | no |
| Legacy MIDI CC Out Event | no |
| MIDI Learn | yes |
| Host Query Interface support | no |
| MPE support for Wrappers | no |
| Parameter Function Name | no |
| Progress display | no |
| Process Context Requirements | no |
| Control Voltage Bus Flag | no |
| Module Info and Plug-in Compatibility | no |
| Get Current SystemTime | no |
| Process  Requirements | no |
| Data Transfert Between Processor/Controller | no |
| Remap Parameter ID | no |
</details> 

## Getting started
- Install as [described here](https://thegraybook.vvvv.org/reference/hde/managing-nugets.html) via commandline:

    `nuget install VL.Audio.VST -pre -source https://nuget.pkg.github.com/vvvv/index.json`

- Usage examples and more information are included in the pack and can be found via the [Help Browser](https://thegraybook.vvvv.org/reference/hde/findinghelp.html)

## Contributing
- Report issues on [the vvvv forum](https://discourse.vvvv.org/c/vvvv-gamma/28)
- For custom development requests, please [get in touch](mailto:devvvvs@vvvv.org)
- When making a pull-request, please make sure to read the general [guidelines on contributing to vvvv libraries](https://thegraybook.vvvv.org/reference/extending/contributing.html)
