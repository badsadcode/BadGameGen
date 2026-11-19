using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace BadGameGen.Core.Generation;

/// <summary>
/// Compact shareable codes ("BGG1-...") that encode a recipe plus the packs it needs.
/// Paste one into BadGameGen and you get exactly the same concept (given the same pack versions).
/// </summary>
public static class RecipeCode
{
    private const string Prefix = "BGG1-";

    public sealed class Payload
    {
        public Recipe Recipe { get; set; } = new();
        public List<string> Packs { get; set; } = new();
    }

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    public static string Encode(Recipe recipe, IEnumerable<string> packIds)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(new Payload { Recipe = recipe, Packs = packIds.Order(StringComparer.Ordinal).ToList() }, Options);
        using var ms = new MemoryStream();
        using (var z = new DeflateStream(ms, CompressionLevel.SmallestSize, leaveOpen: true)) z.Write(json);
        return Prefix + Convert.ToBase64String(ms.ToArray()).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    public static bool TryDecode(string code, out Payload payload, out string error)
    {
        payload = new Payload();
        error = "";
        code = (code ?? "").Trim();
        if (!code.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
        {
            error = "That doesn't look like a BadGameGen recipe code (they start with BGG1-).";
            return false;
        }
        try
        {
            var b64 = code[Prefix.Length..].Replace('-', '+').Replace('_', '/');
            b64 = b64.PadRight(b64.Length + (4 - b64.Length % 4) % 4, '=');
            using var input = new MemoryStream(Convert.FromBase64String(b64));
            using var z = new DeflateStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            var buffer = new byte[8192];
            int read, total = 0;
            while ((read = z.Read(buffer)) > 0)
            {
                total += read;
                if (total > 256 * 1024) { error = "Recipe code is suspiciously large."; return false; }
                output.Write(buffer, 0, read);
            }
            payload = JsonSerializer.Deserialize<Payload>(Encoding.UTF8.GetString(output.ToArray()), Options) ?? new Payload();
            payload.Recipe ??= new Recipe();
            payload.Packs ??= new();
            return true;
        }
        catch (Exception ex) when (ex is FormatException or InvalidDataException or JsonException)
        {
            error = "That recipe code is damaged or incomplete.";
            return false;
        }
    }
}
