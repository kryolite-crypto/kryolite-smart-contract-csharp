using System.Runtime.InteropServices;

namespace Kryolite.SmartContract;

public static class Contract
{
    public static Address Address { get; private set; } = Address.NULL_ADDRESS;
    public static Address Owner { get; private set; } = Address.NULL_ADDRESS;
    public static ulong Balance { get; private set; }

    [UnmanagedCallersOnly(EntryPoint = "set_contract")]
    public static unsafe void SetContract(byte* addrPtr, int addrLen, byte* ownerPtr, int ownerLen, ulong balance)
    {
        Address = new Address(addrPtr, addrLen);
        Owner = new Address(ownerPtr, ownerLen);
        Balance = balance;
    }
}
