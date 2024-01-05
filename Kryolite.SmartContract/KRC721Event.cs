namespace Kryolite.SmartContract;

public class KRC721Event
{
    public unsafe static void Transfer(Address from, Address to, U256 tokenId)
    {
        fixed (byte* fromPtr = from.Buffer)
        fixed (byte* toPtr = to.Buffer)
        fixed (byte* tokenPtr = tokenId.Buffer)
        {
            API.__transfer_token(fromPtr, toPtr, tokenPtr);
        }
    }

    public unsafe static void Consume(Address owner, U256 tokenId)
    {
        fixed (byte* ownerPtr = owner.Buffer)
        fixed (byte* tokenPtr = tokenId.Buffer)
        {
            API.__consume_token(ownerPtr, tokenPtr);
        }
    }
}
