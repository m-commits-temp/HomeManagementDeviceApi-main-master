using System.Text.Json;
using HomeManagement.Shared;

namespace HomeManagementDeviceApi;

internal sealed class DeviceDashboard(HttpClient client, IConfiguration configuration)
{
    public Task<DashboardDevice[]> GetDevicesAsync(CancellationToken cancellationToken)
    {
        var devices = configuration.GetSection("Dashboard:Devices").Get<MonitoredDevice[]>() ?? [];
        return Task.WhenAll(devices.Select(device => GetPeerAsync(device, cancellationToken)));
    }


    private async Task<DashboardDevice> GetPeerAsync(MonitoredDevice peer, CancellationToken cancellationToken)
    {
        try
        {
            var device = await client.GetFromJsonAsync<DeviceInfo>(peer.InfoUrl, cancellationToken);
            if (device is not null)
            {
                var name = string.IsNullOrWhiteSpace(device.Name) ? peer.Name : device.Name;
                return new DashboardDevice(name, peer.InfoUrl.GetLeftPart(UriPartial.Authority), true, device.Metrics);
            }
        }
        catch (HttpRequestException)
        {
            // The device did not return a successful response.
        }
        catch (JsonException)
        {
            // The response did not contain valid device information.
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // The device did not respond before the HTTP client timed out.
        }

        return new DashboardDevice(peer.Name, peer.InfoUrl.GetLeftPart(UriPartial.Authority), false, null);
    }

    private sealed record MonitoredDevice(string Name, Uri InfoUrl);
}

internal sealed record DashboardDevice(string Name, string Address, bool Online, Dictionary<string, string>? Metrics);
