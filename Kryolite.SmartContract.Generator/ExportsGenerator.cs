using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kryolite.SmartContract.Generator;

[Generator]
public class ExportsGenerator : ISourceGenerator
{
    private ContractManifest Manifest = new();

    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxReceiver is not SyntaxReceiver receiver)
        {
            return;
        }

        if (receiver.ContractDeclaration is null)
        {
            throw new Exception("[SmartContract] declaration not found");
        }

        var classModel = context.Compilation.GetSemanticModel(receiver.ContractDeclaration.SyntaxTree);
        var classSymbol = classModel.GetDeclaredSymbol(receiver.ContractDeclaration);

        if (classSymbol is null)
        {
            throw new Exception("class declaration not found");
        }

        var contract = classSymbol.GetAttributes().Where(x => x.AttributeClass?.Name == "SmartContract").First();

        Manifest.Name = (string)contract.NamedArguments.Where(x => x.Key == "Name").Select(x => x.Value).First().Value!;
        Manifest.Url = (string?)contract.NamedArguments.Where(x => x.Key == "Url").Select(x => x.Value).First().Value!;
        Manifest.ApiLevel = (int)contract.NamedArguments.Where(x => x.Key == "ApiLevel").Select(x => x.Value).First().Value!;

        var symbols = new List<IMethodSymbol>();
    
        foreach (var method in receiver.Methods)
        {
            var model = context.Compilation.GetSemanticModel(method.SyntaxTree);
            var symbol = model.GetDeclaredSymbol(method);

            if (symbol is null)
            {
                continue;
            }
        
            if (symbol.GetAttributes().Any(ad => ad?.AttributeClass?.Name == "Method"))
            {
                symbols.Add((IMethodSymbol)symbol);
            }
        }

        var source = GenerateExports(symbols.First().ContainingType, symbols);
        context.AddSource("ContractExports_g.cs", SourceText.From(source, Encoding.UTF8));
    }

    public void Initialize(GeneratorInitializationContext context)
    {
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
    }

    private string GenerateExports(INamedTypeSymbol classSymbol, List<IMethodSymbol> methods)
    {
        var source = new StringBuilder($@"
            using System.Threading.Tasks;
            using System.Runtime.InteropServices;
            using Kryolite.SmartContract;

            namespace {classSymbol.ContainingNamespace.ToDisplayString()};
            
            public static class Exports
            {{
            ");

        foreach (var method in methods)
        {
            var methodManifest = new ContractMethod
            {
                Name = method.Name,
                Description = method.GetAttributes().Where(x => x.AttributeClass?.Name == "Description").First()
                    .ConstructorArguments.First().Value?.ToString()
            };

            Manifest.Methods.Add(methodManifest);

            var parameters = new List<string>();
            var statements = new List<string>();
            var toPass = new List<string>();

            var num = 0;

            foreach (var parameter in method.Parameters)
            {
                var parameterManifest = new ContractParam
                {
                    Name = parameter.Name,
                    Type = parameter.Type.Name,
                    Description = method.GetAttributes().Where(x => x.AttributeClass?.Name == "Description").First()
                        .ConstructorArguments.First().Value?.ToString()
                };

                methodManifest.Params.Add(parameterManifest);

                switch (parameter.Type.Name)
                {
                    case "String":
                        parameters.Add($"byte* __ptr_{num}");
                        parameters.Add($"int __len_{num}");

                        statements.Add($"var span = new Span<byte>(__ptr_{num}, __len_{num});");
                        statements.Add($"var __val_{num} = Encoding.UTF8.GetString(span);");
                        break;
                    case "byte[]":
                    case "ReadOnlySpan<byte>":
                        parameters.Add($"byte* __ptr_{num}");
                        parameters.Add($"int __len_{num}");

                        statements.Add($"var __val_{num} = new Span<byte>(__ptr_{num}, __len_{num}).ToArray();");
                        break;
                    case "Address":
                        parameters.Add($"byte* __ptr_{num}");
                        parameters.Add($"int __len");

                        statements.Add($"var __val_{num} = new Address(__ptr_{num}, __len_{num})");
                        break;
                    case "U256":
                        parameters.Add($"byte* __ptr_{num}");
                        parameters.Add($"int __len");

                        statements.Add($"var __val_{num} = new U256(__ptr_{num}, __len_{num})");
                        break;
                    default:
                        parameters.Add($"{parameter.Type.Name} __val_{num}");
                        break;
                }

                toPass.Add($"__val_{num}");
                num++;
            }

            var methodCall = $"{classSymbol.Name}.{method.Name}({string.Join(", ", toPass)})";

            switch (method.ReturnType.Name)
            {
                case "String":
                case "byte[]":
                case "ReadOnlySpan<byte>":
                case "Address":
                case "U256":
                    statements.Add($"Program.Return({methodCall});");
                    break;
                default:
                    statements.Add($"{methodCall};");
                    break;
            }

            source.Append($@"
            [UnmanagedCallersOnly(EntryPoint = ""{method.Name}"")]
            public static unsafe void {method.Name}({string.Join(", ", parameters)})
            {{
            ");

            foreach (var statement in statements)
            {
                source.AppendLine(statement);
            }

            source.AppendLine("}");
        }

        source.Append($@"
        [UnmanagedCallersOnly(EntryPoint = ""GetManifest"")]
        public static void GetManifest()
        {{
            return @""{JsonSerializer.Serialize(Manifest)}"";
        }}
        ");

        source.AppendLine("}");
        return source.ToString();
    }
}

class SyntaxReceiver : ISyntaxReceiver
{
    public ClassDeclarationSyntax? ContractDeclaration;
    public List<MethodDeclarationSyntax> Methods { get; } = [];

    public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
    {
        if (syntaxNode is ClassDeclarationSyntax cSyntax && IsSmartContract(cSyntax))
        {
            if (ContractDeclaration is not null)
            {
                throw new Exception("multiple [SmartContract] declarations");
            }

            ContractDeclaration = cSyntax;
        }

        if (syntaxNode is MethodDeclarationSyntax syntax && IsExportedMethod(syntax))
        {
            Methods.Add(syntax);
        }
    }

    public static bool IsSmartContract(ClassDeclarationSyntax syntax)
        => syntax.AttributeLists.Count > 0 && syntax.AttributeLists.Any(x => x.Attributes.Any(y => y.Name.ToString() == "SmartContract"));

    public static bool IsExportedMethod(MethodDeclarationSyntax syntax)
        => syntax.AttributeLists.Count > 0 && syntax.AttributeLists.Any(x => x.Attributes.Any(y => y.Name.ToString() == "Method"));
}

public class ContractManifest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
    [JsonPropertyName("api_level")]
    public int ApiLevel { get; set; }
    [JsonPropertyName("methods")]
    public List<ContractMethod> Methods { get; set; } = [];
}

public class ContractMethod
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    [JsonPropertyName("method_params")]
    public List<ContractParam> Params { get; set; } = [];
}

public class ContractParam
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    [JsonPropertyName("param_type")]
    public string Type { get; set; } = string.Empty;
}