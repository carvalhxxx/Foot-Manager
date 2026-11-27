using Jogo.Dominio.Jogadores;

namespace Jogo.Motor.Testes;

public class SetoresTestes
{
    private static ForcaSetores Calcular(Formacao formacao, double valor = 70) =>
        Setores.Calcular(formacao.PosicoesComGoleiro.Select(p => (p, valor)), 0.5);

    [Fact]
    public void QuatroQuatroDois_EhAReferencia_ForcaIgualAQualidade()
    {
        var forca = Calcular(FormacoesPadrao.QuatroQuatroDois);

        Assert.Equal(70, forca.Gol, 6);
        Assert.Equal(70, forca.Defesa, 6);
        Assert.Equal(70, forca.Meio, 6);
        Assert.Equal(70, forca.Ataque, 6);
    }

    [Fact]
    public void CincoZeroCinco_QuaseSemMeio_PerdeOMeioCampo()
    {
        var extrema = Setores.Calcular(
            new Formacao("5-0-5",
            [
                Posicao.LateralDireito, Posicao.Zagueiro, Posicao.Zagueiro, Posicao.Zagueiro, Posicao.LateralEsquerdo,
                Posicao.PontaDireita, Posicao.SegundoAtacante, Posicao.Centroavante, Posicao.Centroavante, Posicao.PontaEsquerda,
            ]).PosicoesComGoleiro.Select(p => (p, 70.0)), 0.5);

        Assert.True(extrema.Meio < 40);
        Assert.True(extrema.Ataque > 70);
    }

    [Fact]
    public void SemNinguemNoSetor_ForcaZero()
    {
        var forca = Setores.Calcular([(Posicao.Goleiro, 80.0), (Posicao.Zagueiro, 70.0)], 0.5);

        Assert.Equal(0, forca.Meio);
        Assert.Equal(0, forca.Ataque);
    }

    [Theory]
    [InlineData(Posicao.LateralDireito, new Posicao[0], Posicao.LateralDireito, 1.0)]
    [InlineData(Posicao.LateralDireito, new[] { Posicao.AlaDireito }, Posicao.AlaDireito, 1.0)]
    [InlineData(Posicao.LateralDireito, new Posicao[0], Posicao.LateralEsquerdo, 0.93)]
    [InlineData(Posicao.LateralDireito, new Posicao[0], Posicao.Zagueiro, 0.85)]
    [InlineData(Posicao.LateralDireito, new Posicao[0], Posicao.Centroavante, 0.7)]
    [InlineData(Posicao.Centroavante, new Posicao[0], Posicao.Goleiro, 0.5)]
    public void FatorImproviso_PorDistanciaDaPosicao(Posicao principal, Posicao[] secundarias, Posicao jogaEm, double esperado)
    {
        var jogador = new Jogador(1, "J", 25, principal, secundarias, Atributos.Uniformes(10),
            new AtributosOcultos { Potencial = 60, PropensaoLesao = 5, Profissionalismo = 10 });

        Assert.Equal(esperado, Setores.FatorImproviso(jogador, jogaEm));
    }

    [Fact]
    public void Formacao_SemDezPosicoesOuComGoleiro_Lanca()
    {
        Assert.Throws<ArgumentException>(() => new Formacao("x", [Posicao.Zagueiro]));
        Assert.Throws<ArgumentException>(() => new Formacao("x", [.. Enumerable.Repeat(Posicao.Goleiro, 10)]));
    }

    [Fact]
    public void FormacoesPadrao_TodasValidasENomesUnicos()
    {
        Assert.Equal(7, FormacoesPadrao.Todas.Count);
        Assert.Equal(7, FormacoesPadrao.Todas.Select(f => f.Nome).Distinct().Count());
    }
}
