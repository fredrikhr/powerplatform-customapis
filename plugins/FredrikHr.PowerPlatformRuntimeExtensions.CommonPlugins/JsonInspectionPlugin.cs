using System.Text.Json;

namespace FredrikHr.PowerPlatformRuntimeExtensions.CommonPlugins;

public class JsonInspectionPlugin : IPlugin
{
    internal static class InputParameterNames
    {
        internal const string JsonString = nameof(JsonString);
        internal const string AllowTrailingCommas = nameof(JsonDocumentOptions.AllowTrailingCommas);
        internal const string CommentHandling = nameof(JsonDocumentOptions.CommentHandling);
        internal const string MaxDepth = nameof(JsonDocumentOptions.MaxDepth);
    }

    internal static class OutputParameterNames
    {
        internal const string ValueKind = nameof(JsonElement.ValueKind);
        internal const string PropertyNames = nameof(PropertyNames);
    }

    public void Execute(IServiceProvider serviceProvider)
    {
        var context = serviceProvider.Get<IPluginExecutionContext>();
        var trace = serviceProvider.Get<ITracingService>();
        ParameterCollection outputs = context.OutputParameters;

        string? jsonString = context.InputParameterOrDefault<string?>(
            InputParameterNames.JsonString
            );
        if (string.IsNullOrEmpty(jsonString))
        {
            outputs[OutputParameterNames.ValueKind] = nameof(JsonValueKind.Undefined);
            return;
        }

        JsonDocumentOptions jsonOptions = new();
        if (context.InputParameters.TryGetValue(InputParameterNames.AllowTrailingCommas, out bool allowCommas))
        {
            jsonOptions.AllowTrailingCommas = allowCommas;
        }
        if (context.InputParameters.TryGetValue(InputParameterNames.CommentHandling, out string commentHandlingString))
        {
            try
            {
                jsonOptions.CommentHandling = (JsonCommentHandling)Enum.Parse(
                    typeof(JsonCommentHandling),
                    commentHandlingString,
                    ignoreCase: true
                    );
            }
            catch (Exception)
            {
                throw new InvalidPluginExecutionException(
                    httpStatus: PluginHttpStatusCode.BadRequest,
                    message: $"Failed to parse input parameter value '{InputParameterNames.CommentHandling}': {commentHandlingString}"
                    );
            }
        }
        if (context.InputParameters.TryGetValue(InputParameterNames.MaxDepth, out int maxDepth))
        {
            jsonOptions.MaxDepth = maxDepth;
        }

        JsonDocument jsonDocument;
        try
        {
            jsonDocument = JsonDocument.Parse(jsonString!, jsonOptions);
        }
        catch (JsonException jsonParseExcept)
        {
            trace.Trace("{0}", jsonParseExcept);
            outputs[OutputParameterNames.ValueKind] = nameof(JsonValueKind.Undefined);
            return;
        }

        outputs[OutputParameterNames.ValueKind] = jsonDocument?.RootElement.ValueKind.ToString();
        if (jsonDocument is { RootElement.ValueKind: JsonValueKind.Object })
        {
            List<string> propertyNames = [];
            foreach (JsonProperty jsonProp in jsonDocument.RootElement.EnumerateObject())
            {
                propertyNames.Add(jsonProp.Name);
            }
            outputs[OutputParameterNames.PropertyNames] = propertyNames.ToArray();
        }
    }
}