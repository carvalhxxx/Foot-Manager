using Jogo.Dominio.Jogadores;

namespace Jogo.Dominio.Testes.Jogadores;

public class CalculadoraForcaTestes
{
    public static TheoryData<Posicao> TodasAsPosicoes => [.. PosicaoExtensoes.Todas];

    [Theory]
    [MemberData(nameof(TodasAsPosicoes))]
    public void Pesos_DeCadaPosicao_SomamCem(Posicao posicao)
    {
        var soma = Enum.GetValues<TipoAtributo>().Sum(a => CalculadoraForca.Peso(posicao, a));

        Assert.Equal(100, soma);
    }

    [Theory]
    [MemberData(nameof(TodasAsPosicoes))]
    public void Calcular_AtributosNoMinimoENoMaximo_DaUmENoventaENove(Posicao posicao)
    {
        Assert.Equal(1, CalculadoraForca.Calcular(Atributos.Uniformes(1), posicao));
        Assert.Equal(99, CalculadoraForca.Calcular(Atributos.Uniformes(20), posicao));
    }

    /// <summary>Exemplos da tabela aprovada (média 10 → 47, 13 → 63, 15 → 73, 18 → 89).</summary>
    [Theory]
    [InlineData(10, 47)]
    [InlineData(13, 63)]
    [InlineData(15, 73)]
    [InlineData(18, 89)]
    public void Calcular_AtributosUniformes_SegueATabelaDeConversao(int valor, int forcaEsperada)
    {
        Assert.Equal(forcaEsperada, CalculadoraForca.Calcular(Atributos.Uniformes(valor), Posicao.Centroavante));
    }

    [Theory]
    [MemberData(nameof(TodasAsPosicoes))]
    public void Calcular_SubirQualquerAtributo_NuncaDiminuiAForca(Posicao posicao)
    {
        var base10 = Atributos.Uniformes(10);
        var forcaBase = CalculadoraForca.Calcular(base10, posicao);

        foreach (var atributo in Enum.GetValues<TipoAtributo>())
        {
            var melhorado = Com(base10, atributo, 20);
            Assert.True(CalculadoraForca.Calcular(melhorado, posicao) >= forcaBase, $"{atributo} em {posicao}");
        }
    }

    [Fact]
    public void Calcular_LadosDaMesmaFuncao_TemAMesmaForca()
    {
        var atributos = Atributos.Uniformes(10) with { Velocidade = 18, Drible = 16 };

        Assert.Equal(
            CalculadoraForca.Calcular(atributos, Posicao.PontaDireita),
            CalculadoraForca.Calcular(atributos, Posicao.PontaEsquerda));
        Assert.Equal(
            CalculadoraForca.Calcular(atributos, Posicao.LateralDireito),
            CalculadoraForca.Calcular(atributos, Posicao.LateralEsquerdo));
    }

    [Fact]
    public void Calcular_Artilheiro_ValeMaisNoAtaqueQueNaZaga()
    {
        var artilheiro = Atributos.Uniformes(8) with { Finalizacao = 19, Velocidade = 16, Decisao = 15 };

        Assert.True(
            CalculadoraForca.Calcular(artilheiro, Posicao.Centroavante)
            > CalculadoraForca.Calcular(artilheiro, Posicao.Zagueiro) + 15);
    }

    [Fact]
    public void Calcular_Goleiro_UsaReflexosEPosicionamento()
    {
        var goleiro = Atributos.Uniformes(3) with { Reflexos = 18, Posicionamento = 18, Decisao = 14 };

        Assert.True(CalculadoraForca.Calcular(goleiro, Posicao.Goleiro) >= 70);
        Assert.True(CalculadoraForca.Calcular(goleiro, Posicao.Centroavante) <= 25);
    }

    /// <summary>Trava de regressão de um perfil completo: só muda com nova decisão em DECISOES.md.</summary>
    [Fact]
    public void Calcular_PerfilDeReferencia_ValoresTravados()
    {
        var meia = new Atributos
        {
            Finalizacao = 12,
            Passe = 17,
            Desarme = 8,
            Drible = 15,
            Velocidade = 11,
            Resistencia = 13,
            Decisao = 16,
            Determinacao = 12,
            Reflexos = 2,
            Posicionamento = 3,
        };

        // MC: (5·12 + 30·17 + 10·8 + 10·15 + 15·13 + 20·16 + 10·12) = 1435 → 1 + (1335·98 + 950)/1900 = 70
        Assert.Equal(70, CalculadoraForca.Calcular(meia, Posicao.MeiaCentral));
        // MO: (20·12 + 25·17 + 20·15 + 5·11 + 5·13 + 20·16 + 5·12) = 1465 → 1 + (1365·98 + 950)/1900 = 71
        Assert.Equal(71, CalculadoraForca.Calcular(meia, Posicao.MeiaOfensivo));
    }

    private static Atributos Com(Atributos a, TipoAtributo tipo, int valor) => tipo switch
    {
        TipoAtributo.Finalizacao => a with { Finalizacao = valor },
        TipoAtributo.Passe => a with { Passe = valor },
        TipoAtributo.Desarme => a with { Desarme = valor },
        TipoAtributo.Drible => a with { Drible = valor },
        TipoAtributo.Velocidade => a with { Velocidade = valor },
        TipoAtributo.Resistencia => a with { Resistencia = valor },
        TipoAtributo.Decisao => a with { Decisao = valor },
        TipoAtributo.Determinacao => a with { Determinacao = valor },
        TipoAtributo.Reflexos => a with { Reflexos = valor },
        TipoAtributo.Posicionamento => a with { Posicionamento = valor },
        _ => throw new ArgumentOutOfRangeException(nameof(tipo)),
    };
}
