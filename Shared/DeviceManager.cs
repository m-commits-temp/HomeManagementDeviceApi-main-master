using System.Diagnostics;
using System.Globalization;

namespace HomeManagement.Shared;

public sealed class DeviceManager(int temperatureCycleSeconds)
{
    private readonly Stopwatch uptime = Stopwatch.StartNew();

    public long GetUptime() => (long)uptime.Elapsed.TotalSeconds;

    public double GetTemperature()
    {
        if (temperatureCycleSeconds > 0)
        {
            var cycle = uptime.Elapsed.TotalSeconds % temperatureCycleSeconds / temperatureCycleSeconds;
            return Math.Round(35 + 5 * Math.Sin(Math.Tau * cycle), 1);
        }

        try
        {
            var tempText = File.ReadAllText("/sys/class/thermal/thermal_zone0/temp");
            var tempCelsius = double.Parse(tempText, CultureInfo.InvariantCulture) / 1000.0;

            return tempCelsius;
        }
        catch
        {
            return 0;
        }
    }
}