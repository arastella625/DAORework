using System.Security.Cryptography;

namespace DAORework.Application.Fixes;

internal static class FixHashes
{
    public static string Of(byte[] content) => Convert.ToHexStringLower(SHA256.HashData(content));
}
