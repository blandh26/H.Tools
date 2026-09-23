using HTools.Server.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;

namespace HTools.Server.Modules;

public sealed class StaticFileServerModule : ServerModuleBase
{
    public string RootFolder { get; set; } = string.Empty;

    protected override void Configure(WebApplication app)
    {
        Directory.CreateDirectory(RootFolder);
        var provider = new PhysicalFileProvider(RootFolder);

        app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = provider });
        app.UseStaticFiles(new StaticFileOptions { FileProvider = provider, ServeUnknownFileTypes = true });
        app.UseDirectoryBrowser(new DirectoryBrowserOptions { FileProvider = provider });
        app.MapFallback(() => Results.NotFound());
    }
}
