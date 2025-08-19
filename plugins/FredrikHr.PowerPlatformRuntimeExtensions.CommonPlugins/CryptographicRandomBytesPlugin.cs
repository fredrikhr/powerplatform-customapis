using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace FredrikHr.PowerPlatformRuntimeExtensions.CommonPlugins;

public class CryptographicRandomBytesPlugin : IPlugin
{
    internal static class InputParameterNames
    {
        internal const string RngName = nameof(RngName);
        internal const string ByteCount = nameof(ByteCount);
        internal const string NonZero = nameof(NonZero);
    }

    internal static class OutputParameterNames
    {
        internal const string DataBase64 = nameof(DataBase64);
        internal const string DataHexString = nameof(DataHexString);
    }

    public void Execute(IServiceProvider serviceProvider)
    {
        var context = serviceProvider.Get<IPluginExecutionContext>();
        var trace = serviceProvider.Get<ITracingService>();
        ParameterCollection outputs = context.OutputParameters;

        int byteCount = context.InputParameterOrDefault<int>(
            InputParameterNames.ByteCount
            );
        var rngName = context.InputParameterOrDefault<string?>(
            InputParameterNames.RngName
            );
        bool nonZero = context.InputParameterOrDefault<bool>(
            InputParameterNames.NonZero
            );

        using var rngProvider = string.IsNullOrEmpty(rngName)
            ? RandomNumberGenerator.Create()
            : RandomNumberGenerator.Create(rngName);

        byte[] randomData;
        try
        {
            randomData = GetArray(byteCount);
            if (nonZero)
            {
                rngProvider.GetNonZeroBytes(randomData);
            }
            else
            {
                rngProvider.GetBytes(randomData);
            }
        }
        catch (Exception rngExcept)
        {
            trace.Trace("While generating random data: {0}", rngExcept);
            throw new InvalidPluginExecutionException(
                httpStatus: PluginHttpStatusCode.InternalServerError,
                message: rngExcept.Message
                );
        }

        randomData ??= [];
        outputs[OutputParameterNames.DataBase64] = Convert.ToBase64String(randomData);
        outputs[OutputParameterNames.DataHexString] = string.Concat(
            randomData.Select(byteValue => byteValue.ToString(
                "x2",
                System.Globalization.CultureInfo.InvariantCulture
                )
            ));
    }

    [SkipLocalsInit()]
    private static unsafe byte[] GetArray(int byteCount)
    {
        return new byte[byteCount];
    }
}