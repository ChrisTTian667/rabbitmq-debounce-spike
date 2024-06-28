using System.Globalization;
using System.Text;

namespace Debounce.Api;

public static class StringExtensions
{
    public static string ComputeSha256Hash(this string rawData)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(rawData));
        var builder = new StringBuilder();
        foreach (var b in bytes)
            builder.Append(b.ToString("x2", CultureInfo.InvariantCulture));

        return builder.ToString();
    }

}
