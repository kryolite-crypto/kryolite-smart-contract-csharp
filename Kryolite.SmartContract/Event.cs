using System.Text;

namespace Kryolite.SmartContract;

public static class Event
{
    public static unsafe void Broadcast<T>(T ev, params object[] values)
    {
        var bytes = Encoding.UTF8.GetBytes(nameof(ev));

        fixed (byte* ptr = bytes)
        {
            API.__append_event(ptr, bytes.Length);
        }

        foreach (var value in values)
        {
            var valBytes = Encoding.UTF8.GetBytes(value.ToString() ?? string.Empty);

            fixed (byte* ptr = valBytes)
            {
                API.__append_event(ptr, valBytes.Length);
            }
        }

        API.__publish_event();
    }
}
