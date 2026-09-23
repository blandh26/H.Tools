using HTools.Server.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace HTools.Server.Modules;

public sealed class FileUploadServerModule : ServerModuleBase
{
    public string UploadFolder { get; set; } = string.Empty;

    /// <summary>Text for the plain upload-form page served on GET, supplied by the caller (which owns
    /// localization) so this server-side project has no UI-language dependency of its own.</summary>
    public string PageTitle { get; set; } = "H.Tools Upload";

    public string UploadButtonText { get; set; } = "Upload";

    public string SuccessText { get; set; } = "Uploaded";

    public event EventHandler<string>? FileReceived;

    protected override void Configure(WebApplication app)
    {
        app.Run(async ctx =>
        {
            Directory.CreateDirectory(UploadFolder);

            if (HttpMethods.IsGet(ctx.Request.Method))
            {
                ctx.Response.ContentType = "text/html; charset=utf-8";
                await ctx.Response.WriteAsync(BuildUploadFormHtml());
                return;
            }

            if (ctx.Request.HasFormContentType)
            {
                var form = await ctx.Request.ReadFormAsync();
                foreach (var file in form.Files)
                {
                    var safeName = Path.GetFileName(file.FileName);
                    var destination = Path.Combine(UploadFolder, $"{DateTime.Now:yyyyMMdd_HHmmss}_{safeName}");
                    await using var stream = File.Create(destination);
                    await file.CopyToAsync(stream);
                    FileReceived?.Invoke(this, destination);
                }
            }
            else
            {
                var destination = Path.Combine(UploadFolder, $"{DateTime.Now:yyyyMMdd_HHmmss}_upload.bin");
                await using var stream = File.Create(destination);
                await ctx.Request.Body.CopyToAsync(stream);
                FileReceived?.Invoke(this, destination);
            }

            ctx.Response.StatusCode = 200;
            await ctx.Response.WriteAsync(SuccessText);
        });
    }

    private string BuildUploadFormHtml() => $"""
        <html><body style="font-family:sans-serif">
        <h3>{PageTitle}</h3>
        <form method="post" enctype="multipart/form-data">
        <input type="file" name="file" />
        <button type="submit">{UploadButtonText}</button>
        </form>
        </body></html>
        """;
}
