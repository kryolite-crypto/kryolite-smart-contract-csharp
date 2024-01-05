using System.Runtime.InteropServices;
using System.Text;

namespace Kryolite.SmartContract;

public static class KryoliteStandardToken
{
    private static IKryoliteStandardToken? Instance;

    public static void Register(IKryoliteStandardToken instance)
    {
        Instance = instance;
    }

    [UnmanagedCallersOnly(EntryPoint = "get_token")]
    public unsafe static void GetToken(byte* tokenPtr, int len)
    {
        if (Instance is null)
        {
            Program.Exit(313);
            return;
        }

        var token = Instance.GetToken(new U256(tokenPtr, len));
        Program.Return(token.ToJson());
    }
}

public interface IKryoliteStandardToken
{
    public StandardToken GetToken(U256 tokenId);
}

public class StandardToken
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;

    public string ToJson()
    {
        var sb = new StringBuilder();

        sb.AppendLine("{");
        sb.AppendFormat("\"name\": \"{0}\"", Name);
        sb.AppendLine(",");
        sb.AppendFormat("\"description\": \"{0}\"", Description);
        sb.AppendLine("}");

        return sb.ToString();
    }
}
