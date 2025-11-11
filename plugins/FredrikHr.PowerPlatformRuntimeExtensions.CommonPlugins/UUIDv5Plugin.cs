using System.Security.Cryptography;

namespace FredrikHr.PowerPlatformRuntimeExtensions.CommonPlugins;

public sealed class UUIDv5Plugin : IPlugin
{
    private static readonly Dictionary<string, Guid> WellKnownNamspaces = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Dns", new("6ba7b810-9dad-11d1-80b4-00c04fd430c8") },
        { "Url", new("6ba7b811-9dad-11d1-80b4-00c04fd430c8") },
        { "Oid", new("6ba7b812-9dad-11d1-80b4-00c04fd430c8") },
        { "X500", new("6ba7b814-9dad-11d1-80b4-00c04fd430c8") },
        { "Nil", Guid.Empty },
        { "Max", new("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF") },
    };

    internal static class InputParameterNames
    {
        internal const string WellKnownNamespace = nameof(WellKnownNamespace);
        internal const string NamespaceId = nameof(NamespaceId);
        internal const string NameText = nameof(NameText);
    }

    internal static class OutputParameterNames
    {
        internal const string NamespaceId = nameof(NamespaceId);
        internal const string Value = nameof(Value);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Security",
        "CA5350: Do Not Use Weak Cryptographic Algorithms",
        Justification = "RFC9562, Section 5.5 UUID Version 5"
        )]
    public void Execute(IServiceProvider serviceProvider)
    {
        var context = serviceProvider.Get<IPluginExecutionContext>();
        ParameterCollection inputs = context.InputParameters;
        ParameterCollection outputs = context.OutputParameters;

        Guid namespaceId = Guid.Empty;
        if (inputs.TryGetValue(InputParameterNames.WellKnownNamespace,
            out string wellKnownNamespace))
        {
            if (!WellKnownNamspaces.TryGetValue(
                wellKnownNamespace, out namespaceId
                ))
            {
                throw new InvalidPluginExecutionException(
                    httpStatus: PluginHttpStatusCode.BadRequest,
                    message: $"Unrecognized well-known UUID version 5 Namespace: '{wellKnownNamespace}'."
                    );
            }
        }
        if (inputs.TryGetValue(InputParameterNames.NamespaceId, out Guid namespaceIdFromInput) &&
            namespaceIdFromInput != Guid.Empty)
        { namespaceId = namespaceIdFromInput; }
        Span<byte> namespaceBytes = stackalloc byte[16];
        _ = namespaceId.TryWriteBytes(namespaceBytes, bigEndian: true, out _);

        byte[] nameBytes = [];
        if (inputs.TryGetValue(InputParameterNames.NameText, out string nameText))
        {
            nameText = nameText.Normalize(System.Text.NormalizationForm.FormC);
            nameBytes = System.Text.Encoding.UTF8.GetBytes(nameText);
        }

        byte[] hashInput = [.. namespaceBytes, .. nameBytes];
        byte[] hashBytes;
        using (SHA1 sha1 = SHA1.Create())
        { hashBytes = sha1.ComputeHash(hashInput); }

        outputs[OutputParameterNames.NamespaceId] = namespaceId;
        outputs[OutputParameterNames.Value] =
            UuidHelper.CreateRfc9562Guid(hashBytes, 5);
    }
}