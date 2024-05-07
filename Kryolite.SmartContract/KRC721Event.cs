namespace Kryolite.SmartContract;

public class KRC721Event
{
    public unsafe static void Transfer(Address from, Address to, U256 tokenId, string name, string description)
    {
        fixed (byte* fromPtr = from.Buffer)
        fixed (byte* toPtr = to.Buffer)
        fixed (byte* tokenPtr = tokenId.Buffer)
        fixed (char* namePtr = name)
        fixed (char* descPtr = description)
        {
            API.__transfer_token(fromPtr, toPtr, tokenPtr, (byte*)namePtr, name.Length, (byte*)descPtr, description.Length);
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
