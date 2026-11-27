using Jogo.Dados.Definicoes;
using Jogo.Dados.Reais;
using Jogo.Simulacao.Geracao;

namespace Jogo.Dados.Testes.Definicoes;

/// <summary>Garante que a pasta dados/definicoes do repositório é válida e está atualizada.</summary>
public class DadosBaseTestes
{
    private static readonly string PastaBase = Path.Combine(RaizDoRepositorio(), "dados", "definicoes");

    [Fact]
    public void DadosBase_CarregamSemErros()
    {
        var (manifesto, mundo) = LeitorDefinicoes.Carregar(PastaBase);

        Assert.Equal("Foot Manager BR · Brasileirão 2026", manifesto.Nome);
        Assert.Equal(40, mundo.Clubes.Count);
        Assert.Equal(2026, mundo.TemporadaAtual);
        Assert.Contains(mundo.Clubes, c => c.Nome == "Flamengo");
    }

    /// <summary>
    /// A base é gerada pela ferramenta Jogo.GeradorDados com a seed 2027. Se este teste falhar,
    /// o gerador mudou: rode <c>dotnet run --project tools/Jogo.GeradorDados</c> e commite os arquivos.
    /// </summary>
    [Fact]
    public void DadosBase_IguaisAoGeradorComSeed2027()
    {
        using var pasta = new PastaTemporaria();
        var reais = LeitorDadosReais.Carregar(Path.Combine(RaizDoRepositorio(), "tools", "Jogo.GeradorDados", "reais"));
        var mundo = GeradorDeMundoReal.Gerar(2027, 2026, reais);
        EscritorDefinicoes.Salvar(mundo, LeitorDefinicoes.Carregar(PastaBase).Manifesto, pasta.Caminho);

        var gerados = Directory.GetFiles(pasta.Caminho, "*.json", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(pasta.Caminho, f)).Order(StringComparer.Ordinal).ToArray();
        var commitados = Directory.GetFiles(PastaBase, "*.json", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(PastaBase, f)).Order(StringComparer.Ordinal).ToArray();

        Assert.Equal(gerados, commitados);
        foreach (var relativo in gerados)
        {
            Assert.True(
                Normalizar(File.ReadAllText(Path.Combine(pasta.Caminho, relativo))) == Normalizar(File.ReadAllText(Path.Combine(PastaBase, relativo))),
                $"{relativo} está desatualizado: rode a ferramenta Jogo.GeradorDados.");
        }
    }

    private static string Normalizar(string texto) => texto.Replace("\r\n", "\n", StringComparison.Ordinal);

    private static string RaizDoRepositorio()
    {
        var pasta = new DirectoryInfo(AppContext.BaseDirectory);
        while (pasta is not null && !File.Exists(Path.Combine(pasta.FullName, "FootManagerBR.slnx")))
        {
            pasta = pasta.Parent;
        }

        return pasta?.FullName ?? throw new InvalidOperationException("Raiz do repositório não encontrada.");
    }
}
