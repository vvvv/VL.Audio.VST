using Stride.Core.Mathematics;
using VL.Core;
using System.Windows.Forms;
using VL.Core.Import;

namespace VL.Audio.VST;

[Smell(SymbolSmell.Advanced)]
public record WindowState(WindowVisibility Visibility, RectangleF Bounds)
{
    public static readonly WindowState Default = new WindowState(default, default);

    public bool IsVisible => Visibility != WindowVisibility.Hidden;
}

[Smell(SymbolSmell.Advanced)]
public enum WindowVisibility
{
    Hidden,
    Minimized,
    Normal,
    Maximized
}

internal static class WindowsFormsExtensions
{
    public static FormWindowState ToFormWindowState(this WindowVisibility visibility)
    {
        return visibility switch
        {
            WindowVisibility.Normal => FormWindowState.Normal,
            WindowVisibility.Minimized => FormWindowState.Minimized,
            WindowVisibility.Maximized => FormWindowState.Maximized,
            _ => FormWindowState.Normal
        };
    }

    public static WindowVisibility ToWindowVisibility(this FormWindowState state)
    {
        return state switch
        {
            FormWindowState.Normal => WindowVisibility.Normal,
            FormWindowState.Minimized => WindowVisibility.Minimized,
            FormWindowState.Maximized => WindowVisibility.Maximized,
            _ => WindowVisibility.Hidden
        };
    }
}