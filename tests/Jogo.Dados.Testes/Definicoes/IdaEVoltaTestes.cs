using Jogo.Dados.Definicoes;
using Jogo.Dominio.Jogadores;

namespace Jogo.Dados.Testes.Definicoes;

public class IdaEVoltaTestes
{
    private static readonly Manifesto ManifestoPadrao = new("Base de teste", "Equipe", "Mundo pequeno para testes");

    [Fact]
    public void SalvarCarregarSalvar_GeraArquivosIdenticos()
    {
        using var pastaA = new PastaTemporaria();
        using var pastaB = new PastaTemporaria();

        EscritorDefinicoes.Salvar(MundoDeExemplo.Criar(), ManifestoPadrao, pastaA.Caminho);
        var carregado = LeitorDefinicoes.Carregar(pastaA.Caminho);
        EscritorDefinicoes.Salvar(carregado.Mundo, carregado.Manifesto, pastaB.Caminho);

        var arquivosA = ListarArquivos(pastaA.Caminho);
        Assert.Equal(arquivosA, ListarArquivos(pastaB.Caminho));
        foreach (var relativo in arquivosA)
        {
            Assert.Equal(File.ReadAllBytes(Path.Combine(pastaA.Caminho, relativo)),
                         File.ReadAllBytes(Path.Combine(pastaB.Caminho, relativo)));
        }
    }

    [Fact]
    public void Carregar_PreservaTodosOsDados()
    {
        using var pasta = new PastaTemporaria();
        var original = MundoDeExemplo.Criar();

        EscritorDefinicoes.Salvar(original, ManifestoPadrao, pasta.Caminho);
        var (manifesto, mundo) = LeitorDefinicoes.Carregar(pasta.Caminho);

        Assert.Equal(ManifestoPadrao, manifesto);
        Assert.Equal(2027, mundo.TemporadaAtual);
        Assert.Equal(["serie-a", "serie-b"], mundo.Divisoes.Select(d => d.Id));
        Assert.Equal(original.Clubes.Select(c => c.Id), mundo.Clubes.Select(c => c.Id));

        var clube = mundo.Clube("uniao-paulista");
        Assert.Equal("União Paulista", clube.Nome);
        Assert.Equal(1_234_567.89m, clube.Caixa);
        Assert.Equal(30_001, clube.Estadio.Capacidade);

        var lateral = clube.Elenco.Single(j => j.Nome == "Pedrinho");
        var lateralOriginal = original.Clube("uniao-paulista").Elenco.Single(j => j.Nome == "Pedrinho");
        Assert.Equal(Posicao.LateralEsquerdo, lateral.PosicaoPrincipal);
        Assert.Equal([Posicao.AlaEsquerdo, Posicao.MeiaEsquerdo], lateral.PosicoesSecundarias);
        Assert.Equal(lateralOriginal.Atributos, lateral.Atributos);
        Assert.Equal(lateralOriginal.Ocultos, lateral.Ocultos);
        Assert.Equal(lateralOriginal.Contrato, lateral.Contrato);
    }

    [Fact]
    public void Salvar_MantemAcentosLegiveisESemBom()
    {
        using var pasta = new PastaTemporaria();

        EscritorDefinicoes.Salvar(MundoDeExemplo.Criar(), ManifestoPadrao, pasta.Caminho);

        var bytes = File.ReadAllBytes(pasta.Arquivo("clubes/uniao-paulista.json"));
        Assert.NotEqual(0xEF, bytes[0]);
        var texto = File.ReadAllText(pasta.Arquivo("clubes/uniao-paulista.json"));
        Assert.Contains("\"nome\": \"União Paulista\"", texto, StringComparison.Ordinal);
        Assert.Contains("\"posicao\": \"LE\"", texto, StringComparison.Ordinal);
        Assert.DoesNotContain("\r\n", texto, StringComparison.Ordinal);
    }

    [Fact]
    public void Salvar_ApagaClubesQueNaoExistemMais()
    {
        using var pasta = new PastaTemporaria();
        Directory.CreateDirectory(pasta.Arquivo("clubes"));
        File.WriteAllText(pasta.Arquivo("clubes/clube-antigo.json"), "{}");

        EscritorDefinicoes.Salvar(MundoDeExemplo.Criar(), ManifestoPadrao, pasta.Caminho);

        Assert.False(File.Exists(pasta.Arquivo("clubes/clube-antigo.json")));
    }

    private static string[] ListarArquivos(string pasta) =>
        [.. Directory.GetFiles(pasta, "*", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(pasta, f).Replace('\\', '/'))
            .Order(StringComparer.Ordinal)];
}
