using Jogo.Dominio.Aleatoriedade;
using Jogo.Dominio.Jogadores;

namespace Jogo.Motor.Testes;

public class MotorDePartidaTestes
{
    [Fact]
    public void MesmaSeed_GeraExatamenteOMesmoJogo()
    {
        var (m, v) = Apoio.DoisTimes();

        var a = MotorDePartida.SimularRapido(m, v, new GeradorAleatorio(42));
        var b = MotorDePartida.SimularRapido(m, v, new GeradorAleatorio(42));

        Assert.Equal(a.Eventos, b.Eventos);
        Assert.Equal(a.Participacoes, b.Participacoes);
    }

    [Fact]
    public void SeedsDiferentes_GeramJogosDiferentes()
    {
        var (m, v) = Apoio.DoisTimes();

        var placares = Enumerable.Range(1, 30)
            .Select(s => MotorDePartida.SimularRapido(m, v, new GeradorAleatorio((ulong)s)))
            .Select(r => (r.GolsMandante, r.GolsVisitante))
            .Distinct()
            .Count();

        Assert.True(placares >= 5);
    }

    [Fact]
    public void Partida_TemInicioIntervaloEFimComAcrescimos()
    {
        var partida = Apoio.Iniciar();
        var resultado = MotorDePartida.SimularAteOFim(partida);

        Assert.Equal(TipoEvento.InicioDeJogo, resultado.Eventos[0].Tipo);
        var intervalo = Assert.Single(resultado.Eventos, e => e.Tipo == TipoEvento.Intervalo);
        Assert.Equal((45, partida.AcrescimoPrimeiroTempo), (intervalo.Minuto, intervalo.Acrescimo));
        var fim = resultado.Eventos[^1];
        Assert.Equal(TipoEvento.FimDeJogo, fim.Tipo);
        Assert.Equal((90, partida.AcrescimoSegundoTempo), (fim.Minuto, fim.Acrescimo));
        Assert.Equal(90 + partida.AcrescimoPrimeiroTempo + partida.AcrescimoSegundoTempo, partida.MinutosCorridos);
        Assert.InRange(partida.AcrescimoSegundoTempo, 1, 6);
    }

    [Fact]
    public void Placar_BateComEventosEstatisticasEParticipacoes()
    {
        for (ulong seed = 1; seed <= 40; seed++)
        {
            var partida = Apoio.Iniciar(seed);
            var r = MotorDePartida.SimularAteOFim(partida);

            Assert.Equal(r.GolsMandante, r.Eventos.Count(e => e is { Tipo: TipoEvento.Gol, Equipe: Equipe.Mandante }));
            Assert.Equal(r.GolsVisitante, r.Eventos.Count(e => e is { Tipo: TipoEvento.Gol, Equipe: Equipe.Visitante }));
            Assert.Equal(r.GolsMandante, r.Participacoes.Where(p => p.Equipe == Equipe.Mandante).Sum(p => p.Gols));
            Assert.Equal(partida.MinutosCorridos, r.EstatisticasMandante.MinutosComPosse + r.EstatisticasVisitante.MinutosComPosse);
            Assert.True(r.EstatisticasMandante.NoAlvo <= r.EstatisticasMandante.Finalizacoes);
            Assert.True(r.GolsMandante <= r.EstatisticasMandante.NoAlvo);
        }
    }

    [Fact]
    public void AvancarMinuto_DepoisDoFim_NaoFazNada()
    {
        var partida = Apoio.Iniciar();
        MotorDePartida.SimularAteOFim(partida);

        Assert.Empty(MotorDePartida.AvancarMinuto(partida));
        Assert.Throws<InvalidOperationException>(() =>
            MotorDePartida.AplicarDecisao(partida, Equipe.Mandante, new TrocaDeMentalidade(Mentalidade.Ofensiva)));
    }

