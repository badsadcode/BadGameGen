using System.Diagnostics;
using System.Runtime.InteropServices;
using BadGameGen.App;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace BadGameGen.Desktop;

/// <summary>
/// The desktop shell: a window hosting the React frontend in WebView2.
/// The frontend talks to <see cref="ApiDispatcher"/> through WebView2 web messages:
///   JS → host: window.chrome.webview.postMessage(jsonRequestString)
///   host → JS: PostWebMessageAsString(jsonResponseString)
/// Files are served from a virtual https host so the page is a secure context (clipboard works).
/// </summary>
public sealed class MainForm : Form
{
    private const string AppHost = "app.badgamegen.local";
    private readonly WebView2 _web = new() { Dock = DockStyle.Fill, DefaultBackgroundColor = Color.FromArgb(15, 10, 19) };
    private readonly ApiDispatcher _api;
    private readonly string _dataDir;
    private readonly string _appRoot;

    public MainForm()
    {
        Text = "BadGameGen";
        Width = 1560;
        Height = 980;
        MinimumSize = new Size(1100, 720);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(15, 10, 19);
        // The icon is embedded in the exe (ApplicationIcon), so this also works for single-file builds.
        try { Icon = Icon.ExtractAssociatedIcon(Environment.ProcessPath!); } catch (ArgumentException) { } catch (IOException) { }
        Controls.Add(_web);

        _dataDir = AppPaths.DefaultDataDir();
        _appRoot = BundledContent.ResolveAppRoot(typeof(MainForm).Assembly, _dataDir);
        var service = new BadGameGenService(new AppPaths(Path.Combine(_appRoot, "content"), _dataDir));
        _api = new ApiDispatcher(service);
        Load += async (_, _) => await InitializeWebViewAsync();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        // Dark title bar on Windows 10 20H1+ / 11. Harmless if unsupported.
        int on = 1;
        _ = DwmSetWindowAttribute(Handle, 20, ref on, sizeof(int));
    }

    private async Task InitializeWebViewAsync()
    {
        try
        {
            var env = await CoreWebView2Environment.CreateAsync(null, Path.Combine(_dataDir, "WebView2"));
            await _web.EnsureCoreWebView2Async(env);
        }
        catch (WebView2RuntimeNotFoundException)
        {
            MessageBox.Show(this,
                "BadGameGen needs the Microsoft Edge WebView2 Runtime, which isn't installed.\n\n" +
                "Get it from: https://developer.microsoft.com/microsoft-edge/webview2/",
                "WebView2 missing", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Close();
            return;
        }

        var core = _web.CoreWebView2;
        var wwwroot = Path.Combine(_appRoot, "wwwroot");
        core.SetVirtualHostNameToFolderMapping(AppHost, wwwroot, CoreWebView2HostResourceAccessKind.DenyCors);
#if !DEBUG
        core.Settings.AreDevToolsEnabled = false;
        core.Settings.AreDefaultContextMenusEnabled = false;
#endif
        core.Settings.IsStatusBarEnabled = false;
        core.Settings.IsZoomControlEnabled = true;

        core.WebMessageReceived += async (_, args) =>
        {
            string request;
            try { request = args.TryGetWebMessageAsString(); }
            catch (ArgumentException) { return; }
            var response = await Task.Run(() => _api.HandleJson(request));
            core.PostWebMessageAsString(response);
        };

        // Keep the app inside its own host; anything else opens in the user's browser.
        core.NavigationStarting += (_, args) =>
        {
            if (Uri.TryCreate(args.Uri, UriKind.Absolute, out var uri) && uri.Host != AppHost && uri.Scheme is "http" or "https")
            {
                args.Cancel = true;
                OpenExternal(uri);
            }
        };
        core.NewWindowRequested += (_, args) =>
        {
            args.Handled = true;
            if (Uri.TryCreate(args.Uri, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https") OpenExternal(uri);
        };

        if (!File.Exists(Path.Combine(wwwroot, "index.html")))
        {
            core.NavigateToString("<body style='background:#0f0a13;color:#f4ecf7;font-family:Segoe UI;padding:40px'>" +
                                  "<h2>Frontend not found</h2><p>Run <code>npm install</code> and <code>npm run build</code> in <code>frontend/</code>, then rebuild.</p></body>");
            return;
        }
        _web.Source = new Uri($"https://{AppHost}/index.html");
    }

    private static void OpenExternal(Uri uri) =>
        Process.Start(new ProcessStartInfo(uri.ToString()) { UseShellExecute = true });

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
}
