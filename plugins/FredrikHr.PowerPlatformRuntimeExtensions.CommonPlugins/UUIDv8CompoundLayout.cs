using System.Buffers.Binary;
using System.IO.Hashing;
using System.Text;

namespace FredrikHr.PowerPlatformRuntimeExtensions.CommonPlugins;

internal static class UUIDv8CompoundLayout
{
    internal static class InputParameterNames
    {
        internal const string Value0 = nameof(Value0);
        internal const string Value1 = nameof(Value1);
        internal const string Value2 = nameof(Value2);
    }

    internal static class OutputParameterNames
    {
        internal const string HashOutput = nameof(HashOutput);
        internal const string UuidIsRfc9562Variant = nameof(UuidIsRfc9562Variant);
        internal const string UuidVariant = nameof(UuidVariant);
        internal const string UuidVersion = nameof(UuidVersion);
        internal const string Value0 = nameof(Value0);
        internal const string Value1 = nameof(Value1);
        internal const string Value2 = nameof(Value2);
    }

    private static readonly System.Globalization.CultureInfo Inv =
        System.Globalization.CultureInfo.InvariantCulture;

    private static string HashBytesToString(ReadOnlySpan<byte> hashBytes)
    {
        StringBuilder hashDigestBuilder = new(capacity: hashBytes.Length * 2);
        foreach (byte hashByte in hashBytes)
        { hashDigestBuilder.Append(hashByte.ToString("x2", Inv)); }
        return hashDigestBuilder.ToString();
    }

    internal static T GetValue<T>(this DataCollection<string, object> collection, string name)
    {
        object obj;
        try { obj = collection[name]; }
        catch (KeyNotFoundException missingKeyExcept)
        {
            throw new KeyNotFoundException(
                $"Key: {name}; {missingKeyExcept.Message}",
                missingKeyExcept
                );
        }
        return obj is T value
            ? value
            : (T)Convert.ChangeType(obj, typeof(T), Inv);
    }

    private static void DecodeUuidVariantAndVersion(
        ReadOnlySpan<byte> guidBigEndianBytes,
        DataCollection<string, object> outputs
        )
    {
        byte octet8 = guidBigEndianBytes[8];
        byte variantMsb = (byte)(octet8 >>> ((sizeof(byte) * 8) - 4));
        bool isRfc9562 = variantMsb switch
        {
            0x8 or 0x9 or 0xA or 0xB => true,
            _ => false,
        };
        outputs[OutputParameterNames.UuidIsRfc9562Variant] = isRfc9562;
        outputs[OutputParameterNames.UuidVariant] = variantMsb.ToString("X1", Inv);

        byte octet6 = guidBigEndianBytes[6];
        outputs[OutputParameterNames.UuidVersion] = octet6 >>> ((sizeof(byte) * 8) - 4);
    }

    internal static readonly Dictionary<string, (Func<byte[], DataCollection<string, object>, Guid> encode, Action<Guid, DataCollection<string, object>> decode)> Adapters = new(StringComparer.OrdinalIgnoreCase)
    {
        { nameof(I32), (I32.Encode, I32.Decode) },
        { nameof(I32I32), (I32I32.Encode, I32I32.Decode) },
        { nameof(I32I32I32), (I32I32I32.Encode, I32I32I32.Decode) },
        { nameof(I64), (I64.Encode, I64.Decode) },
        { nameof(I64I32), (I64I32.Encode, I64I32.Decode) },
    };

    private static class I32
    {
        internal static Guid Encode(byte[] hashInput, DataCollection<string, object> parameters)
        {
            Span<byte> hashBytes = XxHash3.Hash(hashInput);
            Span<byte> guidBytes = stackalloc byte[16];
            BinaryPrimitives.WriteInt32BigEndian(guidBytes[12..],
                parameters.GetValue<int>(InputParameterNames.Value0)
                );
            hashBytes[..4].CopyTo(guidBytes[..4]);
            hashBytes[4..].CopyTo(guidBytes[8..]);
            Guid guid = UuidHelper.CreateRfc9562Guid(guidBytes, 8);
            _ = guid.TryWriteBytes(guidBytes, bigEndian: true, out _);
            guidBytes[..4].CopyTo(hashBytes[..4]);
            guidBytes[8..12].CopyTo(hashBytes[4..]);
            parameters[OutputParameterNames.HashOutput] = HashBytesToString(hashBytes);
            return guid;
        }

