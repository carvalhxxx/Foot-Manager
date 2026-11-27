using Jogo.Dominio.Jogadores;

namespace Jogo.Motor.Testes;

public class DecisoesTestes
{
    [Fact]
    public void Substituicao_ReservaEntraNaPosicaoDeQuemSaiu()
    {
        var partida = Apoio.Iniciar(automatico: false);
        var sai = partida.Mandante.EmCampo[1];
        var entra = partida.Mandante.Reservas.First(r => r.PosicaoPrincipal != Posicao.Goleiro);

        var evento = MotorDePartida.AplicarDecisao(partida, Equipe.Mandante, new Substituicao(sai.Jogador.Id, entra.Id));

        Assert.Equal((TipoEvento.Substituicao, sai.Jogador.Id, entra.Id), (evento.Tipo, evento.JogadorId!.Value, evento.OutroJogadorId!.Value));
        var novo = partida.Mandante.EmCampo[1];
        Assert.Equal(entra.Id, novo.Jogador.Id);
        Assert.Equal(sai.Posicao, novo.Posicao);
        Assert.DoesNotContain(partida.Mandante.Reservas, r => r.Id == entra.Id);
        Assert.Equal(1, partida.Mandante.SubstituicoesFeitas);
    }

    [Fact]
    public void Substituicao_QuemSaiuNaoVolta()
    {
        var partida = Apoio.Iniciar(automatico: false);
        var sai = partida.Mandante.EmCampo[2].Jogador;
        var entra = partida.Mandante.Reservas.Last();

        MotorDePartida.AplicarDecisao(partida, Equipe.Mandante, new Substituicao(sai.Id, entra.Id));

        Assert.Throws<InvalidOperationException>(() =>
            MotorDePartida.AplicarDecisao(partida, Equipe.Mandante, new Substituicao(partida.Mandante.EmCampo[3].Jogador.Id, sai.Id)));
    }

    [Fact]
    public void Substituicao_LimiteDeCinco()
    {
        var partida = Apoio.Iniciar(automatico: false);
        var time = partida.Mandante;

        for (var i = 0; i < 5; i++)
        {
            MotorDePartida.AplicarDecisao(partida, Equipe.Mandante,
                new Substituicao(time.EmCampo[1 + i].Jogador.Id, time.Reservas[^1].Id));
        }

        Assert.Throws<InvalidOperationException>(() => MotorDePartida.AplicarDecisao(partida, Equipe.Mandante,
            new Substituicao(time.EmCampo[7].Jogador.Id, time.Reservas[0].Id)));
    }

    [Fact]
    public void Substituicao_JogadorDoOutroTime_Lanca()
    {
        var partida = Apoio.Iniciar(automatico: false);

        Assert.Throws<InvalidOperationException>(() => MotorDePartida.AplicarDecisao(partida, Equipe.Mandante,
            new Substituicao(partida.Visitante.EmCampo[1].Jogador.Id, partida.Mandante.Reservas[0].Id)));
    }

    [Fact]
    public void Lesao_AbreVagaQueAceitaSubstituicao()
    {
        var cfg = ConfiguracaoMotor.Padrao with { ChanceLesaoPorMinuto = 1 };
        var partida = Apoio.Iniciar(cfg: cfg, automatico: false);

        var eventos = MotorDePartida.AvancarMinuto(partida);
        var lesao = eventos.First(e => e is { Tipo: TipoEvento.Lesao, Equipe: Equipe.Mandante });
        var reserva = partida.Mandante.Reservas.First(r => r.PosicaoPrincipal != Posicao.Goleiro);

        MotorDePartida.AplicarDecisao(partida, Equipe.Mandante, new Substituicao(lesao.JogadorId!.Value, reserva.Id));

        Assert.Contains(partida.Mandante.EmCampo, j => j.Jogador.Id == reserva.Id);
        Assert.InRange(int.Parse(lesao.Detalhe!, System.Globalization.CultureInfo.InvariantCulture), 1, 12);
    }

