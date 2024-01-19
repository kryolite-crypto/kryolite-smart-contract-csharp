using System.Runtime.InteropServices;

namespace Kryolite.SmartContract;

public static class View
{
    public static ulong Height { get; private set; }
    public static ulong Timestamp { get; private set; }

    [UnmanagedCallersOnly(EntryPoint = "set_view")]
    public static unsafe void SetView(ulong height, ulong timestamp)
    {
        Height = height;
        Timestamp = timestamp;
    }
}
