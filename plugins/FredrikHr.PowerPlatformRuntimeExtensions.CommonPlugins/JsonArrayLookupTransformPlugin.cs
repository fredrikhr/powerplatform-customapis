using System.Text.Json;

namespace FredrikHr.PowerPlatformRuntimeExtensions.CommonPlugins;

public class JsonArrayLookupTransformPlugin : IPlugin
{
    internal static class InputParameterNames
    {
        internal const string JsonString = nameof(JsonString);
        internal const string KeyPropertyName = nameof(KeyPropertyName);
        internal const string AllowTrailingCommas = nameof(JsonDocumentOptions.AllowTrailingCommas);
        internal const string CommentHandling = nameof(JsonDocumentOptions.CommentHandling);
        internal const string MaxDepth = nameof(JsonDocumentOptions.MaxDepth);
        internal const string Indented = nameof(JsonWriterOptions.Indented);
    }

    internal static class OutputParameterNames
    {
        internal const string JsonString = nameof(JsonString);
    }

    public void Execute(IServiceProvider serviceProvider)
    {
        var context = serviceProvider.Get<IPluginExecutionContext>();
        var trace = serviceProvider.Get<ITracingService>();
        ParameterCollection outputs = context.OutputParameters;

        var keyPropertyName = context.InputParameterOrDefault<string?>(
            InputParameterNames.KeyPropertyName
            );
        if (string.IsNullOrEmpty(keyPropertyName))
        {
            throw new InvalidPluginExecutionException(
                httpStatus: PluginHttpStatusCode.BadRequest,
                message: $"Input parameter '{InputParameterNames.KeyPropertyName}' may not be empty, null or missing."
                );
        }

        var jsonString = context.InputParameterOrDefault<string?>(
            InputParameterNames.JsonString
            );
        if (string.IsNullOrEmpty(jsonString))
        {
            throw new InvalidPluginExecutionException(
                httpStatus: PluginHttpStatusCode.BadRequest,
                message: $"Input parameter '{InputParameterNames.JsonString}' may not be empty, null or missing."
                );
        }

        JsonDocumentOptions jsonOptions = new();
        JsonWriterOptions writerOptions = new();
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
            writerOptions.MaxDepth = maxDepth;
        }
        if (context.InputParameters.TryGetValue(InputParameterNames.Indented, out bool indented))
        {
            writerOptions.Indented = indented;
        }

        JsonDocument jsonDocument;
        try
        {
            jsonDocument = JsonDocument.Parse(jsonString!, jsonOptions);
        }
        catch (JsonException jsonParseExcept)
        {
            trace.Trace("{0}", jsonParseExcept);
            throw new InvalidPluginExecutionException(
                httpStatus: PluginHttpStatusCode.BadRequest,
                message: $"Invalid JSON in parameter value '{InputParameterNames.JsonString}': {jsonParseExcept.Message}"
                );
        }

        if (jsonDocument is not { RootElement.ValueKind: JsonValueKind.Array })
        {
            throw new InvalidPluginExecutionException(
                httpStatus: PluginHttpStatusCode.BadRequest,
                message: $"Invalid JSON in parameter value '{InputParameterNames.JsonString}': Expected root element value kind {JsonValueKind.Array} but got {jsonDocument.RootElement.ValueKind}."
                );
        }

        using MemoryStream resultStream = new();
        using Utf8JsonWriter resultWriter = new(resultStream, writerOptions);

        resultWriter.WriteStartObject();

        int jsonIdx = 0;
        foreach (JsonElement jsonElement in jsonDocument.RootElement.EnumerateArray())
        {
            if (
                jsonElement is not { ValueKind: JsonValueKind.Object } ||
                !jsonElement.TryGetProperty(keyPropertyName!, out var keyProperty) ||
                keyProperty.GetString() is not { Length: > 0 } keyPropertyValue
                )
            {
                trace.Trace("Skipping element at index {0}. Not a JSON object, or object does not contain a value for key property named '{1}'.", jsonIdx, keyPropertyName);
                jsonIdx++;
                continue;
            }

            try
            {
                resultWriter.WritePropertyName(keyPropertyValue);
                jsonElement.WriteTo(resultWriter);
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception jsonWriteExcept)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                trace.Trace("Failed to write element at index {0} with key property ('{1}') value '{2}': {3}", jsonIdx, keyPropertyName, keyPropertyValue, jsonWriteExcept.Message);
            }

            jsonIdx++;
        }

        resultWriter.WriteEndObject();
        resultWriter.Flush();

        outputs[OutputParameterNames.JsonString] = System.Text.Encoding
            .UTF8.GetString(resultStream.ToArray());
    }
}