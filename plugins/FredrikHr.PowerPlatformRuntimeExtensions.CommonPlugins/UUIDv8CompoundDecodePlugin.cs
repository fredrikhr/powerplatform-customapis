namespace FredrikHr.PowerPlatformRuntimeExtensions.CommonPlugins;

public sealed class UUIDv8CompoundDecodePlugin : IPlugin
{
    internal static class InputParameterNames
    {
        internal const string Value = nameof(Value);
        internal const string LayoutType = nameof(LayoutType);
    }

    internal static class OutputParameterNames
    {
        internal const string LayoutParameters = nameof(LayoutParameters);
    }

    public void Execute(IServiceProvider serviceProvider)
    {
        var context = serviceProvider.Get<IPluginExecutionContext>();
        ParameterCollection inputs = context.InputParameters;
        ParameterCollection outputs = context.OutputParameters;

        var guid = context.InputParameterOrDefault<Guid>(
            InputParameterNames.Value
            );

        Action<Guid, DataCollection<string, object>> decoder = inputs
            .TryGetValue(InputParameterNames.LayoutType, out string layoutType)
            ? UUIDv8CompoundLayout.Adapters.TryGetValue(layoutType, out var adapter)
                ? adapter.decode
                : throw new InvalidPluginExecutionException(
                    httpStatus: PluginHttpStatusCode.BadRequest,
                    message: $"Unrecognized layout type '{layoutType}'."
                    )
            : throw new InvalidPluginExecutionException(
                httpStatus: PluginHttpStatusCode.BadRequest,
                message: $"Required input parameter '{InputParameterNames.LayoutType}' is not specified."
                );

        Entity layoutParameters = new();

        try
        {
            decoder(guid, layoutParameters.Attributes);
        }
        catch (Exception encodeExcept)
        {
            serviceProvider.Get<ITracingService>().Trace("While decoding layout of type '{0}': {1}", layoutType, encodeExcept);
            throw new InvalidPluginExecutionException(
                httpStatus: PluginHttpStatusCode.InternalServerError,
                message: $"Failed to decode UUID with layout type '{layoutType}'. {encodeExcept.Message}"
                );
        }

        if (!layoutParameters.TryGetAttributeValue(
            UUIDv8CompoundLayout.OutputParameterNames.UuidIsRfc9562Variant,
            out bool isRfc9562
            ) || !isRfc9562)
        {
            if (!layoutParameters.TryGetAttributeValue(
                UUIDv8CompoundLayout.OutputParameterNames.UuidVariant,
                out string uuidVariant
                ) || string.IsNullOrEmpty(uuidVariant))
                uuidVariant = "?";

            throw new InvalidPluginExecutionException(
                httpStatus: PluginHttpStatusCode.BadRequest,
                message: $"Invalid UUID. The specified UUID is not an RFC9562 compliant variant. Variant specified: {uuidVariant}"
                );
        }

        if (!layoutParameters.TryGetAttributeValue(
            UUIDv8CompoundLayout.OutputParameterNames.UuidVersion,
            out int uuidVersion
            ) || uuidVersion != 8)
        {
            throw new InvalidPluginExecutionException(
                httpStatus: PluginHttpStatusCode.BadRequest,
                message: $"Invalid UUID. Expected a UUID version 8 value. Version specified: {uuidVersion}"
                );
        }

        outputs[OutputParameterNames.LayoutParameters] = layoutParameters;
    }
}