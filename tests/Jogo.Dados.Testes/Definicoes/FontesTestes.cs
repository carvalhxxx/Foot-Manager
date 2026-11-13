using Jogo.Dados.Definicoes;

namespace Jogo.Dados.Testes.Definicoes;

public class FontesTestes
{
    private static readonly Manifesto ManifestoPadrao = new("Base de teste");

    [Fact]
    public void FonteMemoria_CarregaOMesmoMundoQueAPasta()
    {
        using var pasta = new PastaTemporaria();
        EscritorDefinicoes.Salvar(MundoDeExemplo.Criar(), ManifestoPadrao, pasta.Caminho);
        var arquivos = LerTudo(pasta.Caminho);

        var doDisco = LeitorDefinicoes.Carregar(pasta.Caminho);
        var daMemoria = LeitorDefinicoes.Carregar(new FonteMemoria(arquivos));

        Assert.Equal(doDisco.Manifesto, daMemoria.Manifesto);
        Assert.Equal(
            doDisco.Mundo.Clubes.SelectMany(c => c.Elenco).Select(j => (j.Id, j.Nome, j.Atributos)),
            daMemoria.Mundo.Clubes.SelectMany(c => c.Elenco).Select(j => (j.Id, j.Nome, j.Atributos)));
    }

    [Fact]
    public void FonteMemoria_IgnoraArquivosForaDaPastaDeClubes()
    {
        var fonte = new FonteMemoria(new Dictionary<string, string>
        {
            ["clubes/a.json"] = "{}",
            ["clubes/sub/b.json"] = "{}",
            ["clubes/leia-me.txt"] = "",
            ["outro/c.json"] = "{}",
        });

        Assert.Equal(["clubes/a.json"], fonte.ArquivosDeClubes());
        Assert.True(fonte.TemPastaDeClubes);
        Assert.Null(fonte.LerTexto("manifesto.json"));
    }

    [Fact]
    public void FonteMemoria_SemClubes_GeraErroDePastaAusente()
    {
        var erros = Assert.Throws<DefinicoesInvalidasException>(() =>
            LeitorDefinicoes.Carregar(new FonteMemoria(new Dictionary<string, string>()))).Erros;

        Assert.Contains(erros, e => e.Mensagem == "Pasta de clubes não encontrada.");
    }

    private static Dictionary<string, string> LerTudo(string pasta) =>
        Directory.GetFiles(pasta, "*.json", SearchOption.AllDirectories).ToDictionary(
            f => Path.GetRelativePath(pasta, f).Replace('\\', '/'),
            File.ReadAllText,
            StringComparer.Ordinal);
}
