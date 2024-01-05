using System.Runtime.InteropServices;

namespace Kryolite.SmartContract;

public static class Transaction
{
    public static ulong Value { get; private set; }
    public static Address From { get; private set; } = Address.NULL_ADDRESS;

    [UnmanagedCallersOnly(EntryPoint = "set_transaction")]
    public static unsafe void SetTransaction(byte* fromPtr, int fromLen, ulong value)
    {
        Value = value;
        From = new Address(fromPtr, fromLen);
    }
}
