namespace Kryolite.SmartContract;

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class Method : Attribute
{
    public bool ReadOnly { get; set; }
}
