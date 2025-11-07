using System.IO.Hashing;
using System.Reflection;

namespace FredrikHr.PowerPlatformRuntimeExtensions.CommonPlugins;

public class ComputeNonCryptographicHashPlugin : IPlugin
{
    internal static class InputParameterNames
    {
        internal const string HashAlgorithmName = nameof(HashAlgorithmName);
        internal const string PayloadBase64 = nameof(PayloadBase64);
        internal const string PayloadText = nameof(PayloadText);
    }

    internal static class OutputParameterNames
    {
        internal const string HashBase64 = nameof(HashBase64);
        internal const string HashHexString = nameof(HashHexString);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2208: Instantiate argument exceptions correctly", Justification = nameof(IPlugin))]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0079: Remove unnecessary suppression", Justification = "false negative")]
    public void Execute(IServiceProvider serviceProvider)
    {
        var context = serviceProvider.Get<IPluginExecutionContext>();
        var trace = serviceProvider.Get<ITracingService>();
        var hashName = context.InputParameterOrDefault<string?>(
            InputParameterNames.HashAlgorithmName
            );
        if (string.IsNullOrEmpty(hashName))
        {
            throw new InvalidPluginExecutionException(
                httpStatus: PluginHttpStatusCode.BadRequest,
                message: new ArgumentNullException(InputParameterNames.HashAlgorithmName).Message
                );
        }

        ParameterCollection outputs = context.OutputParameters;

        byte[] payloadBytes;
        if (context.InputParameterOrDefault<string?>(
            InputParameterNames.PayloadBase64
            ) is string payloadBase64)
        {
            try
            {
                payloadBytes = Convert.FromBase64String(payloadBase64);
            }
            catch (FormatException base64ParseExcept)
            {
                trace.Trace(
                    "While parsing Base64 data from input parameter {0}: {1}",
                    InputParameterNames.PayloadBase64,
                    base64ParseExcept
                    );
                throw new InvalidPluginExecutionException(
                    httpStatus: PluginHttpStatusCode.BadRequest,
                    message: $"Parameter '{InputParameterNames.PayloadBase64}': {base64ParseExcept.Message}"
                    );
            }
        }
        else
        {
            payloadBytes = context.InputParameterOrDefault<string?>(
                InputParameterNames.PayloadText
                ) is string payloadText
                ? System.Text.Encoding.UTF8.GetBytes(payloadText)
                : [];
        }

        const StringComparison cmp = StringComparison.OrdinalIgnoreCase;
        byte[] hashBytes;
        try
        {
            hashBytes =
                nameof(XxHash32).Equals(hashName, cmp)
                ? XxHash32.Hash(payloadBytes)
                : nameof(XxHash64).Equals(hashName, cmp)
                ? XxHash64.Hash(payloadBytes)
                : nameof(Crc32).Equals(hashName, cmp)
                ? Crc32.Hash(payloadBytes)
                : nameof(Crc64).Equals(hashName, cmp)
                ? Crc64.Hash(payloadBytes)
                : nameof(XxHash3).Equals(hashName, cmp)
                ? XxHash3.Hash(payloadBytes)
                : nameof(XxHash128).Equals(hashName, cmp)
                ? XxHash128.Hash(payloadBytes)
                : HashByName(hashName!, payloadBytes, trace);
        }
        catch (Exception hashingExcept)
        {
            trace.Trace("While performing hashing operation: {0}", hashingExcept);
            throw new InvalidPluginExecutionException(
                httpStatus: PluginHttpStatusCode.InternalServerError,
                message: hashingExcept.Message
                );
        }

        hashBytes ??= [];
        outputs[OutputParameterNames.HashBase64] = Convert.ToBase64String(hashBytes);
        outputs[OutputParameterNames.HashHexString] = string.Concat(
            hashBytes.Select(byteValue => byteValue.ToString(
                "x2",
                System.Globalization.CultureInfo.InvariantCulture
                )
            ));
    }

    private static byte[] HashByName(
        string hashName,
        byte[] payload,
        ITracingService trace
        )
    {
        Assembly hashingAssembly =
            typeof(NonCryptographicHashAlgorithm).Assembly;
        Type hashType;
        try
        {
            hashType = hashingAssembly.GetType(
                $"System.IO.Hashing.{hashName}",
                throwOnError: true,
                ignoreCase: true
                );
        }
        catch (Exception typeLoadExcept)
        {
            trace.Trace("{0}", typeLoadExcept);
            throw new InvalidPluginExecutionException(
                message: $"Specified non-cryptographic hash algorithm name '{hashName}' is not recognized. {typeLoadExcept.Message}",
                httpStatus: PluginHttpStatusCode.BadRequest
                );
        }

        try
        {
            byte[] hashBytes = (byte[])hashType.InvokeMember(
                nameof(Crc32.Hash),
                BindingFlags.Public |
                BindingFlags.Static |
                BindingFlags.InvokeMethod,
                Type.DefaultBinder,
                target: null,
                args: [payload],
                culture: System.Globalization.CultureInfo.InvariantCulture
                );
            return hashBytes;
        }
        catch (TargetInvocationException targetInvokeExcept)
        when (targetInvokeExcept.InnerException is Exception hashExcept)
        {
            trace.Trace("{0}", hashExcept);
            throw new InvalidPluginExecutionException(
                message: $"Error while execting non-cryptographic hash algorithm: {hashExcept.Message}",
                httpStatus: PluginHttpStatusCode.InternalServerError
                );
        }
        catch (Exception hashExcept)
        {
            trace.Trace("{0}", hashExcept);
            throw new InvalidPluginExecutionException(
                message: $"Error while execting non-cryptographic hash algorithm: {hashExcept.Message}",
                httpStatus: PluginHttpStatusCode.InternalServerError
                );
        }
    }
}
