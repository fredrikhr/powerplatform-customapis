using System.Security.Cryptography;

namespace FredrikHr.PowerPlatformRuntimeExtensions.CommonPlugins;

public class ComputeHashPlugin : IPlugin
{
    internal static class InputParameterNames
    {
        internal const string HashAlgorithmName = nameof(HashAlgorithmName);
        internal const string PayloadBase64 = nameof(PayloadBase64);
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
        var payloadBase64 = context.InputParameterOrDefault<string?>(
            InputParameterNames.PayloadBase64
            ) ?? string.Empty;
        byte[] payloadBytes;
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

        using HashAlgorithm? hashProvider = HashAlgorithm.Create(hashName);
#pragma warning disable CA1508 // Avoid dead conditional code
        // hashProvider CAN be null
        if (hashProvider is null)
        {
            throw new InvalidPluginExecutionException(
                httpStatus: PluginHttpStatusCode.BadRequest,
                message: $"Specified Hash Algorithm '{hashName}' is not a recognized Cryptographic Hash Algorithm. Consult https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.hashalgorithm.create?view=netframework-4.7.1#system-security-cryptography-hashalgorithm-create(system-string) for valid Hash algorithm names."
                );
        }
#pragma warning restore CA1508 // Avoid dead conditional code
        byte[] hashBytes;
        try
        {
            hashBytes = hashProvider.ComputeHash(payloadBytes);
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
}
