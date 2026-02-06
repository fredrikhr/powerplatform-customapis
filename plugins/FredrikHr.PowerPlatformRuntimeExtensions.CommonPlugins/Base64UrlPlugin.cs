using System.Buffers.Text;

namespace FredrikHr.PowerPlatformRuntimeExtensions.CommonPlugins;

public sealed class Base64UrlPlugin : IPlugin
{
    internal static class ParameterNames
    {
        internal const string Base64String = nameof(Base64String);
        internal const string Base64UrlString = nameof(Base64UrlString);
        internal const string FormattingOptions = nameof(FormattingOptions);
    }

    internal static Dictionary<string, Action<IServiceProvider>> MessageHandlers = new(StringComparer.OrdinalIgnoreCase)
    {
        { "pwrplatf_ConvertToBase64UrlString", ExecuteToBase64Url },
        { "pwrplatf_ConvertFromBase64UrlString", ExecuteFromBase64Url },
    };

    public void Execute(IServiceProvider serviceProvider)
    {
        var context = serviceProvider.Get<IPluginExecutionContext>();
        if (!MessageHandlers.TryGetValue(context.MessageName, out Action<IServiceProvider> messageHandler))
        {
            throw new InvalidPluginExecutionException(
                httpStatus: PluginHttpStatusCode.BadRequest,
                message: $"Invalid SDK message for plugin execution: {context.MessageName}"
            );
        }

        messageHandler(serviceProvider);
    }

    private static void ExecuteToBase64Url(IServiceProvider serviceProvider)
    {
        var context = serviceProvider.Get<IPluginExecutionContext>();
        ParameterCollection inputs = context.InputParameters;
        ParameterCollection outputs = context.OutputParameters;

        _ = inputs.TryGetValue(ParameterNames.Base64String, out string? base64String);
        byte[] dataBytes = base64String switch
        {
            null => [],
            var _ => Convert.FromBase64String(base64String),
        };

        string base64UrlString = Base64Url.EncodeToString(dataBytes);
        outputs[ParameterNames.Base64UrlString] = base64UrlString;
    }

    private static void ExecuteFromBase64Url(IServiceProvider serviceProvider)
    {
        var context = serviceProvider.Get<IPluginExecutionContext>();
        ParameterCollection inputs = context.InputParameters;
        ParameterCollection outputs = context.OutputParameters;

        _ = inputs.TryGetValue(ParameterNames.Base64UrlString, out string? base64UrlString);
        byte[] dataBytes = base64UrlString switch
        {
            null => [],
            var _ => Base64Url.DecodeFromChars(base64UrlString.AsSpan()),
        };

        string base64String =
            inputs.TryGetValue(ParameterNames.FormattingOptions, out string formattingOptionsString) &&
            Enum.TryParse(formattingOptionsString, ignoreCase: true, out Base64FormattingOptions formattingOptions)
            ? Convert.ToBase64String(dataBytes, formattingOptions)
            : Convert.ToBase64String(dataBytes);
        outputs[ParameterNames.Base64String] = base64String;
    }
}