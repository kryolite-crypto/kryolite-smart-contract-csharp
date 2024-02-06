namespace Kryolite.SmartContract;

public class Address
{
    public readonly byte[] Buffer;

    public Address(byte[] buffer)
    {
        Assert.True(buffer.Length == ADDRESS_SZ);

        Buffer = buffer;
    }

    public unsafe Address(byte* ptr, int len)
    {
        Assert.True(len == ADDRESS_SZ);

        Buffer = new byte[ADDRESS_SZ];
        
        for (var i = 0; i < Buffer.Length; i++)
        {
            Buffer[i] = ptr[i];
        }
    }

    public unsafe void Transfer(long value)
    {
        fixed (byte* ptr = Buffer)
        {
            API.__transfer(ptr, value);
        }
    }

    public override string ToString()
    {
        return "kryo:" + Base32.Kryolite.Encode(Buffer);
    }

    public override bool Equals(object? obj) 
    {
        return obj is Address c && Enumerable.SequenceEqual(this.Buffer, c.Buffer);
    }

    public static bool operator ==(Address? a, Address? b)
    {
        if (a is null || b is null)
        {
            return false;
        }

        return a.Equals(b);
    }

    public static bool operator !=(Address a, Address b)
    {
        return !(a == b);
    }

    public override int GetHashCode()
    {
        int hash = 17;
        foreach (var b in Buffer)
        {
            hash = hash * 31 + b.GetHashCode();
        }
        return hash;
    }

    public static Address NULL_ADDRESS { get; } = new Address(new byte[ADDRESS_SZ]);
    public const int ADDRESS_SZ = 25;
}
