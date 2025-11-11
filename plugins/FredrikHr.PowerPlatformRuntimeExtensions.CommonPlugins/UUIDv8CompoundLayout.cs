using System.Buffers.Binary;
using System.IO.Hashing;
using System.Text;

namespace FredrikHr.PowerPlatformRuntimeExtensions.CommonPlugins;

internal static class UUIDv8CompoundLayout
{
    private static readonly System.Globalization.CultureInfo Inv =
        System.Globalization.CultureInfo.InvariantCulture;

    private static string HashBytesToString(ReadOnlySpan<byte> hashBytes)
    {
        StringBuilder hashDigestBuilder = new(capacity: hashBytes.Length * 2);
        foreach (byte hashByte in hashBytes)
        { hashDigestBuilder.Append(hashByte.ToString("x2", Inv)); }
        return hashDigestBuilder.ToString();
    }

    internal static class I32
    {
        internal static Guid Encode(byte[] hashInput, Entity parameters, out string hashOutput)
        {
            Span<byte> hashBytes = XxHash3.Hash(hashInput);
            Span<byte> guidBytes = stackalloc byte[16];
            BinaryPrimitives.WriteInt32BigEndian(guidBytes[12..],
                parameters.GetAttributeValue<int>("Value0")
                );
            hashBytes[..4].CopyTo(guidBytes[..4]);
            hashBytes[4..].CopyTo(guidBytes[8..]);
            Guid guid = UuidHelper.CreateRfc9562Guid(guidBytes, 8);
            _ = guid.TryWriteBytes(guidBytes, bigEndian: true, out _);
            guidBytes[..4].CopyTo(hashBytes[..4]);
            guidBytes[8..12].CopyTo(hashBytes[4..]);
            hashOutput = HashBytesToString(hashBytes);
            return guid;
        }

        internal static void Decode(Guid guid, Entity outputs)
        {
            Span<byte> guidBytes = stackalloc byte[16];
            _ = guid.TryWriteBytes(guidBytes, bigEndian: true, out _);
            Span<byte> hashBytes = stackalloc byte[64 / 8];
            guidBytes[..4].CopyTo(hashBytes[..4]);
            guidBytes[8..12].CopyTo(hashBytes[4..]);
            outputs["HashOutput"] = HashBytesToString(hashBytes);
            outputs["Value0"] = BinaryPrimitives.ReadInt32BigEndian(guidBytes[12..]);
        }
    }

    internal static class I32I32
    {
        internal static Guid Encode(byte[] hashInput, Entity parameters, out string hashOutput)
        {
            Span<byte> guidBytes = stackalloc byte[16];
            Span<byte> hashBytes = XxHash3.Hash(hashInput);
            hashBytes.CopyTo(guidBytes[4..]);
            BinaryPrimitives.WriteInt32BigEndian(guidBytes[12..],
                parameters.GetAttributeValue<int>("Value0")
                );
            BinaryPrimitives.WriteInt32BigEndian(guidBytes,
                parameters.GetAttributeValue<int>("Value1")
                );
            Guid guid = UuidHelper.CreateRfc9562Guid(guidBytes, 8);
            _ = guid.TryWriteBytes(guidBytes, bigEndian: true, out _);
            hashOutput = HashBytesToString(guidBytes[4..12]);
            return guid;
        }

        internal static void Decode(Guid guid, Entity outputs)
        {
            Span<byte> guidBytes = stackalloc byte[16];
            _ = guid.TryWriteBytes(guidBytes, bigEndian: true, out _);
            outputs["HashOutput"] = HashBytesToString(guidBytes[4..12]);
            outputs["Value0"] = BinaryPrimitives.ReadInt32BigEndian(guidBytes[12..]);
            outputs["Value1"] = BinaryPrimitives.ReadInt32BigEndian(guidBytes);
        }
    }

    internal static class I64
    {
        internal static Guid Encode(byte[] hashInput, Entity parameters, out string hashOutput)
        {
            byte[] hashBytes = XxHash3.Hash(hashInput);
            Span<byte> guidBytes = stackalloc byte[16];
            Span<byte> value0Bytes = stackalloc byte[sizeof(long)];
            BinaryPrimitives.WriteInt64BigEndian(value0Bytes,
                parameters.GetAttributeValue<long>("Value0")
                );
            value0Bytes[..4].CopyTo(guidBytes);
            value0Bytes[4..].CopyTo(guidBytes[12..]);
            hashBytes.CopyTo(guidBytes[4..]);
            Guid guid = UuidHelper.CreateRfc9562Guid(guidBytes, 8);
            _ = guid.TryWriteBytes(guidBytes, bigEndian: true, out _);
            hashOutput = HashBytesToString(guidBytes[4..12]);
            return guid;
        }

        internal static void Decode(Guid guid, Entity outputs)
        {
            Span<byte> guidBytes = stackalloc byte[16];
            _ = guid.TryWriteBytes(guidBytes, bigEndian: true, out _);
            outputs["HashOutput"] = HashBytesToString(guidBytes[4..12]);
            Span<byte> value0Bytes = stackalloc byte[sizeof(long)];
            guidBytes[..4].CopyTo(value0Bytes);
            guidBytes[12..].CopyTo(value0Bytes[4..]);
            outputs["Value0"] = BinaryPrimitives.ReadInt64BigEndian(value0Bytes);
        }
    }

    internal static class I64I32
    {
        internal static Guid Encode(byte[] hashInput, Entity parameters, out string hashOutput)
        {
            byte[] hashBytes = XxHash32.Hash(hashInput);
            Span<byte> guidBytes = stackalloc byte[16];
            Span<byte> value0Bytes = stackalloc byte[sizeof(long)];
            BinaryPrimitives.WriteInt64BigEndian(value0Bytes,
                parameters.GetAttributeValue<long>("Value0")
                );
            Span<byte> value1Bytes = stackalloc byte[sizeof(int)];
            BinaryPrimitives.WriteInt32BigEndian(value1Bytes,
                parameters.GetAttributeValue<int>("Value1")
                );
            value0Bytes[..4].CopyTo(guidBytes);
            value0Bytes[4..].CopyTo(guidBytes[12..]);
            value1Bytes[..2].CopyTo(guidBytes[4..]);
            value1Bytes[2..].CopyTo(guidBytes[10..]);
            hashBytes.CopyTo(guidBytes[6..]);
            Guid guid = UuidHelper.CreateRfc9562Guid(guidBytes, 8);
            _ = guid.TryWriteBytes(guidBytes, bigEndian: true, out _);
            hashOutput = HashBytesToString(guidBytes[6..10]);
            return guid;
        }

        internal static void Decode(Guid guid, Entity outputs)
        {
            Span<byte> guidBytes = stackalloc byte[16];
            _ = guid.TryWriteBytes(guidBytes, bigEndian: true, out _);
            outputs["HashOutput"] = HashBytesToString(guidBytes[6..10]);
            Span<byte> value0Bytes = stackalloc byte[sizeof(long)];
            guidBytes[..4].CopyTo(value0Bytes);
            guidBytes[12..].CopyTo(value0Bytes[4..]);
            outputs["Value0"] = BinaryPrimitives.ReadInt64BigEndian(value0Bytes);
            Span<byte> value1Bytes = stackalloc byte[sizeof(int)];
            guidBytes[4..6].CopyTo(value1Bytes[0..2]);
            guidBytes[10..12].CopyTo(value1Bytes[2..4]);
            outputs["Value1"] = BinaryPrimitives.ReadInt32BigEndian(value1Bytes);
        }
    }
}