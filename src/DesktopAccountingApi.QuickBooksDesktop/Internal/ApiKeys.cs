using System.Text;

namespace DesktopAccountingApi.QuickBooksDesktop;

/// <summary>
/// Local secret-key check: <c>sk_live_</c> or <c>sk_test_</c> + 40 base62 characters, where the
/// last 6 are the zero-padded base62 CRC32 (IEEE) of the 34 random characters before them. Catches
/// typos and truncated keys before any request is sent.
/// </summary>
internal static class ApiKeys
{
    private const string Base62 = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
    private const int RandomLength = 34;
    private const int ChecksumLength = 6;
    private static readonly uint[] s_table = BuildTable();

    public static bool IsValid(string? key)
    {
        if (key is null) return false;
        if (key.Length != 8 + RandomLength + ChecksumLength) return false;
        if (!key.StartsWith("sk_live_", System.StringComparison.Ordinal) && !key.StartsWith("sk_test_", System.StringComparison.Ordinal)) return false;
        for (var i = 8; i < key.Length; i++)
        {
            if (Base62.IndexOf(key[i]) < 0) return false;
        }
        var random = key.Substring(8, RandomLength);
        return Checksum(random) == key.Substring(8 + RandomLength);
    }

    public static void Validate(string key)
    {
        if (!IsValid(key))
        {
            throw new DaapiException("The API key is not a valid Desktop Accounting API secret key (expected sk_live_... or sk_test_... with 40 characters after the prefix). Copy it again from the dashboard or check DAAPI_SECRET_KEY.");
        }
    }

    internal static string Checksum(string random)
    {
        var value = Crc32(Encoding.UTF8.GetBytes(random));
        var chars = new char[ChecksumLength];
        for (var i = ChecksumLength - 1; i >= 0; i--)
        {
            chars[i] = Base62[(int)(value % 62)];
            value /= 62;
        }
        return new string(chars);
    }

    internal static uint Crc32(byte[] bytes)
    {
        var crc = 0xFFFFFFFFu;
        foreach (var b in bytes) crc = s_table[(crc ^ b) & 0xFF] ^ (crc >> 8);
        return crc ^ 0xFFFFFFFFu;
    }

    private static uint[] BuildTable()
    {
        var table = new uint[256];
        for (uint n = 0; n < 256; n++)
        {
            var c = n;
            for (var k = 0; k < 8; k++) c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
            table[n] = c;
        }
        return table;
    }
}