        internal static void Decode(Guid guid, DataCollection<string, object> outputs)
        {
            Span<byte> guidBytes = stackalloc byte[16];
            _ = guid.TryWriteBytes(guidBytes, bigEndian: true, out _);
            DecodeUuidVariantAndVersion(guidBytes, outputs);
            Span<byte> hashBytes = stackalloc byte[64 / 8];
            guidBytes[..4].CopyTo(hashBytes[..4]);
            guidBytes[8..12].CopyTo(hashBytes[4..]);
            outputs[OutputParameterNames.HashOutput] =
                HashBytesToString(hashBytes);
            outputs[OutputParameterNames.Value0] =
                BinaryPrimitives.ReadInt32BigEndian(guidBytes[12..]);
        }
    }

    private static class I32I32
    {
        internal static Guid Encode(byte[] hashInput, DataCollection<string, object> parameters)
        {
            Span<byte> guidBytes = stackalloc byte[16];
            Span<byte> hashBytes = XxHash3.Hash(hashInput);
            hashBytes.CopyTo(guidBytes[4..]);
            BinaryPrimitives.WriteInt32BigEndian(guidBytes[12..],
                parameters.GetValue<int>(InputParameterNames.Value0)
                );
            BinaryPrimitives.WriteInt32BigEndian(guidBytes,
                parameters.GetValue<int>(InputParameterNames.Value1)
                );
            Guid guid = UuidHelper.CreateRfc9562Guid(guidBytes, 8);
            _ = guid.TryWriteBytes(guidBytes, bigEndian: true, out _);
            parameters[OutputParameterNames.HashOutput] =
                HashBytesToString(guidBytes[4..12]);
            return guid;
        }

        internal static void Decode(Guid guid, DataCollection<string, object> outputs)
        {
            Span<byte> guidBytes = stackalloc byte[16];
            _ = guid.TryWriteBytes(guidBytes, bigEndian: true, out _);
            DecodeUuidVariantAndVersion(guidBytes, outputs);
            outputs[OutputParameterNames.HashOutput] =
                HashBytesToString(guidBytes[4..12]);
            outputs[OutputParameterNames.Value0] =
                BinaryPrimitives.ReadInt32BigEndian(guidBytes[12..]);
            outputs[OutputParameterNames.Value1] =
                BinaryPrimitives.ReadInt32BigEndian(guidBytes);
        }
    }

    private static class I32I32I32
    {
        internal static Guid Encode(byte[] hashInput, DataCollection<string, object> parameters)
        {
            Span<byte> guidBytes = stackalloc byte[16];
            Span<byte> hashBytes = XxHash32.Hash(hashInput);
            hashBytes.CopyTo(guidBytes[6..]);
            BinaryPrimitives.WriteInt32BigEndian(guidBytes[12..],
                parameters.GetValue<int>(InputParameterNames.Value0)
                );
            BinaryPrimitives.WriteInt32BigEndian(guidBytes,
                parameters.GetValue<int>(InputParameterNames.Value1)
                );
            Span<byte> value2Bytes = stackalloc byte[sizeof(int)];
            BinaryPrimitives.WriteInt32BigEndian(value2Bytes,
                parameters.GetValue<int>(InputParameterNames.Value2)
                );
            value2Bytes[..2].CopyTo(guidBytes[4..]);
            value2Bytes[2..].CopyTo(guidBytes[10..]);
            Guid guid = UuidHelper.CreateRfc9562Guid(guidBytes, 8);
            _ = guid.TryWriteBytes(guidBytes, bigEndian: true, out _);
            parameters[OutputParameterNames.HashOutput] =
                HashBytesToString(guidBytes[6..10]);
            return guid;
        }

        internal static void Decode(Guid guid, DataCollection<string, object> outputs)
        {
            Span<byte> guidBytes = stackalloc byte[16];
            _ = guid.TryWriteBytes(guidBytes, bigEndian: true, out _);
            DecodeUuidVariantAndVersion(guidBytes, outputs);
            outputs[OutputParameterNames.HashOutput] =
                HashBytesToString(guidBytes[6..10]);
            outputs[OutputParameterNames.Value0] =
                BinaryPrimitives.ReadInt32BigEndian(guidBytes[12..]);
            outputs[OutputParameterNames.Value1] =
                BinaryPrimitives.ReadInt32BigEndian(guidBytes);
            Span<byte> value2Bytes = stackalloc byte[sizeof(int)];
            guidBytes[4..6].CopyTo(value2Bytes[..2]);
            guidBytes[10..12].CopyTo(value2Bytes[2..]);
            outputs[OutputParameterNames.Value2] =
                BinaryPrimitives.ReadInt32BigEndian(value2Bytes);
        }
    }

