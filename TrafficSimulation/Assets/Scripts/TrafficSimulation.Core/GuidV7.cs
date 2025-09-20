using System.Security.Cryptography;

namespace TrafficSimulation.Core;

public static class GuidV7 {
    public static Guid Create() => Create(DateTimeOffset.UtcNow);

    public static Guid Create(DateTimeOffset time) {
        // bytes [0-5]: 48-bit timestamp (milliseconds since Unix epoch)
        // byte [6]: 4 bits version (0111 = 7) + 4 bits random
        // byte [7]: 8 bits random
        // byte [8]: 2 bits variant (10) + 6 bits random
        // bytes [9-15]: 56 bits random
        Span<byte> uuidAsBytes = stackalloc byte[16];
        FillTimePart(uuidAsBytes, time);
        RandomNumberGenerator.Fill(uuidAsBytes[6..]);

        // Set version bits (0111 = 7) in byte 6, bits 4-7
        uuidAsBytes[6] &= 0x0F; // Clear upper 4 bits
        uuidAsBytes[6] |= 0x70; // Set version 7 (0111xxxx)

        // Set variant bits (10) in byte 8, bits 6-7
        uuidAsBytes[8] &= 0x3F; // Clear upper 2 bits
        uuidAsBytes[8] |= 0x80; // Set variant (10xxxxxx)

        return new Guid(uuidAsBytes);
    }

    private static void FillTimePart(Span<byte> uuidAsBytes, DateTimeOffset dateTimeOffset) {
        var currentTimestamp = dateTimeOffset.ToUnixTimeMilliseconds();
        var current = BitConverter.GetBytes(currentTimestamp);
        if (BitConverter.IsLittleEndian) {
            Array.Reverse(current);
        }

        current.AsSpan()[2..8].CopyTo(uuidAsBytes);
    }
}
