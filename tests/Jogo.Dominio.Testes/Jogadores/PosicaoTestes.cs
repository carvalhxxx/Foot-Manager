using Jogo.Dominio.Jogadores;

namespace Jogo.Dominio.Testes.Jogadores;

public class PosicaoTestes
{
    [Fact]
    public void Todas_TemQuinzePosicoes()
    {
        Assert.Equal(15, PosicaoExtensoes.Todas.Count);
    }

    [Theory]
    [InlineData(Posicao.Goleiro, Setor.Gol)]
    [InlineData(Posicao.LateralEsquerdo, Setor.Defesa)]
    [InlineData(Posicao.AlaDireito, Setor.Meio)]
    [InlineData(Posicao.MeiaOfensivo, Setor.Meio)]
    [InlineData(Posicao.SegundoAtacante, Setor.Ataque)]
    [InlineData(Posicao.PontaEsquerda, Setor.Ataque)]
    public void Setor_DeCadaPosicao(Posicao posicao, Setor esperado)
    {
        Assert.Equal(esperado, posicao.Setor());
    }

    [Theory]
    [InlineData(Posicao.LateralDireito, Lado.Direita)]
    [InlineData(Posicao.MeiaEsquerdo, Lado.Esquerda)]
    [InlineData(Posicao.Centroavante, Lado.Centro)]
    public void Lado_DeCadaPosicao(Posicao posicao, Lado esperado)
    {
        Assert.Equal(esperado, posicao.Lado());
    }

    [Fact]
    public void Funcao_LadoEsquerdo_MapeiaParaODireito()
    {
        Assert.Equal(Posicao.AlaDireito, Posicao.AlaEsquerdo.Funcao());
        Assert.Equal(Posicao.Volante, Posicao.Volante.Funcao());
    }
}
