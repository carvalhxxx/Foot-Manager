using Jogo.Dominio.Jogadores;
using Jogo.Motor;
using Jogo.Simulacao.Geracao;
using Jogo.Simulacao.Temporadas;

namespace Jogo.Simulacao.Testes;

public class AoVivoTestes
{
    private static Carreira Nova() => new(GeradorDeMundo.Gerar(2027, 2027, CatalogoBase.Clubes), "pampa", 9);

    [Fact]
    public void RodadaPassoAPasso_SemDecisoes_IgualAoModoRapido()
    {
        var rapida = new Temporada(GeradorDeMundo.Gerar(2027, 2027, CatalogoBase.Clubes), 5);
        var passoAPasso = new Temporada(GeradorDeMundo.Gerar(2027, 2027, CatalogoBase.Clubes), 5);

        rapida.JogarRodada();
        var emAndamento = passoAPasso.IniciarRodada();
        while (!emAndamento.Encerrada)
        {
            emAndamento.AvancarMinuto();
        }

        passoAPasso.ConcluirRodada();

        Assert.Equal(rapida.Tabela("serie-a"), passoAPasso.Tabela("serie-a"));
        Assert.Equal(rapida.Tabela("serie-b"), passoAPasso.Tabela("serie-b"));
    }

    [Fact]
    public void RodadaEmAndamento_NaoDeixaIniciarOutraNemConcluirAntesDoFim()
    {
        var temporada = new Temporada(GeradorDeMundo.Gerar(2027, 2027, CatalogoBase.Clubes), 5);
        var rodada = temporada.IniciarRodada();

        Assert.Throws<InvalidOperationException>(() => temporada.IniciarRodada());
        Assert.Throws<InvalidOperationException>(() => temporada.JogarRodada());
        Assert.Throws<InvalidOperationException>(() => temporada.ConcluirRodada());

        rodada.AvancarAteOFim();
        temporada.ConcluirRodada();
        Assert.Null(temporada.EmAndamento);
        Assert.Equal(1, temporada.RodadaAtual);
    }

    [Fact]
    public void AoVivo_TimeDoJogadorNaoTemTreinadorAutomatico()
    {
        var carreira = Nova();
        carreira.IniciarRodadaAoVivo();

        Assert.False(carreira.MeuTimeAoVivo!.Automatico);
        var adversario = carreira.MeuJogoAoVivo!.Partida.Adversario(carreira.MeuTimeAoVivo.Equipe);
        Assert.True(adversario.Automatico);
    }

    [Fact]
    public void AoVivo_SubstituicaoMentalidadeEConclusao()
    {
        var carreira = Nova();
        carreira.IniciarRodadaAoVivo();
        for (var i = 0; i < 30; i++)
        {
            carreira.AvancarMinutoAoVivo();
        }

        var time = carreira.MeuTimeAoVivo!;
        var sai = time.EmCampo.First(j => j.Posicao != Posicao.Goleiro).Jogador.Id;
        var entra = time.Reservas.First(r => r.PosicaoPrincipal != Posicao.Goleiro).Id;

        var evento = carreira.DecidirAoVivo(new Substituicao(sai, entra));
        carreira.DecidirAoVivo(new TrocaDeMentalidade(Mentalidade.Ofensiva));

        Assert.Equal(TipoEvento.Substituicao, evento.Tipo);
        Assert.Contains(time.EmCampo, j => j.Jogador.Id == entra);
        Assert.Equal(Mentalidade.Ofensiva, time.Mentalidade);

        carreira.AoVivo!.AvancarAteOFim();
        var relatorio = carreira.ConcluirRodadaAoVivo();

        Assert.Equal(1, relatorio.Rodada);
        Assert.Null(carreira.AoVivo);
        Assert.Equal(Mentalidade.Ofensiva, carreira.Tatica.Mentalidade);
        var meuJogo = relatorio.JogosPorDivisao["serie-a"].Single(j => j.Resultado.MandanteId == "pampa" || j.Resultado.VisitanteId == "pampa");
        Assert.Contains(meuJogo.Resultado.Participacoes, p => p.JogadorId == entra && p.MinutosJogados > 0);
    }

