using System.Runtime.CompilerServices;
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

    public static unsafe void Return(ReadOnlySpan<byte> bytes)
    {
        fixed (byte* ptr = bytes)
        {
            API.__return(ptr, bytes.Length);
        }
    }

    public static unsafe void Return(Address addr)
    {
        var bytes = addr.Buffer;
        fixed (byte* ptr = bytes)
        {
            API.__return(ptr, bytes.Length);
        }
    }

    public static unsafe void Return(U256 u256)
    {
        var bytes = u256.Buffer;
        fixed (byte* ptr = bytes)
        {
            API.__return(ptr, bytes.Length);
        }
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
