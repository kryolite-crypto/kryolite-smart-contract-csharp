namespace Kryolite.SmartContract;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter, Inherited = false)]
public sealed class Description(string value) : Attribute
{
    public string Value { get; set; } = value;
}