    [Fact]
    public void Condicao_CaiDuranteOJogo()
    {
        var partida = Apoio.Iniciar(automatico: false);
        for (var i = 0; i < 60; i++)
        {
            MotorDePartida.AvancarMinuto(partida);
        }

        Assert.All(partida.Mandante.EmCampo, j => Assert.InRange(j.Condicao, 60, 90));
    }

    [Fact]
    public void TimeMaisForte_VenceAMaioria_MasNaoTodas()
    {
        var (forte, fraco) = Apoio.DoisTimes(forcaMandante: 78, forcaVisitante: 62);

        var resultados = Enumerable.Range(1, 300)
            .Select(s => MotorDePartida.SimularRapido(forte, fraco, new GeradorAleatorio((ulong)s)))
            .ToArray();

        var vitorias = resultados.Count(r => r.GolsMandante > r.GolsVisitante);
        var derrotas = resultados.Count(r => r.GolsMandante < r.GolsVisitante);
        Assert.InRange(vitorias, 180, 290);
        Assert.True(derrotas > 0, "Zebra precisa ser possível.");
    }

    [Fact]
    public void Mando_TimesIguais_MandanteVenceMaisQuePerde()
    {
        var (m, v) = Apoio.DoisTimes(70, 70);

        var resultados = Enumerable.Range(1, 1500)
            .Select(s => MotorDePartida.SimularRapido(m, v, new GeradorAleatorio((ulong)s)))
            .ToArray();

        Assert.True(resultados.Count(r => r.GolsMandante > r.GolsVisitante) > resultados.Count(r => r.GolsMandante < r.GolsVisitante));
    }

    [Fact]
    public void CampoNeutro_RemoveOBonusDeMando()
    {
        var neutro = Apoio.Iniciar();
        var (m, v) = Apoio.DoisTimes();
        var semMando = MotorDePartida.Iniciar(m, v, new GeradorAleatorio(1), new CondicoesPartida(CampoNeutro: true));

        Assert.True(MotorDePartida.Forca(neutro, neutro.Mandante).Meio > MotorDePartida.Forca(semMando, semMando.Mandante).Meio);
    }

    [Fact]
    public void Goleada_TravaReduzAChanceDeMaisGols()
    {
        var (forte, fraco) = Apoio.DoisTimes(forcaMandante: 90, forcaVisitante: 40);
        var semTrava = ConfiguracaoMotor.Padrao with { TravaGoleada = 1.0 };

        double Media(ConfiguracaoMotor cfg) => Enumerable.Range(1, 200)
            .Average(s => MotorDePartida.SimularRapido(forte, fraco, new GeradorAleatorio((ulong)s), configuracao: cfg).GolsMandante);

        Assert.True(Media(ConfiguracaoMotor.Padrao) < Media(semTrava));
    }

    [Fact]
    public void Iniciar_JogadorNosDoisTimes_Lanca()
    {
        var (m, _) = Apoio.DoisTimes();

        Assert.Throws<ArgumentException>(() => MotorDePartida.Iniciar(m, m, new GeradorAleatorio(1)));
    }

    [Fact]
    public void Expulsao_TiraOJogadorEOGoleiroESubstituidoNaPosicao()
    {
        // Toda falta vira vermelho: o time vai ficando com menos jogadores.
        var cfg = ConfiguracaoMotor.Padrao with { ChanceFaltaPorMinuto = 1, ChanceVermelhoDiretoPorFalta = 1 };
        var partida = Apoio.Iniciar(cfg: cfg, automatico: false);

        for (var i = 0; i < 4; i++)
        {
            MotorDePartida.AvancarMinuto(partida);
        }

        foreach (var time in new[] { partida.Mandante, partida.Visitante })
        {
            Assert.Equal(11 - time.Estatisticas.Vermelhos, time.EmCampo.Count);
            Assert.DoesNotContain(time.EmCampo, j => j.Expulso);
            Assert.Single(time.EmCampo, j => j.Posicao == Posicao.Goleiro);
        }
    }
}
