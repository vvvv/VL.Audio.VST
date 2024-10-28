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
    public int left;
    public int top;
    public int right;
    public int bottom;

    public int Width => right - left;
    public int Height => bottom - top;
}
