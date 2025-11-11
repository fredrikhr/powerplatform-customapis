using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace FredrikHr.PowerPlatformRuntimeExtensions.CommonPlugins;

internal static class UuidHelper
{
    internal static Guid CreateRfc9562Guid(ReadOnlySpan<byte> bigEndianBytes, int version)
    {
        ReadOnlySpan<byte> bytes = bigEndianBytes;
        if (bytes.Length < 16)
        {
            throw new ArgumentException(
                message: "",
                paramName: nameof(bigEndianBytes)
                );
        }

        int a = BinaryPrimitives.ReadInt32BigEndian(bytes[..4]);
        short b = BinaryPrimitives.ReadInt16BigEndian(bytes[4..]);
        short c = BinaryPrimitives.ReadInt16BigEndian(bytes[6..]);
        byte d = bytes[8], e = bytes[9], f = bytes[10], g = bytes[11],
            h = bytes[12], i = bytes[13], j = bytes[14], k = bytes[15];

        const byte uuidNotVariantMask = 0b_0011_1111;
        const byte uuidVariant = 0b1000_0000;
        d &= uuidNotVariantMask;
        d |= uuidVariant;

        const short uuidNotVersionMask = 0b_0000_1111_1111_1111;
        short versionValue = unchecked((short)(version << 12));
        c &= uuidNotVersionMask;
        c |= versionValue;

        return new(a, b, c, d, e, f, g, h, i, j, k);
    }

    internal static bool TryWriteBytes(this Guid guid, Span<byte> destination, bool bigEndian, out int bytesWritten)
    {
        if (destination.Length < 16)
        {
            bytesWritten = 0;
            return false;
        }

        MemoryMarshal.Write(destination, ref guid);
        bytesWritten = 16;
        if (BitConverter.IsLittleEndian == bigEndian)
        {
            Span<int> int32 = MemoryMarshal.Cast<byte, int>(destination[0..4]);
            int32[0] = BinaryPrimitives.ReverseEndianness(int32[0]);
            Span<short> int16 = MemoryMarshal.Cast<byte, short>(destination[4..8]);
            int16[0] = BinaryPrimitives.ReverseEndianness(int16[0]);
            int16[1] = BinaryPrimitives.ReverseEndianness(int16[1]);
        }
        return true;
    }
}