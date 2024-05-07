using System.Text;

namespace Kryolite.SmartContract;

public class KRC721Event
{
    public unsafe static void Transfer(Address from, Address to, U256 tokenId, string name, string description)
    {
        var nameBytes = Encoding.UTF8.GetBytes(name);
        var descBytes = Encoding.UTF8.GetBytes(description);

        fixed (byte* fromPtr = from.Buffer)
        fixed (byte* toPtr = to.Buffer)
        fixed (byte* tokenPtr = tokenId.Buffer)
        fixed (byte* namePtr = nameBytes)
        fixed (byte* descPtr = descBytes)
        {
            API.__transfer_token(fromPtr, toPtr, tokenPtr, namePtr, name.Length, descPtr, description.Length);
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
