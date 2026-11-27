using Jogo.Motor;
using Jogo.Simulacao.Geracao;

namespace Jogo.Simulacao.Testes;

public class CarreiraTestes
{
    private static Carreira Nova(string clube = "pampa") =>
        new(GeradorDeMundo.Gerar(2027, 2027, CatalogoBase.Clubes), clube, 9);

    [Fact]
    public void Inicio_TaticaAutomaticaValidaEProximoJogoNaRodadaUm()
    {
        var carreira = Nova();

        Assert.Empty(carreira.ValidarTatica(carreira.Tatica));
        Assert.Equal(1, carreira.ProximoJogo!.Rodada);
        Assert.Equal("serie-a", carreira.MinhaCompeticao.Divisao.Id);
    }

    [Fact]
    public void MontarEscalacao_LevaEstiloECapitao_LesionadoViraAutomatico()
    {
        var carreira = Nova();
        var t = carreira.Tatica;
        carreira.DefinirTatica(t with { Marcacao = Marcacao.Forte, Concentracao = Concentracao.Direita, CapitaoId = t.Titulares[4], CobradorId = t.Titulares[7] });

        var escalacao = carreira.MontarEscalacao().Escalacao;
        Assert.Equal((Marcacao.Forte, Concentracao.Direita, t.Titulares[4], t.Titulares[7]),
            (escalacao.Marcacao, escalacao.Concentracao, escalacao.CapitaoId!.Value, escalacao.CobradorId!.Value));

        carreira.Clube.Elenco.Single(j => j.Id == t.Titulares[4]).Lesionar(2);
        Assert.Null(carreira.MontarEscalacao().Escalacao.CapitaoId);
    }

    [Fact]
    public void DefinirTatica_CapitaoQueNaoETitular_VoltaParaOAutomatico()
    {
        var carreira = Nova();
        var reserva = carreira.Tatica.Reservas[0];

        carreira.DefinirTatica(carreira.Tatica with { CapitaoId = reserva, CobradorId = reserva });

        Assert.Null(carreira.Tatica.CapitaoId);
        Assert.Null(carreira.Tatica.CobradorId);
    }

    [Fact]
    public void MontarEscalacao_TrocaLesionadoEAvisa()
    {
        var carreira = Nova();
        var titular = carreira.Clube.Elenco.Single(j => j.Id == carreira.Tatica.Titulares[3]);
        titular.Lesionar(2);

        var (escalacao, ajustes) = carreira.MontarEscalacao();

        Assert.DoesNotContain(escalacao.Titulares, j => j.Id == titular.Id);
        Assert.DoesNotContain(escalacao.Reservas, j => j.Id == titular.Id);
        var aviso = Assert.Single(ajustes);
        Assert.Contains(titular.Nome, aviso, StringComparison.Ordinal);
        Assert.Contains("lesionado", aviso, StringComparison.Ordinal);
    }

    [Fact]
    public void DefinirTatica_RejeitaJogadorDeOutroClubeERepetido()
    {
        var carreira = Nova();
        var outro = carreira.Mundo.Clube("cerrado").Elenco[0].Id;
        var t = carreira.Tatica;

        Assert.Throws<ArgumentException>(() => carreira.DefinirTatica(t with { Titulares = [.. t.Titulares.Take(10), outro] }));
        Assert.Throws<ArgumentException>(() => carreira.DefinirTatica(t with { Reservas = [t.Titulares[0]] }));
    }

    [Fact]
    public void AvancarRodada_UsaATaticaDoJogador()
    {
        var carreira = Nova();
        carreira.DefinirTatica(carreira.Tatica with { Mentalidade = Mentalidade.MuitoOfensiva });

        var rodada = carreira.AvancarRodada();

        var meuJogo = rodada.JogosPorDivisao["serie-a"].Single(j =>
            j.Resultado.MandanteId == "pampa" || j.Resultado.VisitanteId == "pampa");
        var titularesEmCampo = meuJogo.Resultado.Participacoes.Where(p => carreira.Tatica.Titulares.Contains(p.JogadorId));
        Assert.Equal(11, titularesEmCampo.Count());
        Assert.Equal(Mentalidade.MuitoOfensiva, carreira.Tatica.Mentalidade);
        Assert.Equal(1, carreira.Temporada.RodadaAtual);
    }

    [Fact]
    public void EncerrarTemporada_ComecaOAnoSeguinteComTaticaValida()
    {
        var carreira = Nova("cerrado");
        while (!carreira.Temporada.Encerrada)
        {
            carreira.AvancarRodada();
        }

        var resultado = carreira.EncerrarTemporada();

        Assert.Equal(2027, resultado.Ano);
        Assert.Equal(2028, carreira.Temporada.Ano);
        Assert.Equal(0, carreira.Temporada.RodadaAtual);
        Assert.Empty(carreira.ValidarTatica(carreira.Tatica));
    }
}
