namespace VST3;

//------------------------------------------------------------------------
/*! \defgroup pluginGUI Graphical User Interface
*/

//------------------------------------------------------------------------
/**  Graphical rectangle structure. Used with IPlugView.
\ingroup pluginGUI
*/
struct ViewRect
{
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;

    public int Width => Right - Left;
    public int Height => Bottom - Top;
}
