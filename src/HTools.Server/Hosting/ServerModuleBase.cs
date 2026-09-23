using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace HTools.Server.Hosting;

/// <summary>
/// Owns the lifecycle of one embedded Kestrel instance (build/start/stop/dispose). Each concrete
/// module just supplies its own request pipeline via <see cref="Configure"/>; every module in the
/// multi-purpose server tool gets its own independently startable/stoppable instance on its own port.
/// </summary>
public abstract class ServerModuleBase : IAsyncDisposable
{
    private WebApplication? _app;

    public bool IsRunning { get; private set; }

    public int Port { get; private set; }

    public string? LastError { get; private set; }

    public async Task<bool> StartAsync(int port)
    {
        if (IsRunning)
        {
            return true;
        }

        try
        {
            var builder = WebApplication.CreateBuilder();
            builder.Logging.ClearProviders();
            builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

            var app = builder.Build();
            Configure(app);
            await app.StartAsync();

            _app = app;
            Port = port;
            IsRunning = true;
            LastError = null;
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            IsRunning = false;
            return false;
        }
    }

    public async Task StopAsync()
    {
        if (_app is null)
        {
            return;
        }

        await _app.StopAsync();
        await _app.DisposeAsync();
        _app = null;
        IsRunning = false;
    }

    protected abstract void Configure(WebApplication app);

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
    }
}
