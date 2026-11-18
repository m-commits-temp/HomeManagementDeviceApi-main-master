using HomeManagement.Shared;
using HomeManagementDeviceApi;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

var dashboardOnly = builder.Environment.IsEnvironment("Dashboard");
if (dashboardOnly)
{
    builder.Services.AddHttpClient<DeviceDashboard>(client => client.Timeout = TimeSpan.FromSeconds(3));
}
else
{
    builder.Services.Configure<CommandsSettings>(builder.Configuration.GetSection("CommandsSettings"));
    builder.Services.AddSingleton(new DeviceManager(builder.Configuration.GetValue<int>("DeviceMetrics:TemperatureCycleSeconds")));
}

var app = builder.Build();

if (dashboardOnly)
{
    app.MapGet("/", () => Results.Redirect("/dashboard/"));
    app.MapGet("/dashboard/", async (HttpContext context, DeviceDashboard dashboard, CancellationToken cancellationToken) =>
    {
        context.Response.Headers.CacheControl = "no-store";
        var devices = await dashboard.GetDevicesAsync(cancellationToken);
        return new
        {
            OnlineCount = devices.Count(device => device.Online),
            TotalCount = devices.Length,
            Devices = devices
        };
    });

    app.Run();
    return;
}

if (app.Configuration.GetValue("DeviceApi:UseHttpsRedirection", true))
{
    app.UseHttpsRedirection();
}


app.MapGet("/info", (HttpContext context, IConfiguration configuration, DeviceManager device) =>
{
    context.Response.Headers.CacheControl = "no-store";
    return new DeviceInfo
    {
        Name = configuration.GetValue("DeviceName", string.Empty),
        Address = NetworkManager.GetAddress(),
        Metrics = new Dictionary<string, string>
        {
            { "UptimeSeconds", device.GetUptime().ToString(CultureInfo.InvariantCulture) },
            { "Temperature", device.GetTemperature().ToString(CultureInfo.InvariantCulture) }
        }
    };
});

app.MapGet("/config", (IConfiguration configuration, IOptions<CommandsSettings> commandsOptions) =>
{
    var actions = new List<DeviceAction>()
    {
        new ("INFO", CommandType.Get, "info"),
        new ("SHUTDOWN", CommandType.Post, "shutdown"),
        new ("RESTART", CommandType.Post, "restart")
    };
    actions.AddRange(commandsOptions.Value.Commands.Select(x => new DeviceAction(x.Name, CommandType.Post, "command", JsonSerializer.Serialize(x.Command))));

    return new DeviceConfig
    {
        Name = configuration.GetValue("DeviceName", string.Empty),
        Address = NetworkManager.GetAddress(),
        Actions = actions
    };
});
app.MapPost("/command", async (Command command, CancellationToken token) =>
{
    if (string.IsNullOrWhiteSpace(command.FileName))
    {
        return "Command file name is required.";
    }

    var arguments = string.Join(' ', command.Arguments);
    var result = await Process.RunAndCaptureTextAsync(new ProcessStartInfo
    {
        FileName = command.FileName,
        Arguments = arguments,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true
    }, token);

    return result.ExitStatus.ExitCode == 0 ? result.StandardOutput : result.StandardError;
});

app.MapPost("/shutdown", () =>
{
    Process.StartAndForget("poweroff");
});

app.MapPost("/restart", () =>
{
    Process.StartAndForget("reboot");
});

app.Run();