    private static class I64
    {
        internal static Guid Encode(byte[] hashInput, DataCollection<string, object> parameters)
        {
            byte[] hashBytes = XxHash3.Hash(hashInput);
            Span<byte> guidBytes = stackalloc byte[16];
            Span<byte> value0Bytes = stackalloc byte[sizeof(long)];
            BinaryPrimitives.WriteInt64BigEndian(value0Bytes,
                parameters.GetValue<long>(InputParameterNames.Value0)
                );
            value0Bytes[..4].CopyTo(guidBytes);
            value0Bytes[4..].CopyTo(guidBytes[12..]);
            hashBytes.CopyTo(guidBytes[4..]);
            Guid guid = UuidHelper.CreateRfc9562Guid(guidBytes, 8);
            _ = guid.TryWriteBytes(guidBytes, bigEndian: true, out _);
            parameters[OutputParameterNames.HashOutput] =
                HashBytesToString(guidBytes[4..12]);
            return guid;
        }

        internal static void Decode(Guid guid, DataCollection<string, object> outputs)
        {
            Span<byte> guidBytes = stackalloc byte[16];
            _ = guid.TryWriteBytes(guidBytes, bigEndian: true, out _);
            DecodeUuidVariantAndVersion(guidBytes, outputs);
            outputs[OutputParameterNames.HashOutput] =
                HashBytesToString(guidBytes[4..12]);
            Span<byte> value0Bytes = stackalloc byte[sizeof(long)];
            guidBytes[..4].CopyTo(value0Bytes);
            guidBytes[12..].CopyTo(value0Bytes[4..]);
            outputs[OutputParameterNames.Value0] =
                BinaryPrimitives.ReadInt64BigEndian(value0Bytes);
        }
    }

    private static class I64I32
    {
        internal static Guid Encode(byte[] hashInput, DataCollection<string, object> parameters)
        {
            byte[] hashBytes = XxHash32.Hash(hashInput);
            Span<byte> guidBytes = stackalloc byte[16];
            Span<byte> value0Bytes = stackalloc byte[sizeof(long)];
            BinaryPrimitives.WriteInt64BigEndian(value0Bytes,
                parameters.GetValue<long>(InputParameterNames.Value0)
                );
            Span<byte> value1Bytes = stackalloc byte[sizeof(int)];
            BinaryPrimitives.WriteInt32BigEndian(value1Bytes,
                parameters.GetValue<int>(InputParameterNames.Value1)
                );
            value0Bytes[..4].CopyTo(guidBytes);
            value0Bytes[4..].CopyTo(guidBytes[12..]);
            value1Bytes[..2].CopyTo(guidBytes[4..]);
            value1Bytes[2..].CopyTo(guidBytes[10..]);
            hashBytes.CopyTo(guidBytes[6..]);
            Guid guid = UuidHelper.CreateRfc9562Guid(guidBytes, 8);
            _ = guid.TryWriteBytes(guidBytes, bigEndian: true, out _);
            parameters[OutputParameterNames.HashOutput] =
                HashBytesToString(guidBytes[6..10]);
            return guid;
        }

        internal static void Decode(Guid guid, DataCollection<string, object> outputs)
        {
            Span<byte> guidBytes = stackalloc byte[16];
            _ = guid.TryWriteBytes(guidBytes, bigEndian: true, out _);
            DecodeUuidVariantAndVersion(guidBytes, outputs);
            outputs[OutputParameterNames.HashOutput] =
                HashBytesToString(guidBytes[6..10]);
            Span<byte> value0Bytes = stackalloc byte[sizeof(long)];
            guidBytes[..4].CopyTo(value0Bytes);
            guidBytes[12..].CopyTo(value0Bytes[4..]);
            outputs[OutputParameterNames.Value0] =
                BinaryPrimitives.ReadInt64BigEndian(value0Bytes);
            Span<byte> value1Bytes = stackalloc byte[sizeof(int)];
            guidBytes[4..6].CopyTo(value1Bytes[0..2]);
            guidBytes[10..12].CopyTo(value1Bytes[2..4]);
            outputs[OutputParameterNames.Value1] =
                BinaryPrimitives.ReadInt32BigEndian(value1Bytes);
        }
    }
}