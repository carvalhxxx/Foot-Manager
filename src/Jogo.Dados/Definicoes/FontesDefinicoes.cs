namespace Jogo.Dados.Definicoes;

/// <summary>
/// De onde vêm os arquivos de definições. Permite ler do disco (ferramentas, testes)
/// ou da memória (navegador: os arquivos chegam por download ou upload de patch).
/// Caminhos são sempre relativos e com "/" (ex.: "clubes/pampa.json").
/// </summary>
public interface IFonteDefinicoes
{
    /// <summary>Conteúdo do arquivo, ou nulo se ele não existir.</summary>
    string? LerTexto(string caminhoRelativo);

    /// <summary>Verdadeiro se a pasta de clubes existe na fonte.</summary>
    bool TemPastaDeClubes { get; }

    /// <summary>Caminhos relativos dos arquivos .json de clubes, em qualquer ordem.</summary>
    IReadOnlyList<string> ArquivosDeClubes();
}

/// <summary>Lê definições de uma pasta no disco.</summary>
public sealed class FontePasta : IFonteDefinicoes
{
    private readonly string _pasta;

    public FontePasta(string pasta)
    {
        if (!Directory.Exists(pasta))
        {
            throw new DirectoryNotFoundException($"Pasta de definições não encontrada: {pasta}");
        }

        _pasta = pasta;
    }

    public bool TemPastaDeClubes => Directory.Exists(PastaClubes);

    private string PastaClubes => Path.Combine(_pasta, FormatoDefinicoes.PastaClubes);

    public string? LerTexto(string caminhoRelativo)
    {
        var caminho = Path.Combine(_pasta, caminhoRelativo);
        return File.Exists(caminho) ? File.ReadAllText(caminho) : null;
    }

    public IReadOnlyList<string> ArquivosDeClubes() =>
        TemPastaDeClubes
            ? [.. Directory.GetFiles(PastaClubes, "*.json").Select(f => $"{FormatoDefinicoes.PastaClubes}/{Path.GetFileName(f)}")]
            : [];
}

/// <summary>Lê definições já carregadas em memória (caminho relativo → conteúdo).</summary>
public sealed class FonteMemoria(IReadOnlyDictionary<string, string> arquivos) : IFonteDefinicoes
{
    private const string PrefixoClubes = FormatoDefinicoes.PastaClubes + "/";

    public bool TemPastaDeClubes => arquivos.Keys.Any(EhArquivoDeClube);

    public string? LerTexto(string caminhoRelativo) =>
        arquivos.TryGetValue(caminhoRelativo, out var conteudo) ? conteudo : null;

    public IReadOnlyList<string> ArquivosDeClubes() => [.. arquivos.Keys.Where(EhArquivoDeClube)];

    private static bool EhArquivoDeClube(string caminho) =>
        caminho.StartsWith(PrefixoClubes, StringComparison.Ordinal)
        && caminho.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
        && !caminho[PrefixoClubes.Length..].Contains('/', StringComparison.Ordinal);
}
