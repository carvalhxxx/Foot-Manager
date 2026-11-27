using Jogo.Dominio.Clubes;
using Jogo.Dominio.Jogadores;
using Jogo.Motor;
using Jogo.Simulacao.Temporadas;

namespace Jogo.Simulacao.Testes.Temporadas;

public class PosJogoTestes
{
    private static readonly RegrasTemporada Regras = RegrasTemporada.Padrao;

    private static (Clube Clube, TimeEscalado Escalacao) Clube()
    {
        var clube = new Clube("casa", "Casa", "CAS", "Recife", "PE", new Cores("#000000", "#FFFFFF"),
            new Estadio("Estádio", 20_000), 50, 50, 0m);
        for (var i = 1; i <= 14; i++)
        {
            var jogador = new Jogador(i, $"J{i}", 25, i == 1 ? Posicao.Goleiro : Posicao.MeiaCentral, [],
                Atributos.Uniformes(10), new AtributosOcultos { Potencial = 60, PropensaoLesao = 5, Profissionalismo = 10 });
            clube.AdicionarAoElenco(jogador);
        }

        var escalacao = new TimeEscalado("casa", FormacoesPadrao.QuatroQuatroDois, [.. clube.Elenco.Take(11)], [.. clube.Elenco.Skip(11)]);
        return (clube, escalacao);
    }

    private static ResultadoPartida Resultado(int golsCasa, int golsFora, params ParticipacaoJogador[] participacoes) =>
        new("casa", "fora", golsCasa, golsFora, [], new EstatisticasTime(), new EstatisticasTime(), participacoes);

    private static ParticipacaoJogador Participou(int id, int condicao = 70, int amarelos = 0, bool expulso = false, int lesao = 0) =>
        new(id, Equipe.Mandante, 90, condicao, 0, amarelos, expulso, lesao);

    [Fact]
    public void Expulsao_SuspendeUmaRodada()
    {
        var (clube, escalacao) = Clube();

        PosJogo.Aplicar(clube, escalacao, Resultado(1, 0, Participou(2, expulso: true)), Equipe.Mandante, Regras);

        var jogador = clube.Elenco.Single(j => j.Id == 2);
        Assert.Equal(1, jogador.RodadasSuspenso);
        Assert.False(jogador.Disponivel);

        jogador.CumprirRodada();
        Assert.True(jogador.Disponivel);
    }

    [Fact]
    public void TerceiroAmarelo_SuspendeEZeraOsCartoes()
    {
        var (clube, escalacao) = Clube();
        var jogador = clube.Elenco.Single(j => j.Id == 3);

        PosJogo.Aplicar(clube, escalacao, Resultado(0, 0, Participou(3, amarelos: 1)), Equipe.Mandante, Regras);
        PosJogo.Aplicar(clube, escalacao, Resultado(0, 0, Participou(3, amarelos: 1)), Equipe.Mandante, Regras);
        Assert.Equal((2, 0), (jogador.AmarelosAcumulados, jogador.RodadasSuspenso));

        PosJogo.Aplicar(clube, escalacao, Resultado(0, 0, Participou(3, amarelos: 1)), Equipe.Mandante, Regras);
        Assert.Equal((0, 1), (jogador.AmarelosAcumulados, jogador.RodadasSuspenso));
    }

    [Fact]
    public void Lesao_TiraDoJogoPelasRodadasDoMotor()
    {
        var (clube, escalacao) = Clube();

        PosJogo.Aplicar(clube, escalacao, Resultado(0, 1, Participou(4, lesao: 3)), Equipe.Mandante, Regras);

        Assert.Equal(3, clube.Elenco.Single(j => j.Id == 4).RodadasLesionado);
    }

    [Fact]
    public void Condicao_FicaComOValorDoFimDoJogo()
    {
        var (clube, escalacao) = Clube();

        PosJogo.Aplicar(clube, escalacao, Resultado(0, 0, Participou(5, condicao: 64)), Equipe.Mandante, Regras);

        Assert.Equal(64, clube.Elenco.Single(j => j.Id == 5).Condicao);
    }

    [Fact]
    public void Moral_VitoriaSobeDerrotaDesce_QuemJogaGanhaMais()
    {
        var (clube, escalacao) = Clube();
        PosJogo.Aplicar(clube, escalacao, Resultado(2, 0, Participou(6)), Equipe.Mandante, Regras);

        var jogou = clube.Elenco.Single(j => j.Id == 6).Moral;
        var naoJogou = clube.Elenco.Single(j => j.Id == 14).Moral;
        Assert.True(jogou > 50);
        Assert.True(jogou > naoJogou);

        var (outro, escalacaoOutro) = Clube();
        PosJogo.Aplicar(outro, escalacaoOutro, Resultado(0, 3, Participou(6)), Equipe.Mandante, Regras);
        Assert.True(outro.Elenco.Single(j => j.Id == 6).Moral < 50);
    }

    [Fact]
    public void Entrosamento_SequenciaDeTitularesDaOBonus()
    {
        var (clube, escalacao) = Clube();
        Assert.Equal(0, PosJogo.BonusEntrosamento(escalacao.Titulares, Regras));

        for (var r = 0; r < Regras.RodadasParaEntrosamentoMaximo; r++)
        {
            PosJogo.Aplicar(clube, escalacao, Resultado(0, 0), Equipe.Mandante, Regras);
        }

        Assert.Equal(Regras.EntrosamentoMaximo, PosJogo.BonusEntrosamento(escalacao.Titulares, Regras), 6);
        Assert.Equal(0, clube.Elenco.Single(j => j.Id == 14).SequenciaComoTitular);
    }
}
