using System.Runtime.InteropServices;

namespace Kryolite.SmartContract;

public static class Transaction
{
    public static ulong Value { get; set; }
    public static Address From { get; set; } = Address.NULL_ADDRESS;
}
