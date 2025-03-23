using System.Collections;

namespace ShadowKVM;

public class Monitors : IEnumerable<Monitor>, IDisposable
{
    public void Add(Monitor monitor)
    {
        _monitors.Add(monitor);
    }

    public IEnumerator<Monitor> GetEnumerator()
    {
        return _monitors.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (var monitor in _monitors)
            {
                monitor.Dispose();
            }
            _monitors.Clear();
        }
    }

    List<Monitor> _monitors = new List<Monitor>();
}

public class Monitor : IDisposable
{
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (!Handle.IsInvalid)
            {
                Handle.Dispose();
                Handle.SetHandleAsInvalid();
            }
        }
    }

    public required string Description { get; set; }
    public required SafePhysicalMonitorHandle Handle { get; set; }

    public string? Adapter { get; set; }
    public string? SerialNumber { get; set; }
}
