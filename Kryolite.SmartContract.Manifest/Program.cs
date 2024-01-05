using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kryolite.SmartContract.Manifest;

internal class Program
{
    private static void Main(string[] args)
    {
        var path = args[0];
        
        AppDomain.CurrentDomain.AssemblyResolve += (object? sender, ResolveEventArgs arg) =>
        {
            var fullPath = Path.Join(Path.GetDirectoryName(path), arg.Name.Split(',').First() + ".dll");
            return Assembly.LoadFile(Path.GetFullPath(fullPath));
        };

        var assembly = Assembly.LoadFile(Path.GetFullPath(path));
        var manifest = new ContractManifest();

        foreach (var type in assembly.GetExportedTypes())
        {
            var smartContract = type.CustomAttributes.SingleOrDefault(x => x.AttributeType.Name == "SmartContract");

            if (smartContract is null)
            {
                continue;
            }

            manifest.Name = (string)smartContract.NamedArguments.First(x => x.MemberName == "Name").TypedValue.Value!;
            manifest.ApiLevel = (int)smartContract.NamedArguments.First(x => x.MemberName == "ApiLevel").TypedValue.Value!;

            foreach (var method in type.GetMethods())
            {
                var attr = method.CustomAttributes.FirstOrDefault(x => x.AttributeType == typeof(UnmanagedCallersOnlyAttribute));

                if (attr is null)
                {
                    continue;
                }

                var contractMethod = new ContractMethod
                {
                    Name = (string)attr.NamedArguments.First(x => x.MemberName == "EntryPoint").TypedValue.Value!
                };

                Console.WriteLine(contractMethod.Name);

                foreach (var param in method.GetParameters())
                {
                    var contractParam = new ContractParam
                    {
                        Name = param.Name!,
                        Type = param.ParameterType.Name
                    };

                    contractMethod.Params.Add(contractParam);
                }

                manifest.Methods.Add(contractMethod);
            }
        }

        var manifestPath = Path.Join(Path.GetDirectoryName(path), "publish", "manifest.json");
        Console.WriteLine(Path.GetFullPath(manifestPath));
        File.WriteAllText(Path.GetFullPath(manifestPath), JsonSerializer.Serialize(manifest));
    }
}

public class ContractManifest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    [JsonPropertyName("api_level")]
    public int ApiLevel { get; set; }
    [JsonPropertyName("methods")]
    public List<ContractMethod> Methods { get; set; } = [];
}

public class ContractMethod
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    [JsonPropertyName("method_params")]
    public List<ContractParam> Params { get; set; } = [];
}

public class ContractParam
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    [JsonPropertyName("param_type")]
    public string Type { get; set; } = string.Empty;
}