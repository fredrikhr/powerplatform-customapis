namespace FredrikHr.PowerPlatformRuntimeExtensions.CommonPlugins;

public sealed class UUIDv8CompoundHashOutputPlugin : IPlugin
{
    private static readonly Entity DefaultLayoutParameters = new()
    {
        Attributes =
        {
            { UUIDv8CompoundLayout.InputParameterNames.Value0, 0L },
            { UUIDv8CompoundLayout.InputParameterNames.Value1, 0L },
            { UUIDv8CompoundLayout.InputParameterNames.Value2, 0L },
        }
    };

    private readonly UUIDv8CompoundCreatePlugin _encoder = new();

    public void Execute(IServiceProvider serviceProvider)
    {
        var context = serviceProvider.Get<IPluginExecutionContext>();
        ParameterCollection inputs = context.InputParameters;

        inputs[UUIDv8CompoundCreatePlugin.InputParameterNames.LayoutParameters] =
            DefaultLayoutParameters;
        _encoder.Execute(serviceProvider);
    }
}