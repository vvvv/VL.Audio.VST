﻿namespace VST3.Hosting;

internal class PlugProvider : IDisposable
{
    public static PlugProvider? Create(PluginFactory factory, Guid id, IHostApplication context)
    {
        // TODO: 2nd call on user thread never returns for plugdata
        var component = factory.CreateInstance<IComponent>(id);
        if (component is null)
            return null;

        component.initialize(context);

        var controller = component as IEditController;
        if (controller is null)
        {
            Guid cid = default;
            component.getControllerClassId(ref cid);
            controller = factory.CreateInstance<IEditController>(cid);
            controller?.initialize(context);
        }

        var provider = new PlugProvider(factory, id, component, controller);
        if (component != controller)
            provider.ConnectComponents();
        return provider;
    }

    private readonly PluginFactory factory;
    private readonly Guid id;
    private ConnectionProxy? componentCP;
    private ConnectionProxy? controllerCP;
    private IComponent component;
    private IEditController? controller;

    private PlugProvider(PluginFactory factory, Guid id, IComponent component, IEditController? controller)
    {
        this.factory = factory;
        this.id = id;
        this.component = component;
        this.controller = controller;
    }

    public IComponent Component => component;
    public IEditController? Controller => controller;
    public Guid Id => id;

    public void Dispose()
    {
        DisconnectComponents();

        if (component != controller)
        {
            component.terminate();
            controller?.terminate();

            controller?.ReleaseComObject();
            component.ReleaseComObject();
        }
        else
        {
            component.terminate();
            component.ReleaseComObject();
        }
    }

    private void ConnectComponents()
    {
        if (component is IConnectionPoint componentCP && controller is IConnectionPoint controllerCP)
        {
            this.componentCP = new ConnectionProxy(componentCP);
            this.controllerCP = new ConnectionProxy(controllerCP);
            this.componentCP.connect(controllerCP);
            this.controllerCP.connect(componentCP);
        }
    }

    private void DisconnectComponents()
    {
        if (componentCP is not null && controllerCP is not null)
        {
            componentCP.disconnect();
            controllerCP.disconnect();

            controllerCP = null;
            componentCP = null;
        }
    }
}
