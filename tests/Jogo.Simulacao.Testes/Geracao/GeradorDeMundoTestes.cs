using Jogo.Dominio;
using Jogo.Dominio.Clubes;
using Jogo.Dominio.Jogadores;
using Jogo.Simulacao.Geracao;

namespace Jogo.Simulacao.Testes.Geracao;

public class GeradorDeMundoTestes
{
    private static readonly Mundo MundoBase = GeradorDeMundo.Gerar(2027, 2027, CatalogoBase.Clubes);

    [Fact]
    public void MesmaSeed_GeraExatamenteOMesmoMundo()
    {
        var outro = GeradorDeMundo.Gerar(2027, 2027, CatalogoBase.Clubes);

        Assert.Equal(Assinatura(MundoBase), Assinatura(outro));
    }

    [Fact]
    public void SeedDiferente_GeraOutroMundo()
    {
        var outro = GeradorDeMundo.Gerar(9999, 2027, CatalogoBase.Clubes);

        Assert.NotEqual(Assinatura(MundoBase), Assinatura(outro));
    }

    [Fact]
    public void RemoverClubesDoCatalogo_NaoMudaElencoDosOutros()
    {
        // Tira dois clubes da Série B (mantém número par).
        var reduzido = CatalogoBase.Clubes.Where(c => c.Id is not "dunas" and not "poti").ToArray();

        var mundo = GeradorDeMundo.Gerar(2027, 2027, reduzido);

        foreach (var clube in mundo.Clubes)
        {
            var original = MundoBase.Clube(clube.Id);
            Assert.Equal(
                original.Elenco.Select(j => (j.Nome, j.Idade, j.PosicaoPrincipal, j.Atributos)),
                clube.Elenco.Select(j => (j.Nome, j.Idade, j.PosicaoPrincipal, j.Atributos)));
        }
    }

    [Fact]
    public void Estrutura_SeriesAEBCom20ClubesE38Rodadas()
    {
        Assert.Equal(40, MundoBase.Clubes.Count);
        Assert.Equal(2, MundoBase.Divisoes.Count);

        var (serieA, serieB) = (MundoBase.Divisoes[0], MundoBase.Divisoes[1]);
        Assert.Equal(("serie-a", "Série A", 20, 0, 4), (serieA.Id, serieA.Nome, serieA.ClubeIds.Count, serieA.VagasAcesso, serieA.VagasRebaixamento));
        Assert.Equal(("serie-b", "Série B", 20, 4, 0), (serieB.Id, serieB.Nome, serieB.ClubeIds.Count, serieB.VagasAcesso, serieB.VagasRebaixamento));
        Assert.Equal(38, serieA.TotalRodadas);
    }

    [Fact]
    public void Elencos_Entre25E30ComTodasAsFuncoesCobertas()
    {
        foreach (var clube in MundoBase.Clubes)
        {
            Assert.InRange(clube.Elenco.Count, 25, 30);
            Assert.True(Contar(clube, Posicao.Goleiro) >= 3, clube.Id);
            Assert.True(Contar(clube, Posicao.Zagueiro) >= 4, clube.Id);
            Assert.True(Contar(clube, Posicao.LateralDireito) >= 2, clube.Id);
            Assert.True(Contar(clube, Posicao.LateralEsquerdo) >= 2, clube.Id);
            Assert.True(Contar(clube, Posicao.Centroavante) >= 3, clube.Id);
            Assert.Equal(clube.Elenco.Count, clube.Elenco.Select(j => j.Nome).Distinct().Count());
            Assert.All(clube.Elenco, j => Assert.Equal(clube.Id, j.Contrato!.ClubeId));
        }
    }

