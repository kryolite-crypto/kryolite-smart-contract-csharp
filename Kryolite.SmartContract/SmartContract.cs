namespace Kryolite.SmartContract;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class SmartContract() : Attribute
{
    public string Name { get; set; } = string.Empty;
    public string? Url { get; set; }
    public ApiLevel ApiLevel { get; set; } = ApiLevel.V1;
}

public enum ApiLevel
{
    V1
}