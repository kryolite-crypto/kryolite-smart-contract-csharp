using System.Text;

namespace Kryolite.SmartContract;

public static class Program
{
    public static unsafe void Print(string str)
    {
        var bytes = Encoding.UTF8.GetBytes(str);
        fixed (byte* ptr = bytes)
        {
            API.__println(ptr, bytes.Length);
        }
    }

    public static float Rand()
    {
        return API.__rand();
    }

    public static unsafe void Return(string str)
    {
        var bytes = Encoding.UTF8.GetBytes(str);
        fixed (byte* ptr = bytes)
        {
            API.__return(ptr, bytes.Length);
        }
    }

    public unsafe static void ReturnUTF8Bytes(ReadOnlySpan<byte> bytes)
    {
        fixed (byte* ptr = bytes)
        {
            API.__return(ptr, bytes.Length);
        }
    }

    public static void Return(ReadOnlySpan<byte> bytes)
    {
        Return(Convert.ToBase64String(bytes));
    }

    public static void Return(Address addr)
    {
        Return(addr.ToString());
    }

    public static void Return(U256 u256)
    {
        Return(u256.ToString());
    }

    public static void Return(byte b)
    {
        Return(b.ToString());
    }

    public static void Return(short s)
    {
        Return(s.ToString());
    }

    public static void Return(ushort s)
    {
        Return(s.ToString());
    }
    
    public static void Return(int i)
    {
        Return(i.ToString());
    }

    public static void Return(uint i)
    {
        Return(i.ToString());
    }

    public static void Return(long l)
    {
        Return(l.ToString());
    }

    public static void Return(ulong l)
    {
        Return(l.ToString());
    }

    public static void Return(float f)
    {
        Return(f.ToString());
    }

    public static void Return(double d)
    {
        Return(d.ToString());
    }

    public static void Return(bool b)
    {
        Return(b.ToString());
    }

    public static void Exit(int exitCode)
    {
        API.__exit(exitCode);
    }

    public static unsafe U256 HashData(byte[] bytes)
    {
        var dest = new byte[32];

        fixed (byte* ptr = bytes)
        fixed (byte* destPtr = dest)
        {
            API.__hash_data(ptr, bytes.Length, destPtr, dest.Length);
        }

        return new U256(dest);
    }
}
