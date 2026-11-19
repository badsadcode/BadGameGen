using System.Text.Json;
using System.Text.Json.Serialization;
using BadGameGen.Core.Generation;
using BadGameGen.Core.Vault;

namespace BadGameGen.App;

/// <summary>
/// The typed API between the frontend and the host, transport-agnostic.
/// Request:  { "id": "7", "method": "concept.generate", "params": { ... } }
/// Response: { "id": "7", "ok": true, "result": ... }  or  { "id": "7", "ok": false, "error": "..." }
/// The desktop shell carries these over WebView2 messages; the dev server over HTTP POST /api/rpc.
/// Keep frontend/src/api.ts in sync when adding methods.
/// </summary>
public sealed class ApiDispatcher
{
    private readonly BadGameGenService _svc;

    public static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DictionaryKeyPolicy = null,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public ApiDispatcher(BadGameGenService service) => _svc = service;

    private sealed class Request
    {
        public JsonElement Id { get; set; }
        public string Method { get; set; } = "";
        public JsonElement Params { get; set; }
    }

    public string HandleJson(string requestJson)
    {
        Request? req;
        try
        {
            req = JsonSerializer.Deserialize<Request>(requestJson, Json);
        }
        catch (JsonException ex)
        {
            return JsonSerializer.Serialize(new { ok = false, error = $"Malformed request: {ex.Message}" }, Json);
        }
        object? id = req?.Id.ValueKind is JsonValueKind.String or JsonValueKind.Number ? req.Id : null;
        try
        {
            var result = Dispatch(req?.Method ?? "", req?.Params ?? default);
            return JsonSerializer.Serialize(new { id, ok = true, result }, Json);
        }
        catch (Exception ex) when (ex is ArgumentException or KeyNotFoundException or JsonException or InvalidOperationException or IOException)
        {
            return JsonSerializer.Serialize(new { id, ok = false, error = ex.Message }, Json);
        }
    }

    private static T P<T>(JsonElement p) =>
        p.ValueKind == JsonValueKind.Undefined || p.ValueKind == JsonValueKind.Null
            ? throw new ArgumentException("Missing params.")
            : p.Deserialize<T>(Json) ?? throw new ArgumentException("Missing params.");

    private sealed record GenerateParams(Recipe Recipe);
    private sealed record OptionsParams(string Category, string? Query);
    private sealed record IdParams(string Id);
    private sealed record CodeParams(string Code);
    private sealed record SaveParams(Concept Concept, string? Notes, List<string>? Tags);
    private sealed record UpdateParams(string Id, string? Title, string? Notes, List<string>? Tags, bool? Favorite, Concept? Concept);
    private sealed record ExportParams(Concept? Concept, string? EntryId, string Format);
    private sealed record PackEnabledParams(string Id, bool Enabled);
    private sealed record ImportParams(string FileName, string Content);

    private object? Dispatch(string method, JsonElement p)
    {
        switch (method)
        {
            case "app.bootstrap": return _svc.Bootstrap();
            case "app.reload": _svc.Reload(); return _svc.Bootstrap();
            case "settings.update": return _svc.UpdateSettings(p);

            case "concept.generate": return _svc.Generate(P<GenerateParams>(p).Recipe);
            case "category.options": { var o = P<OptionsParams>(p); return _svc.Options(o.Category, o.Query); }

            case "recipe.encode": return new { code = _svc.EncodeRecipe(P<GenerateParams>(p).Recipe) };
            case "recipe.decode":
            {
                if (!RecipeCode.TryDecode(P<CodeParams>(p).Code, out var payload, out var error)) throw new ArgumentException(error);
                return payload;
            }

            case "vault.list": return _svc.Vault.List();
            case "vault.get": return _svc.Vault.Get(P<IdParams>(p).Id) ?? throw new KeyNotFoundException("Idea not found.");
            case "vault.save": { var s = P<SaveParams>(p); return _svc.Vault.Save(s.Concept, s.Notes, s.Tags); }
            case "vault.update":
            {
                var u = P<UpdateParams>(p);
                return _svc.Vault.Update(u.Id, new VaultStore.Patch(u.Title, u.Notes, u.Tags, u.Favorite, u.Concept));
            }
            case "vault.duplicate": return _svc.Vault.Duplicate(P<IdParams>(p).Id);
            case "vault.delete": return new { deleted = _svc.Vault.Delete(P<IdParams>(p).Id) };

            case "export.render":
            {
                var e = P<ExportParams>(p);
                if (e.EntryId != null)
                {
                    var entry = _svc.Vault.Get(e.EntryId) ?? throw new KeyNotFoundException("Idea not found.");
                    return _svc.Export(entry.Concept, e.Format, entry.Notes, entry.Title);
                }
                return _svc.Export(e.Concept ?? throw new ArgumentException("Nothing to export."), e.Format, null, null);
            }

            case "packs.list": return _svc.PackInfos();
            case "packs.contents": return _svc.PackContents(P<IdParams>(p).Id) ?? throw new KeyNotFoundException("Pack not found.");
            case "packs.setEnabled": { var e = P<PackEnabledParams>(p); return _svc.SetPackEnabled(e.Id, e.Enabled); }
            case "packs.import": { var i = P<ImportParams>(p); return _svc.ImportPack(i.FileName, i.Content); }
            case "packs.delete": return new { deleted = _svc.DeleteUserPack(P<IdParams>(p).Id) };

            case "stats.get": return _svc.Stats();
            default: throw new ArgumentException($"Unknown method '{method}'.");
        }
    }
}
