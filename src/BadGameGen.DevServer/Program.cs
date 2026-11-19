using BadGameGen.App;
using Microsoft.Extensions.FileProviders;

// Development host: serves the same RPC API as the desktop shell over HTTP, plus the built frontend.
// Run:  dotnet run --project src/BadGameGen.DevServer   then open http://localhost:5178
// With hot reload: run this, then `npm run dev` in frontend/ (Vite proxies /api here).
var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls(builder.Configuration["urls"] ?? "http://localhost:5178");
var app = builder.Build();

var contentDir = AppPaths.FindContentDir();
var repoRoot = Path.GetFullPath(Path.Combine(contentDir, ".."));
var dataDir = builder.Configuration["dataDir"] ?? Path.Combine(repoRoot, ".devdata");
var service = new BadGameGenService(new AppPaths(contentDir, dataDir));
var api = new ApiDispatcher(service);

app.MapPost("/api/rpc", async (HttpRequest request) =>
{
    using var reader = new StreamReader(request.Body);
    var body = await reader.ReadToEndAsync();
    return Results.Content(api.HandleJson(body), "application/json");
});

var dist = Path.Combine(repoRoot, "frontend", "dist");
if (Directory.Exists(dist))
{
    var files = new PhysicalFileProvider(dist);
    app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = files });
    app.UseStaticFiles(new StaticFileOptions { FileProvider = files });
}
else
{
    app.MapGet("/", () => Results.Text("Frontend not built. Run `npm install && npm run build` in frontend/, or `npm run dev` for hot reload."));
}

Console.WriteLine($"BadGameGen dev server. Content: {contentDir}  Data: {dataDir}");
app.Run();