    [Fact]
    public void TreinadorAutomatico_RepoeLesionado()
    {
        var cfg = ConfiguracaoMotor.Padrao with { ChanceLesaoPorMinuto = 0.004 };
        var verificados = 0;

        for (ulong seed = 1; seed <= 30; seed++)
        {
            var partida = Apoio.Iniciar(seed, cfg);
            var r = MotorDePartida.SimularAteOFim(partida);

            Assert.All([partida.Mandante, partida.Visitante], t => Assert.Single(t.EmCampo, j => j.Posicao == Posicao.Goleiro));

            foreach (var lesao in r.Eventos.Where(e => e.Tipo == TipoEvento.Lesao))
            {
                // Se as trocas acabaram até o fim daquele minuto (ex.: dois lesionados com uma troca sobrando), não há o que repor.
                var trocasAteOFimDoMinuto = r.Eventos.Count(e => e.Tipo == TipoEvento.Substituicao && e.Equipe == lesao.Equipe
                    && (e.Minuto < lesao.Minuto || (e.Minuto == lesao.Minuto && e.Acrescimo <= lesao.Acrescimo)));
                if (trocasAteOFimDoMinuto >= cfg.MaximoSubstituicoes)
                {
                    continue;
                }

                // Repor o lesionado é a primeira coisa que o treinador faz, no mesmo minuto.
                Assert.Contains(r.Eventos, e => e is { Tipo: TipoEvento.Substituicao } && e.JogadorId == lesao.JogadorId
                    && (e.Minuto, e.Acrescimo) == (lesao.Minuto, lesao.Acrescimo));
                verificados++;
            }
        }

        Assert.True(verificados > 5);
    }

    [Fact]
    public void TrocaDeMentalidade_MudaOTimeERegistra()
    {
        var partida = Apoio.Iniciar(automatico: false);

        var evento = MotorDePartida.AplicarDecisao(partida, Equipe.Visitante, new TrocaDeMentalidade(Mentalidade.MuitoOfensiva));

        Assert.Equal(Mentalidade.MuitoOfensiva, partida.Visitante.Mentalidade);
        Assert.Equal(TipoEvento.TrocaDeMentalidade, evento.Tipo);
    }

    [Fact]
    public void MentalidadeOfensiva_GeraMaisLancesQueDefensiva()
    {
        var (m, v) = Apoio.DoisTimes();

        double Finalizacoes(Mentalidade mentalidade)
        {
            var total = 0;
            for (ulong s = 1; s <= 300; s++)
            {
                var partida = MotorDePartida.Iniciar(m, v, new Dominio.Aleatoriedade.GeradorAleatorio(s), mandanteAutomatico: false);
                MotorDePartida.AplicarDecisao(partida, Equipe.Mandante, new TrocaDeMentalidade(mentalidade));
                total += MotorDePartida.SimularAteOFim(partida).EstatisticasMandante.Finalizacoes;
            }

            return total / 300.0;
        }

        Assert.True(Finalizacoes(Mentalidade.MuitoOfensiva) > Finalizacoes(Mentalidade.MuitoDefensiva) + 1);
    }

    [Fact]
    public void TrocaDeFormacao_ReposicionaOsJogadores()
    {
        var partida = Apoio.Iniciar(automatico: false);
        var deLinha = partida.Mandante.EmCampo.Where(j => j.Posicao != Posicao.Goleiro).Select(j => j.Jogador.Id).ToArray();

        MotorDePartida.AplicarDecisao(partida, Equipe.Mandante, new TrocaDeFormacao(FormacoesPadrao.QuatroTresTres, deLinha));

        Assert.Equal(FormacoesPadrao.QuatroTresTres, partida.Mandante.Formacao);
        Assert.Equal(
            FormacoesPadrao.QuatroTresTres.Posicoes,
            deLinha.Select(id => partida.Mandante.EmCampo.Single(j => j.Jogador.Id == id).Posicao));
    }

    [Fact]
    public void TrocaDeFormacao_ListaErrada_Lanca()
    {
        var partida = Apoio.Iniciar(automatico: false);

        Assert.Throws<InvalidOperationException>(() => MotorDePartida.AplicarDecisao(partida, Equipe.Mandante,
            new TrocaDeFormacao(FormacoesPadrao.QuatroTresTres, [1, 2, 3])));
    }
}
