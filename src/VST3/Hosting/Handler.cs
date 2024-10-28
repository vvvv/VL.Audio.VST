using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Threading.Tasks;

namespace VST3.Hosting;

[GeneratedComClass]
sealed partial class Handler : IComponentHandler
{
    private readonly ParameterChanges inputParameterChanges;

    public Handler(ParameterChanges inputParameterChanges)
    {
        this.inputParameterChanges = inputParameterChanges;
    }

    public void beginEdit(uint id)
    {
    }

    public void endEdit(uint id)
    {
    }

    public void performEdit(uint id, double valueNormalized)
    {
        var queue = inputParameterChanges.AddParameterData(id, out _);
        queue.addPoint(0, valueNormalized);
    }

    public void restartComponent(RestartFlags flags)
    {
    }
}