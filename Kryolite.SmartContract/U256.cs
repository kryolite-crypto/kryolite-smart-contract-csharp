namespace Kryolite.SmartContract;

public class U256
{
    public readonly byte[] Buffer;

    public U256(byte[] buffer)
    {
        Assert.True(buffer.Length == 32);

        Buffer = buffer;
    }

    public unsafe U256(byte* ptr, int len)
    {
        Assert.True(len == 32);

        Buffer = new byte[32];
        
        for (var i = 0; i < Buffer.Length; i++)
        {
            Buffer[i] = ptr[i];
        }
    }

    public override string ToString()
    {
        return Base32.Kryolite.Encode(Buffer);
    }

    public override bool Equals(object? obj) 
    {
        return obj is U256 c && Enumerable.SequenceEqual(this.Buffer, c.Buffer);
    }

    public static bool operator ==(U256 a, U256 b)
    {
        if (a is null || (b is null))
        {
            return false;
        }

        return a.Equals(b);
    }

    public static bool operator !=(U256 a, U256 b)
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

    public static U256 NULL_U256 { get; } = new U256(new byte[32]);
}
