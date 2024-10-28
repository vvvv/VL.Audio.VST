using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace VST3.Hosting;

class Module : IDisposable
{
    public static List<string> GetModulePaths()
    {
        // https://steinbergmedia.github.io/vst3_dev_portal/pages/Technical+Documentation/Locations+Format/Plugin+Locations.html
        var paths = new List<string>();

        if (OperatingSystem.IsWindows())
        {
            FindModules(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Common", "VST3"));
            FindModules(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles), "VST3"));
            FindModules(Path.Combine(AppContext.BaseDirectory, "VST3"));
        }

        return paths;

        void FindModules(string path)
        {
            if (Directory.Exists(path))
                paths.AddRange(GetModulePaths(path));
        }
    }

    public static IEnumerable<string> GetModulePaths(string directory)
    {
        return FindFilesWithExtension(new DirectoryInfo(directory), ".vst3");
    }

    private static IEnumerable<string> FindFilesWithExtension(DirectoryInfo path, string extension, bool recursive = true, bool moduleExecutable = false)
    {
        foreach (var p in path.GetFileSystemInfos())
        {
            FileSystemInfo finalPath;

            if (p.LinkTarget != null)
                finalPath = Directory.Exists(p.LinkTarget) ? new DirectoryInfo(p.LinkTarget) : new FileInfo(p.LinkTarget);
            else
                finalPath = p;

            if (!finalPath.Exists)
                continue;

            if (string.Equals(finalPath.Extension, extension, StringComparison.OrdinalIgnoreCase))
            {
                if (CheckVST3Package(finalPath.FullName, out var f))
                {
                    yield return moduleExecutable ? f : finalPath.FullName;
                    continue;
                }
            }

            if (recursive && finalPath is DirectoryInfo dir)
            {
                foreach (var f in FindFilesWithExtension(dir, extension, recursive))
                    yield return f;
            }
            else if (string.Equals(finalPath.Extension, extension, StringComparison.OrdinalIgnoreCase))
            {
                yield return finalPath.FullName;
            }
        }
    }

    private static bool CheckVST3Package(string path, [NotNullWhen(true)] out string? finalPath)
    {
        // https://steinbergmedia.github.io/vst3_dev_portal/pages/Technical+Documentation/Locations+Format/Plugin+Format.html
        if (OpenVST3Package(path, GetArchString(), out finalPath))
            return true;

        if (TestForArm64X() && OpenVST3Package(path, Arm64XWin, out finalPath))
            return true;

        return false;
    }

    private static bool OpenVST3Package(string path, string archString, [NotNullWhen(true)] out string? finalPath)
    {
        var p = Path.Combine(path, "Contents", archString, Path.GetFileName(path));
        try
        {
            using (File.Open(p, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                finalPath = p;
                return true;
            }
        }
        catch (Exception)
        {
            finalPath = null;
            return false;
        }
    }

    static string GetArchString()
    {
        if (OperatingSystem.IsWindows())
        {
            var cpu = RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X64 => "x86_64",
                Architecture x => x.ToString().ToLowerInvariant(),
            };
            return $"{cpu}-win";
        }

        throw new NotImplementedException();
    }

    static bool TestForArm64X() => OperatingSystem.IsWindowsVersionAtLeast(11) && RuntimeInformation.ProcessArchitecture == Architecture.Arm64;

    const string Arm64XWin = "arm64x-win";

    public static unsafe bool TryCreate(string path, [NotNullWhen(true)] out Module? module)
    {
        nint modulePtr;
        bool isBundle;

        module = null;

        if (Directory.Exists(path))
        {
            isBundle = true;
            modulePtr = LoadAsPackage(GetArchString());

            if (modulePtr == default && TestForArm64X())
                modulePtr = LoadAsPackage(Arm64XWin);
        }
        else
        {
            isBundle = false;
            modulePtr = LoadAsDll();
        }

        if (modulePtr == default)
            return false;

        var factoryProc = (delegate* unmanaged[Cdecl]<nint>)NativeLibrary.GetExport(modulePtr, "GetPluginFactory");
        if (factoryProc is null)
            return false;

        var dllEntry = (delegate* unmanaged[Cdecl]<bool>)NativeLibrary.GetExport(modulePtr, "InitDll");
        if (dllEntry is not null && !dllEntry())
            return false;

        var pluginFactoryPtr = factoryProc();
        var pluginFactory = VstWrappers.Instance.CreateObjectForComInstance<IPluginFactory>(pluginFactoryPtr);
        module = new Module(modulePtr, Path.GetFileName(path), path, isBundle, new PluginFactory(pluginFactory));
        return true;

        nint LoadAsDll()
        {
            return NativeLibrary.Load(path);
        }

        nint LoadAsPackage(string archString)
        {
            var p = Path.Combine(path, "Contents", archString, Path.GetFileName(path));
            return NativeLibrary.Load(p);
        }
    }

    private readonly nint module;

    private Module(nint module, string name, string filePath, bool isBundle, PluginFactory factory)
    {
        this.module = module;

        Name = name;
        FilePath = filePath;
        IsBundle = isBundle;
        Factory = factory;
    }

    public string Name { get; }
    public string FilePath { get; }
    public PluginFactory Factory { get; }
    public bool IsBundle { get; }

    public unsafe void Dispose()
    {
        Factory.Dispose();

        var exitDll = (delegate* unmanaged[Cdecl]<bool>)NativeLibrary.GetExport(module, "ExitDll");
        if (exitDll is not null)
            exitDll();

        NativeLibrary.Free(module);
    }
}