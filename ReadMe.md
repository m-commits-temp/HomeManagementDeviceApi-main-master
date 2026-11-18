## Device monitoring dashboard

Select the **Dashboard** launch profile in Visual Studio, or run:

```cmd
dotnet run --launch-profile Dashboard
```

Open [the dashboard](http://localhost:1238/dashboard/). It starts independently and returns JSON with `onlineCount`, `totalCount`, and `devices`. Reload the response to check the devices again.

To bring devices online, run each command in its own terminal while the dashboard is running:

```cmd
dotnet run --launch-profile Device1
dotnet run --launch-profile Device2
```

The device APIs expose `/info` at `http://localhost:1235/info` and `http://localhost:1237/info`.

Uptime counts seconds since the device process started. Device1 and Device2 simulate temperatures from 30 to 40 °C with 90- and 120-second cycles. The dashboard displays the metrics reported by `/info`.

All three launch profiles listen on localhost HTTP and require no development certificate.

To monitor other devices, edit `Dashboard:Devices` in `appsettings.Dashboard.json`. Set each device's `Name` and `InfoUrl` to its existing `/info` endpoint.
