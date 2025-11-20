namespace FredrikHr.PowerPlatformRuntimeExtensions.CommonPlugins;

public sealed class UUIDv8CompoundCreatePlugin : IPlugin
{
    internal static class InputParameterNames
    {
        internal const string Name = nameof(Name);
        internal const string LayoutType = nameof(LayoutType);
        internal const string LayoutParameters = nameof(LayoutParameters);
    }

    internal static class OutputParameterNames
    {
        internal const string HashOutput = nameof(HashOutput);
        internal const string Value = nameof(Value);
    }

    public void Execute(IServiceProvider serviceProvider)
    {
        var context = serviceProvider.Get<IPluginExecutionContext>();
        ParameterCollection inputs = context.InputParameters;
        ParameterCollection outputs = context.OutputParameters;

        byte[] nameBytes = [];
        if (inputs.TryGetValue(InputParameterNames.Name, out string nameText))
        {
            nameText = nameText.Normalize(System.Text.NormalizationForm.FormC);
            nameBytes = System.Text.Encoding.UTF8.GetBytes(nameText);
        }

        Func<byte[], DataCollection<string, object>, Guid> encoder = inputs
            .TryGetValue(InputParameterNames.LayoutType, out string layoutType)
            ? UUIDv8CompoundLayout.Adapters.TryGetValue(layoutType, out var adapter)
                ? adapter.encode
                : throw new InvalidPluginExecutionException(
                    httpStatus: PluginHttpStatusCode.BadRequest,
                    message: $"Unrecognized layout type '{layoutType}'."
                    )
            : throw new InvalidPluginExecutionException(
                httpStatus: PluginHttpStatusCode.BadRequest,
                message: $"Required input parameter '{InputParameterNames.LayoutType}' is not specified."
                );

        var layoutParameters = inputs.GetValue<Entity?>(
            InputParameterNames.LayoutParameters
            ) ?? new();

        try
        {
            outputs[OutputParameterNames.Value] =
                encoder(nameBytes, layoutParameters.Attributes);
        }
        catch (KeyNotFoundException missingParameterExcept)
        {
            serviceProvider.Get<ITracingService>().Trace("While encoding layout of type '{0}': {1}", layoutType, missingParameterExcept);
            throw new InvalidPluginExecutionException(
                httpStatus: PluginHttpStatusCode.BadRequest,
                message: $"Failed to encode UUID with the specified layout parameters. {missingParameterExcept.Message}"
                );
        }
        catch (Exception encodeExcept)
        {
            serviceProvider.Get<ITracingService>().Trace("While encoding layout of type '{0}': {1}", layoutType, encodeExcept);
            throw new InvalidPluginExecutionException(
                httpStatus: PluginHttpStatusCode.InternalServerError,
                message: $"Failed to encode UUID with the specified layout parameters. {encodeExcept.Message}"
                );
        }

        if (layoutParameters.TryGetAttributeValue(
            UUIDv8CompoundLayout.OutputParameterNames.HashOutput,
            out string hashOutput))
            outputs[OutputParameterNames.HashOutput] = hashOutput;
    }
}
