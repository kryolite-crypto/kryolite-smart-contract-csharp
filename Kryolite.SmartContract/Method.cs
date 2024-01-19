namespace Kryolite.SmartContract;

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class Method() : Attribute
{
    public string Name { get; set; } = string.Empty;
}
