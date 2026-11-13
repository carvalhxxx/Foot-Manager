using Jogo.Dominio.Jogadores;

namespace Jogo.Dominio.Testes.Jogadores;

public class JogadorTestes
{
    private static readonly AtributosOcultos OcultosPadrao = new()
    {
        Potencial = 80,
        PropensaoLesao = 10,
        Profissionalismo = 10,
    };

    private static Jogador Criar(
        Posicao principal = Posicao.LateralDireito,
        Posicao[]? secundarias = null,
        int idade = 24) =>
        new(1, "Joãozinho", idade, principal, secundarias ?? [], Atributos.Uniformes(12), OcultosPadrao);

    [Fact]
    public void ForcaGeral_UsaAPosicaoPrincipal()
    {
        var jogador = new Jogador(
            1, "Artilheiro", 27, Posicao.Centroavante, [],
            Atributos.Uniformes(8) with { Finalizacao = 19 }, OcultosPadrao);

        Assert.Equal(jogador.ForcaNaPosicao(Posicao.Centroavante), jogador.ForcaGeral);
        Assert.True(jogador.ForcaGeral > jogador.ForcaNaPosicao(Posicao.Zagueiro));
    }

    [Fact]
    public void JogaNaPosicao_PrincipalESecundarias()
    {
        var jogador = Criar(secundarias: [Posicao.AlaDireito, Posicao.MeiaDireito]);

        Assert.True(jogador.JogaNaPosicao(Posicao.LateralDireito));
        Assert.True(jogador.JogaNaPosicao(Posicao.AlaDireito));
        Assert.True(jogador.JogaNaPosicao(Posicao.MeiaDireito));
        Assert.False(jogador.JogaNaPosicao(Posicao.LateralEsquerdo));
    }

    [Fact]
    public void Construtor_MaisDeDuasSecundarias_Lanca()
    {
        Assert.Throws<ArgumentException>(() =>
            Criar(secundarias: [Posicao.AlaDireito, Posicao.MeiaDireito, Posicao.PontaDireita]));
    }

    [Fact]
    public void Construtor_SecundariaIgualAPrincipal_Lanca()
    {
        Assert.Throws<ArgumentException>(() => Criar(secundarias: [Posicao.LateralDireito]));
    }

    [Fact]
    public void Construtor_SecundariasRepetidas_Lanca()
    {
        Assert.Throws<ArgumentException>(() => Criar(secundarias: [Posicao.AlaDireito, Posicao.AlaDireito]));
    }

    [Theory]
    [InlineData(14)]
    [InlineData(46)]
    public void Construtor_IdadeForaDaFaixa_Lanca(int idade)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Criar(idade: idade));
    }

    [Fact]
    public void Construtor_NomeVazio_Lanca()
    {
        Assert.Throws<ArgumentException>(() =>
            new Jogador(1, "  ", 20, Posicao.Volante, [], Atributos.Uniformes(10), OcultosPadrao));
    }

    [Fact]
    public void AjustarMoralECondicao_FicamEntreZeroECem()
    {
        var jogador = Criar();

        jogador.AjustarMoral(+500);
        jogador.AjustarCondicao(-500);

        Assert.Equal(100, jogador.Moral);
        Assert.Equal(0, jogador.Condicao);
    }

    [Fact]
    public void NovoJogador_ComecaDescansadoComMoralNeutraESemContrato()
    {
        var jogador = Criar();

        Assert.Equal(100, jogador.Condicao);
        Assert.Equal(50, jogador.Moral);
        Assert.Null(jogador.Contrato);
    }

    [Fact]
    public void AssinarEEncerrarContrato()
    {
        var jogador = Criar();

        jogador.AssinarContrato(new Contrato("porto-alegre-fc", 15_000m, 2028));
        Assert.Equal("porto-alegre-fc", jogador.Contrato!.ClubeId);

        jogador.EncerrarContrato();
        Assert.Null(jogador.Contrato);
    }

    [Fact]
    public void Envelhecer_SomaUmAno()
    {
        var jogador = Criar(idade: 30);

        jogador.Envelhecer();

        Assert.Equal(31, jogador.Idade);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(21)]
    public void Atributos_ForaDaEscala_Lanca(int valor)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Atributos.Uniformes(valor));
        Assert.Throws<ArgumentOutOfRangeException>(() => Atributos.Uniformes(10) with { Passe = valor });
    }

    [Fact]
    public void AtributosOcultos_PotencialForaDaEscala_Lanca()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => OcultosPadrao with { Potencial = 100 });
    }

    [Fact]
    public void Contrato_SalarioZero_Lanca()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Contrato("clube", 0m, 2027));
    }

    [Fact]
    public void Contrato_VenceAoFimDaTemporadaFinal()
    {
        var contrato = new Contrato("clube", 1_000m, 2027);

        Assert.False(contrato.VenceAoFimDe(2026));
        Assert.True(contrato.VenceAoFimDe(2027));
    }
}
