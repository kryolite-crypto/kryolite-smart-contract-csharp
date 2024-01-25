using System.Runtime.InteropServices;
using System.Text;

namespace Kryolite.SmartContract;

public static class Contract
{
    public static Address Address { get; set; } = Address.NULL_ADDRESS;
    public static Address Owner { get; set; } = Address.NULL_ADDRESS;
    public static ulong Balance { get; set; }

    public static SchedulePlanner Scheduler(Action method)
    {
        return new SchedulePlanner(method.Method.Name);
    }

    public static SchedulePlanner Scheduler(Action method, params object[] methodParams)
    {
        return new SchedulePlanner(method.Method.Name, methodParams);
    }
}

public class SchedulePlanner
{
    private string MethodName;
    private object[] MethodParams = Array.Empty<object>();
    private DateTimeOffset ScheduledTime = DateTimeOffset.FromUnixTimeMilliseconds(View.Timestamp).Date;

    public SchedulePlanner(string methodName)
    {
        MethodName = methodName;
    }

    public SchedulePlanner(string methodName, object[] methodParams)
    {
        MethodName = methodName;
        MethodParams = methodParams;
    }

    public SchedulePlanner DayOfWeek(DayOfWeek dayOfWeek)
    {
        ScheduledTime = ScheduledTime.AddDays(-(int)ScheduledTime.DayOfWeek + (int)dayOfWeek);
        return this;
    }

    public SchedulePlanner At(int hour, int minute)
    {
        ScheduledTime = ScheduledTime
            .AddHours(hour)
            .AddMinutes(minute);

        return this;
    }

    public SchedulePlanner At(DateTimeOffset dateTime)
    {
        ScheduledTime = dateTime;
        return this;
    }

    public unsafe void Save()
    {
        foreach (var param in MethodParams)
        {
            var str = param switch {
                string s => s,
                byte[] a => Convert.ToBase64String(a),
                byte b => b.ToString(),
                short s => s.ToString(),
                ushort us => us.ToString(),
                int i => i.ToString(),
                uint ui => ui.ToString(),
                long l => l.ToString(),
                ulong ul => ul.ToString(),
                float f => f.ToString(),
                double d => d.ToString(),
                bool b => b.ToString(),
                Address addr => addr.ToString(),
                U256 u256 => u256.ToString(),
                _ => null
            };

            var bytes = Encoding.UTF8.GetBytes(str!);
            fixed (byte* ptr = bytes)
            {
                API.__schedule_param(ptr, bytes.Length);
            }
        }

        var bytes2 = Encoding.UTF8.GetBytes(MethodName);
        fixed (byte* ptr = bytes2)
        {
            API.__schedule(ptr, bytes2.Length, ScheduledTime.ToUnixTimeMilliseconds());
        }
    }
}
