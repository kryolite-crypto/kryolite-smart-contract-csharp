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

        if (receiver.Install is null)
        {
            throw new Exception("[Install] declaration not found");
        }

        if (receiver.Uninstall is null)
        {
            throw new Exception("[Uninstall] declaration not found");
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

        var installModel = context.Compilation.GetSemanticModel(receiver.Install.SyntaxTree);
        var installSymbol = (IMethodSymbol)installModel.GetDeclaredSymbol(receiver.Install)!;

        var uninstallModel = context.Compilation.GetSemanticModel(receiver.Uninstall.SyntaxTree);
        var uninstallSymbol = (IMethodSymbol)installModel.GetDeclaredSymbol(receiver.Uninstall)!;

        var source = GenerateExports(context, symbols.First().ContainingType, symbols, installSymbol, uninstallSymbol);
        context.AddSource("ContractExports_g.cs", SourceText.From(source, Encoding.UTF8));

        #pragma warning disable RS1035
        if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.projectdir", out var projectDir))
        {
            File.WriteAllText(Path.Combine(projectDir, "bin", "Release", "net8.0", "wasi-wasm", "publish", $"{classSymbol.Name.ToLower()}.json"), JsonSerializer.Serialize(Manifest));
        }
        #pragma warning restore RS1035
    }

    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
    }

    private string GenerateExports(GeneratorExecutionContext context, INamedTypeSymbol classSymbol, List<IMethodSymbol> methods, IMethodSymbol installSymbol, IMethodSymbol uninstallSymbol)
    {
        var source = new StringBuilder($@"
            using System.Threading.Tasks;
            using System.Runtime.InteropServices;
            using Kryolite.SmartContract;
            using System.Text;

            namespace {classSymbol.ContainingNamespace.ToDisplayString()};
            
            public static class Exports
            {{
                private static readonly {classSymbol.Name} Instance = new();

                [UnmanagedCallersOnly(EntryPoint = ""__malloc"")]
                public static unsafe byte* __malloc(int len)
                {{
                    return (byte*)Marshal.AllocHGlobal(len);
                }}

                [UnmanagedCallersOnly(EntryPoint = ""__free"")]
                public static unsafe void __free(byte* ptr, int len)
                {{
                    Marshal.FreeHGlobal((nint)ptr);
                }}

                [UnmanagedCallersOnly(EntryPoint = ""__install"")]
                public static unsafe void __install()
                {{
                    Instance.{installSymbol.Name}();
                }}

                [UnmanagedCallersOnly(EntryPoint = ""__uninstall"")]
                public static unsafe void __uninstall()
                {{
                    Instance.{uninstallSymbol.Name}();
                }}

                [UnmanagedCallersOnly(EntryPoint = ""set_transaction"")]
                public static unsafe void SetTransaction(byte* fromPtr, int fromLen, long value)
                {{
                    Transaction.Value = value;
                    Transaction.From = new Address(fromPtr, fromLen);
                }}

                [UnmanagedCallersOnly(EntryPoint = ""set_view"")]
                public static unsafe void SetView(long height, long timestamp)
                {{
                    View.Height = height;
                    View.Timestamp = timestamp;
                }}

                [UnmanagedCallersOnly(EntryPoint = ""set_contract"")]
                public static unsafe void SetContract(byte* addrPtr, int addrLen, byte* ownerPtr, int ownerLen, long balance)
                {{
                    Contract.Address = new Address(addrPtr, addrLen);
                    Contract.Owner = new Address(ownerPtr, ownerLen);
                    Contract.Balance = balance;
                }}
            ");

        foreach (var method in methods)
        {
            var methodManifest = new ContractMethod
            {
                Name = method.Name,
                Description = method.GetAttributes()
                    .DefaultIfEmpty()
                    .Where(x => x?.AttributeClass?.Name == "Description")
                    .FirstOrDefault()?.ConstructorArguments.FirstOrDefault().Value?.ToString(),
                IsReadOnly = (bool)(method.GetAttributes()
                    .DefaultIfEmpty()
                    .Where(x => x?.AttributeClass?.Name == "Method")
                    .FirstOrDefault()?.NamedArguments.Where(x => x.Key == "ReadOnly").Select(x => x.Value.Value).FirstOrDefault() ?? false)
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
                    Type = parameter.Type.ToDisplayString().Split(".").Last(),
                    Description = parameter.GetAttributes()
                        .DefaultIfEmpty()
                        .Where(x => x?.AttributeClass?.Name == "Description")
                        .FirstOrDefault()?.ConstructorArguments.FirstOrDefault().Value?.ToString()
                };

                methodManifest.Params.Add(parameterManifest);

                switch (parameter.Type.ToDisplayString())
                {
                    case "string":
                        parameters.Add($"byte* __ptr_{num}");
                        parameters.Add($"int __len_{num}");

                        statements.Add($"var span_{num} = new Span<byte>(__ptr_{num}, __len_{num});");
                        statements.Add($"var __val_{num} = Encoding.UTF8.GetString(span_{num});");
                        break;
                    case "byte[]":
                        parameters.Add($"byte* __ptr_{num}");
                        parameters.Add($"int __len_{num}");

                        statements.Add($"var __val_{num} = new Span<byte>(__ptr_{num}, __len_{num}).ToArray();");
                        break;
                    case "System.ReadOnlySpan<byte>":
                        parameters.Add($"byte* __ptr_{num}");
                        parameters.Add($"int __len_{num}");

                        statements.Add($"var __val_{num} = new Span<byte>(__ptr_{num}, __len_{num});");
                        break;
                    case "Kryolite.SmartContract.Address":
                        parameters.Add($"byte* __ptr_{num}");
                        parameters.Add($"int __len_{num}");

                        statements.Add($"var __val_{num} = new Address(__ptr_{num}, __len_{num});");
                        break;
                    case "Kryolite.SmartContract.U256":
                        parameters.Add($"byte* __ptr_{num}");
                        parameters.Add($"int __len_{num}");

                        statements.Add($"var __val_{num} = new U256(__ptr_{num}, __len_{num});");
                        break;
                    default:
                        parameters.Add($"{parameter.Type.ToDisplayString()} __val_{num}");
                        break;
                }

                toPass.Add($"__val_{num}");
                num++;
            }

            var methodCall = $"Instance.{method.Name}({string.Join(", ", toPass)})";

            switch (method.ReturnType.ToDisplayString())
            {
                case "void":
                    statements.Add($"{methodCall};");
                    break;
                default:
                    statements.Add($"Program.Return({methodCall});");
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

        source.AppendLine("}");
        return source.ToString();
    }
}

class SyntaxReceiver : ISyntaxReceiver
{
    public ClassDeclarationSyntax? ContractDeclaration;
    public List<MethodDeclarationSyntax> Methods { get; } = [];
    public MethodDeclarationSyntax? Install;
    public MethodDeclarationSyntax? Uninstall;

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

        if (syntaxNode is MethodDeclarationSyntax syntax)
        {
            if (IsExportedMethod(syntax))
            {
                Methods.Add(syntax);
            }
            else if (IsInstall(syntax))
            {
                if (Install is not null)
                {
                    throw new Exception("Multiple [Install] declarations found");
                }

                Install = syntax;
            }
            else if (IsUninstall(syntax))
            {
                if (Uninstall is not null)
                {
                    throw new Exception("Multiple [Uninstall] declarations found");
                }

                Uninstall = syntax;
            }
        }
    }

    public static bool IsSmartContract(ClassDeclarationSyntax syntax)
        => syntax.AttributeLists.Count > 0 && syntax.AttributeLists.Any(x => x.Attributes.Any(y => y.Name.ToString() == "SmartContract"));

    public static bool IsExportedMethod(MethodDeclarationSyntax syntax)
        => syntax.AttributeLists.Count > 0 && syntax.AttributeLists.Any(x => x.Attributes.Any(y => y.Name.ToString() == "Method"));

    public static bool IsInstall(MethodDeclarationSyntax syntax)
        => syntax.AttributeLists.Count > 0 && syntax.AttributeLists.Any(x => x.Attributes.Any(y => y.Name.ToString() == "Install"));

    public static bool IsUninstall(MethodDeclarationSyntax syntax)
        => syntax.AttributeLists.Count > 0 && syntax.AttributeLists.Any(x => x.Attributes.Any(y => y.Name.ToString() == "Uninstall"));
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
    [JsonPropertyName("readonly")]
    public bool IsReadOnly { get; set; }
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
