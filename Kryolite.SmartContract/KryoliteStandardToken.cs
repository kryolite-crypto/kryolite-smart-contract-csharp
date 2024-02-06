using System.Text;

namespace Kryolite.SmartContract;

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