    [Fact]
    public void AoVivo_TrocaDeFormacaoReposicionaQuemEstaEmCampo()
    {
        var carreira = Nova();
        carreira.IniciarRodadaAoVivo();
        var time = carreira.MeuTimeAoVivo!;
        var antes = time.EmCampo.Select(j => j.Jogador.Id).Order().ToArray();
        var nova = FormacoesPadrao.Todas.First(f => f != time.Formacao);

        carreira.TrocarFormacaoAoVivo(nova);

        Assert.Equal(nova, time.Formacao);
        Assert.Equal(antes, time.EmCampo.Select(j => j.Jogador.Id).Order());
        Assert.Equal(
            nova.PosicoesComGoleiro.Order(),
            time.EmCampo.Select(j => j.Posicao).Order());
    }

    [Fact]
    public void AoVivo_MesmaSeedEMesmasDecisoes_MesmoJogo()
    {
        static IReadOnlyList<EventoPartida> Jogar()
        {
            var carreira = Nova();
            carreira.IniciarRodadaAoVivo();
            for (var minuto = 1; !carreira.AoVivo!.Encerrada; minuto++)
            {
                carreira.AvancarMinutoAoVivo();
                if (minuto == 20)
                {
                    carreira.DecidirAoVivo(new TrocaDeMentalidade(Mentalidade.MuitoOfensiva));
                }

                if (minuto == 60)
                {
                    var time = carreira.MeuTimeAoVivo!;
                    carreira.DecidirAoVivo(new Substituicao(
                        time.EmCampo.First(j => j.Posicao != Posicao.Goleiro).Jogador.Id,
                        time.Reservas.First(r => r.PosicaoPrincipal != Posicao.Goleiro).Id));
                    carreira.TrocarFormacaoAoVivo(FormacoesPadrao.QuatroTresTres);
                }
            }

            return carreira.MeuJogoAoVivo!.Partida.Eventos;
        }

        Assert.Equal(Jogar(), Jogar());
    }

    [Fact]
    public void AoVivo_DecisoesMudamOAndamento()
    {
        var semDecisao = Nova();
        semDecisao.IniciarRodadaAoVivo();
        semDecisao.AoVivo!.AvancarAteOFim();

        var comDecisao = Nova();
        comDecisao.IniciarRodadaAoVivo();
        comDecisao.DecidirAoVivo(new TrocaDeMentalidade(Mentalidade.MuitoOfensiva));
        comDecisao.AoVivo!.AvancarAteOFim();

        Assert.NotEqual(
            semDecisao.MeuJogoAoVivo!.Partida.Eventos.Skip(1),
            comDecisao.MeuJogoAoVivo!.Partida.Eventos.Skip(2));
    }

    [Fact]
    public void AoVivo_NaoDeixaSimularRapidoNoMeio()
    {
        var carreira = Nova();
        carreira.IniciarRodadaAoVivo();

        Assert.Throws<InvalidOperationException>(() => carreira.AvancarRodada());
    }

    [Fact]
    public void AvancarMinutoAoVivo_DevolveSoOsEventosNovosDoMeuJogo()
    {
        var carreira = Nova();
        carreira.IniciarRodadaAoVivo();
        var todos = new List<EventoPartida>();

        while (!carreira.AoVivo!.Encerrada)
        {
            todos.AddRange(carreira.AvancarMinutoAoVivo());
        }

        // O "Início de jogo" já estava lá antes do primeiro minuto; o resto veio minuto a minuto, sem repetição.
        Assert.Equal(carreira.MeuJogoAoVivo!.Partida.Eventos.Skip(1), todos);
    }
}
