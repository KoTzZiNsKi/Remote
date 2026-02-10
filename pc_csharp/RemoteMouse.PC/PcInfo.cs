using System.Management;

namespace RemoteMouse.PC;

public static class PcInfo
{
    private static string? _cachedModel;
    private static readonly object _lock = new();

    public static string? GetModel()
    {
        lock (_lock)
        {
            if (_cachedModel != null) return _cachedModel;
        }
        string? result = null;
        try
        {
            using var searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT Manufacturer, Model FROM Win32_ComputerSystem");
            foreach (var queryObj in searcher.Get())
            {
                var manufacturer = queryObj["Manufacturer"]?.ToString()?.Trim();
                var model = queryObj["Model"]?.ToString()?.Trim();
                if (string.IsNullOrEmpty(model)) continue;
                if (!string.IsNullOrEmpty(manufacturer) && !model.StartsWith(manufacturer, StringComparison.OrdinalIgnoreCase))
                    result = $"{manufacturer} {model}";
                else
                    result = model;
                break;
            }
        }
        catch { }
        if (result != null && result.Length > 64)
            result = result.Substring(0, 64);
        lock (_lock)
        {
            _cachedModel = result;
        }
        return result;
    }
}
