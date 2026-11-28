using Jogo.Dominio.Aleatoriedade;
using Jogo.Simulacao.Copas;
using Jogo.Simulacao.Geracao;
using Jogo.Simulacao.Temporadas;

namespace Jogo.Simulacao.Testes.Copas;

public class CopaDoBrasilTestes
{
    private static Dominio.Mundo NovoMundo() => GeradorDeMundo.Gerar(2027, 2027, CatalogoBase.Clubes);

    [Fact]
    public void Sorteio_40Clubes_CabecasCompletamA2aFaseCom32()
    {
        var mundo = NovoMundo();
        var copa = new CopaDoBrasil(mundo, 7);

        Assert.Equal(64 - 40, copa.Cabecas.Count);
        var primeira = copa.Confrontos(FaseCopa.PrimeiraFase);
        Assert.Equal((40 - copa.Cabecas.Count) / 2, primeira.Count);
        // Ninguém joga duas vezes nem é cabeça e joga a 1ª fase.
        var naPrimeira = primeira.SelectMany(c => new[] { c.ClubeA, c.ClubeB }).ToList();
        Assert.Equal(naPrimeira.Count, naPrimeira.Distinct().Count());
        Assert.Empty(naPrimeira.Intersect(copa.Cabecas));
    }

    [Fact]
    public void Sorteio_MesmaSeed_MesmosConfrontos()
    {
        var a = new CopaDoBrasil(NovoMundo(), 7).Confrontos(FaseCopa.PrimeiraFase).Select(c => (c.ClubeA, c.ClubeB));
        var b = new CopaDoBrasil(NovoMundo(), 7).Confrontos(FaseCopa.PrimeiraFase).Select(c => (c.ClubeA, c.ClubeB));

        Assert.Equal(a, b);
    }

    [Fact]
    public void Confronto_AgregadoEPenaltis()
    {
        var copa = new CopaDoBrasil(NovoMundo(), 7);
        var pf = copa.Confrontos(FaseCopa.PrimeiraFase)[0];

        copa.Registrar(new JogoCopa(FaseCopa.PrimeiraFase, 0, 1, 3, pf.ClubeA, pf.ClubeB, 1, 1, 3, 4));

        Assert.Equal(pf.ClubeB, pf.Vencedor);
    }

    [Fact]
    public void Registrar_JogoQueNaoBateComOSorteio_Lanca()
    {
        var copa = new CopaDoBrasil(NovoMundo(), 7);
        var pf = copa.Confrontos(FaseCopa.PrimeiraFase)[0];

        Assert.Throws<InvalidOperationException>(() =>
            copa.Registrar(new JogoCopa(FaseCopa.PrimeiraFase, 0, 1, 3, pf.ClubeB, pf.ClubeA, 1, 0)));
    }

    [Fact]
    public void Temporada_CopaTerminaComCampeaoEPremio()
    {
        var mundo = NovoMundo();
        var temporada = new Temporada(mundo, 5);
        temporada.JogarAteOFim();
        var copa = temporada.Copa!;

        Assert.NotNull(copa.Campeao);
        Assert.NotNull(copa.Vice);
        Assert.Equal(16, copa.Confrontos(FaseCopa.SegundaFase).Count);
        Assert.All(copa.FasesDefinidas.SelectMany(copa.Confrontos), c => Assert.NotNull(c.Vencedor));
        Assert.Equal(11, copa.Disputados.Select(j => j.Rodada).Distinct().Count());
        Assert.True(mundo.Clube(copa.Campeao!).Extrato[Dominio.Clubes.CategoriaFinanceira.Premiacao] >= CopaDoBrasil.PremioPorAvancar(FaseCopa.Final));
        var resultado = FimDeTemporada.Encerrar(temporada, mundo, 5);
        Assert.Equal((copa.Campeao, copa.Vice), (resultado.CampeaoCopa, resultado.ViceCopa));
    }

    [Fact]
    public void Temporada_RelatorioDaRodadaTrazOsJogosDaCopa()
    {
        var temporada = new Temporada(NovoMundo(), 5);
        RelatorioRodada? terceira = null;
        for (var r = 0; r < 3; r++)
        {
            terceira = temporada.JogarRodada();
        }

        Assert.Equal(temporada.Copa!.Confrontos(FaseCopa.PrimeiraFase).Count, terceira!.JogosDaCopa.Count);
        Assert.Equal(terceira.JogosDaCopa.Count, temporada.RelatorioDaRodada(3)!.JogosDaCopa.Count);
    }

    [Fact]
    public void Penaltis_TerminaComVencedorEDeterministico()
    {
        var mundo = NovoMundo();
        var a = mundo.Clubes[0].Elenco.Take(11).ToArray();
        var b = mundo.Clubes[1].Elenco.Take(11).ToArray();

        for (ulong s = 1; s <= 50; s++)
        {
            var (m, v, cobrancas) = Penaltis.Disputar(a, null, b, null, new GeradorAleatorio(s));
            Assert.NotEqual(m, v);
            Assert.Equal((m, v), (cobrancas.Count(c => c.Equipe == Motor.Equipe.Mandante && c.Gol), cobrancas.Count(c => c.Equipe == Motor.Equipe.Visitante && c.Gol)));
            var repetida = Penaltis.Disputar(a, null, b, null, new GeradorAleatorio(s));
            Assert.Equal((m, v), (repetida.Mandante, repetida.Visitante));
        }
    }
}
