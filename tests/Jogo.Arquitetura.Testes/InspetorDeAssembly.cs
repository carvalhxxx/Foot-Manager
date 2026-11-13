using System.Reflection;
using Mono.Cecil;

namespace Jogo.Arquitetura.Testes;

/// <summary>
/// Lê o IL compilado de um assembly e lista o que ele referencia (assemblies, tipos e membros).
/// Usado para garantir as regras de camada e de determinismo sem depender de revisão manual.
/// </summary>
internal static class InspetorDeAssembly
{
    /// <summary>Tipos proibidos em código puro: I/O, rede, relógio e aleatoriedade fora do IGeradorAleatorio.</summary>
    private static readonly string[] TiposProibidos =
    [
        "System.Random",
        "System.Console",
        "System.Environment",
        "System.Diagnostics.Stopwatch",
        "System.Threading.Tasks.Parallel",
        "System.Threading.Timer",
        "System.Security.Cryptography.RandomNumberGenerator",
    ];

    private static readonly string[] NamespacesProibidos =
    [
        "System.IO",
        "System.Net",
        "System.Data",
    ];

    /// <summary>Membros que dependem do relógio ou de entropia externa.</summary>
    private static readonly string[] MembrosProibidos =
    [
        "System.DateTime::get_Now",
        "System.DateTime::get_UtcNow",
        "System.DateTime::get_Today",
        "System.DateTimeOffset::get_Now",
        "System.DateTimeOffset::get_UtcNow",
        "System.Guid::NewGuid",
        "System.TimeProvider::get_System",
    ];

    public static string Caminho(string nomeAssembly) =>
        Assembly.Load(new AssemblyName(nomeAssembly)).Location;

    public static IReadOnlyList<string> AssembliesReferenciados(string caminho)
    {
        using var modulo = ModuleDefinition.ReadModule(caminho);
        return [.. modulo.AssemblyReferences.Select(r => r.Name)];
    }

    /// <summary>Retorna as violações de pureza/determinismo encontradas no assembly.</summary>
    public static IReadOnlyList<string> ViolacoesDePureza(string caminho)
    {
        using var modulo = ModuleDefinition.ReadModule(caminho);
        var violacoes = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var tipo in modulo.GetTypeReferences())
        {
            var nome = NomeCompleto(tipo);
            if (TiposProibidos.Contains(nome) || NamespacesProibidos.Any(ns => EstaNoNamespace(tipo, ns)))
            {
                violacoes.Add($"tipo {nome}");
            }
        }

        foreach (var membro in modulo.GetMemberReferences())
        {
            var chave = $"{NomeCompleto(membro.DeclaringType.GetElementType())}::{membro.Name}";
            if (MembrosProibidos.Contains(chave))
            {
                violacoes.Add($"membro {chave}");
            }
        }

        return [.. violacoes];
    }

    private static string NomeCompleto(TypeReference tipo) =>
        tipo.IsNested ? $"{NomeCompleto(tipo.DeclaringType)}+{tipo.Name}" : tipo.FullName;

    private static bool EstaNoNamespace(TypeReference tipo, string ns)
    {
        while (tipo.IsNested)
        {
            tipo = tipo.DeclaringType;
        }

        return tipo.Namespace == ns || tipo.Namespace.StartsWith(ns + ".", StringComparison.Ordinal);
    }
}
