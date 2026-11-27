using Jogo.Dominio;
using Jogo.Dominio.Jogadores;
using Jogo.Motor;
using Jogo.Simulacao.Geracao;
using Jogo.Simulacao.Ia;

namespace Jogo.Simulacao.Testes.Ia;

public class EscaladorTestes
{
    private static readonly Mundo Mundo = GeradorDeMundo.Gerar(2027, 2027, CatalogoBase.Clubes);

    [Fact]
    public void Escalar_TodosOsClubes_GoleiroNoGolOnzeDistintosEBancoComGoleiro()
    {
        foreach (var clube in Mundo.Clubes)
        {
            var time = Escalador.Escalar(clube);

            Assert.Equal(Posicao.Goleiro, time.Titulares[0].PosicaoPrincipal);
            Assert.Equal(11, time.Titulares.Select(j => j.Id).Distinct().Count());
            Assert.Equal(Escalador.Reservas, time.Reservas.Count);
            Assert.Contains(time.Reservas, j => j.PosicaoPrincipal == Posicao.Goleiro);
            Assert.Contains(time.Formacao, FormacoesPadrao.Todas);
        }
    }

    [Fact]
    public void EscalarNaFormacao_PoeQuemJogaNaPosicaoQuandoPossivel()
    {
        var clube = Mundo.Clube("pampa");

        var time = Escalador.EscalarNaFormacao(clube, FormacoesPadrao.QuatroQuatroDois);

        var naturais = time.Titulares.Where((j, i) => j.JogaNaPosicao(time.Formacao.PosicoesComGoleiro[i])).Count();
        Assert.True(naturais >= 9, $"só {naturais} jogadores na posição natural");
    }

    [Fact]
    public void Escalar_IgnoraIndisponiveis()
    {
        var clube = Mundo.Clube("pampa");
        var titulares = Escalador.Escalar(clube).Titulares.Select(j => j.Id).ToHashSet();

        var semTitulares = Escalador.Escalar(clube, indisponiveis: titulares);

        Assert.DoesNotContain(semTitulares.Titulares.Concat(semTitulares.Reservas), j => titulares.Contains(j.Id));
    }

    [Fact]
    public void Escalar_EhDeterministico()
    {
        var clube = Mundo.Clube("cerrado");

        Assert.Equal(
            Escalador.Escalar(clube).Titulares.Select(j => j.Id),
            Escalador.Escalar(clube).Titulares.Select(j => j.Id));
    }

    [Fact]
    public void Escalar_MenosDeOnzeDisponiveis_Lanca()
    {
        var clube = Mundo.Clube("cerrado");
        var quase = clube.Elenco.Skip(10).Select(j => j.Id).ToHashSet();

        Assert.Throws<InvalidOperationException>(() => Escalador.Escalar(clube, indisponiveis: quase));
    }
}
