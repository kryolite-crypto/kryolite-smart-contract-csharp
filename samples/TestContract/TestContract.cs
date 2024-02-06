using System.Runtime.InteropServices;
using System.Text;
using Kryolite.SmartContract;

namespace TestContract;

[SmartContract(Name = "Test Contract", Url = "", ApiLevel = ApiLevel.V1)]
public class TestContract
{
    private int Counter = 0;

    [Install]
    public void InstallContract()
    {
        Contract.Scheduler(Increment)
            .At(DateTimeOffset.FromUnixTimeMilliseconds(View.Timestamp).AddMinutes(15))
            .Save();
    }

    [Uninstall]
    public void UninstallContract()
    {
        Contract.Owner.Transfer(Contract.Balance);
    }

    [Method]
    [Description("Increment")]
    public void Increment()
    {
        Counter++;

        Contract.Scheduler(Increment)
            .At(DateTimeOffset.FromUnixTimeMilliseconds(View.Timestamp).AddMinutes(15))
            .Save();
    }

    [Method(ReadOnly = true)]
    [Description("Byte Add")]
    public byte ByteAdd(byte a, byte b)
    {
        return (byte)(a+b);
    }

    [Method(ReadOnly = true)]
    [Description("Short Add")]
    public short ShortAdd(short a, short b)
    {
        return (short)(a+b);
    }

    [Method(ReadOnly = true)]
    [Description("Int Add")]
    public int IntAdd(int a, int b)
    {
        return a+b;
    }

    [Method(ReadOnly = true)]
    [Description("Long Add")]
    public long LongAdd(long a, long b)
    {
        return a+b;
    }

    [Method(ReadOnly = true)]
    [Description("Float Add")]
    public float FloatAdd(float a, float b)
    {
        return a+b;
    }

    [Method(ReadOnly = true)]
    [Description("Double Add")]
    public double DoubleAdd(double a, double b)
    {
        return a+b;
    }

    [Method(ReadOnly = true)]
    [Description("String Add")]
    public string StringAdd(string a, string b)
    {
        return a+b;
    }

    [Method(ReadOnly = true)]
    [Description("ByteArray Concat")]
    public byte[] ByteArrayAdd(byte[] a, byte[] b)
    {
        var c = new byte[a.Length + b.Length];
        a.CopyTo(c, 0);
        b.CopyTo(c, a.Length);
        return c;
    }

    [Method(ReadOnly = true)]
    [Description("ByteReadOnlySpan Concat")]
    public ReadOnlySpan<byte> ByteSpanAdd(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        Span<byte> c = stackalloc byte[a.Length + b.Length];
        a.CopyTo(c);
        b.CopyTo(c.Slice(a.Length));
        return c.ToArray();
    }

    [Method(ReadOnly = true)]
    [Description("Address")]
    public Address AddressRet(Address addr)
    {
        return addr;
    }

    [Method(ReadOnly = true)]
    [Description("U256")]
    public U256 U256Ret(U256 u256)
    {
        return u256;
    }

    [Method(ReadOnly = true)]
    [Description("Get Count")]
    public int GetCount()
    {
        return Counter;
    }
}