    [Fact]
    public void Forca_GrandesAcimaDeMediosAcimaDePequenos()
    {
        static double MediaTop11(Porte porte, int divisao) => CatalogoBase.Clubes
            .Where(m => m.Porte == porte && m.Divisao == divisao)
            .Average(m => Top11(MundoBase.Clube(m.Id)));

        var grandes = MediaTop11(Porte.Grande, 1);
        var mediosA = MediaTop11(Porte.Medio, 1);
        var mediosB = MediaTop11(Porte.Medio, 2);
        var pequenos = MediaTop11(Porte.Pequeno, 2);

        Assert.True(grandes > mediosA + 3, $"grandes {grandes:F1} x médios A {mediosA:F1}");
        Assert.True(mediosA > mediosB + 2, $"médios A {mediosA:F1} x médios B {mediosB:F1}");
        Assert.True(mediosB > pequenos + 2, $"médios B {mediosB:F1} x pequenos {pequenos:F1}");
    }

    [Fact]
    public void Craques_TodoGrandeTemAoMenosUmAcimaDe85()
    {
        foreach (var modelo in CatalogoBase.Clubes.Where(m => m.Porte == Porte.Grande))
        {
            Assert.Contains(MundoBase.Clube(modelo.Id).Elenco, j => j.ForcaGeral >= 85);
        }

        var maiorForca = MundoBase.Clubes.SelectMany(c => c.Elenco).Max(j => j.ForcaGeral);
        Assert.InRange(maiorForca, 85, 95);
    }

    [Fact]
    public void Divisoes_SerieAMaisForteQueSerieB()
    {
        static double Media(int i) => MundoBase.Divisoes[i].ClubeIds.Average(id => Top11(MundoBase.Clube(id)));

        Assert.True(Media(0) > Media(1) + 5);
    }

    [Fact]
    public void Dinheiro_GrandesTemMaisCaixaETorcida()
    {
        var grandes = CatalogoBase.Clubes.Where(m => m.Porte == Porte.Grande).Select(m => MundoBase.Clube(m.Id)).ToArray();
        var pequenos = CatalogoBase.Clubes.Where(m => m.Porte == Porte.Pequeno).Select(m => MundoBase.Clube(m.Id)).ToArray();

        Assert.True(grandes.Min(c => c.Caixa) > pequenos.Max(c => c.Caixa));
        Assert.True(grandes.Min(c => c.Torcida) > pequenos.Max(c => c.Torcida));
        Assert.True(grandes.Min(c => c.Estadio.Capacidade) > pequenos.Max(c => c.Estadio.Capacidade));
    }

    [Fact]
    public void Catalogo_IdsSiglasENomesUnicos()
    {
        Assert.Equal(40, CatalogoBase.Clubes.Select(c => c.Id).Distinct().Count());
        Assert.Equal(40, CatalogoBase.Clubes.Select(c => c.Sigla).Distinct().Count());
        Assert.Equal(40, CatalogoBase.Clubes.Select(c => c.Nome).Distinct().Count());
    }

    [Fact]
    public void HashEstavel_ValoresDeReferenciaDoFnv1a()
    {
        Assert.Equal(14695981039346656037UL, GeradorDeMundo.HashEstavel(""));
        Assert.Equal(0xAF63DC4C8601EC8CUL, GeradorDeMundo.HashEstavel("a"));
    }

    private static int Contar(Clube clube, Posicao posicao) => clube.Elenco.Count(j => j.PosicaoPrincipal == posicao);

    private static double Top11(Clube clube) =>
        clube.Elenco.OrderByDescending(j => j.ForcaGeral).Take(11).Average(j => j.ForcaGeral);

    private static List<string> Assinatura(Mundo mundo) =>
    [
        .. mundo.Clubes.Select(c => $"{c.Id}|{c.Estadio}|{c.Torcida}|{c.Reputacao}|{c.Caixa}"),
        .. mundo.Clubes.SelectMany(c => c.Elenco).Select(j =>
            $"{j.Id}|{j.Nome}|{j.Idade}|{j.PosicaoPrincipal}|{string.Join(',', j.PosicoesSecundarias)}|{j.Atributos}|{j.Ocultos}|{j.Contrato}"),
    ];
}
