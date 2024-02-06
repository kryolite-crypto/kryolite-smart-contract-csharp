namespace Kryolite.SmartContract;

public static class KRC721
{
    private static IKRC721? Instance;

    public static void Register(IKRC721 instance)
    {
        Instance = instance;
    }

    public static void BalanceOf(Address owner)
    {
        if (Instance is null)
        {
            Program.Exit(313);
            return;
        }

        Program.Return(Instance.BalanceOf(owner).ToString());
    }

    public static void OwnerOf(U256 tokenId)
    {
        if (Instance is null)
        {
            Program.Exit(313);
            return;
        }

        Program.Return(Instance.OwnerOf(tokenId).ToString());
    }

    public static void GetApproved(U256 tokenId)
    {
        if (Instance is null)
        {
            Program.Exit(313);
            return;
        }

        Program.Return(Instance.GetApproved(tokenId).ToString());
    }

    public static void Approve(Address to, U256 tokenId)
    {
        if (Instance is null)
        {
            Program.Exit(313);
            return;
        }

        Instance.Approve(to, tokenId);
    }

    public static void TransferFrom(Address from, Address to, U256 tokenId, byte[] data)
    {
        if (Instance is null)
        {
            Program.Exit(313);
            return;
        }

        Instance.TransferFrom(from, to, tokenId, data);
    }
}

public interface IKRC721
{
    public abstract long BalanceOf(Address owner);
    public abstract Address OwnerOf(U256 tokenId);
    public abstract void Approve(Address to, U256 tokenId);
    public abstract Address GetApproved(U256 tokenId);
    public abstract void TransferFrom(Address from, Address to, U256 tokenId, byte[] data);    
}
