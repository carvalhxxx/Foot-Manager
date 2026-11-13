using Jogo.Dominio.Aleatoriedade;

namespace Jogo.Dominio.Testes.Aleatoriedade;

public class GeradorAleatorioTestes
{
    [Fact]
    public void MesmaSeed_GeraMesmaSequencia()
    {
        var a = new GeradorAleatorio(123);
        var b = new GeradorAleatorio(123);

        for (var i = 0; i < 1_000; i++)
        {
            Assert.Equal(a.Proximo(0, 100), b.Proximo(0, 100));
        }
    }

    [Fact]
    public void SeedsDiferentes_GeramSequenciasDiferentes()
    {
        var a = new GeradorAleatorio(1);
        var b = new GeradorAleatorio(2);

        var sequenciaA = Enumerable.Range(0, 20).Select(_ => a.Proximo(0, 1_000_000)).ToArray();
        var sequenciaB = Enumerable.Range(0, 20).Select(_ => b.Proximo(0, 1_000_000)).ToArray();

        Assert.NotEqual(sequenciaA, sequenciaB);
    }

    /// <summary>
    /// Trava de regressão: se este teste quebrar, saves e replays antigos deixam de reproduzir.
    /// Só altere os valores esperados com uma decisão registrada em docs/DECISOES.md.
    /// </summary>
    [Fact]
    public void SeedFixa_SequenciaNuncaMuda()
    {
        var gerador = new GeradorAleatorio(2026);

        var sequencia = Enumerable.Range(0, 5).Select(_ => gerador.Proximo(0, 1_000_000)).ToArray();

        Assert.Equal([573731, 283679, 812509, 893674, 812035], sequencia);
    }

    [Fact]
    public void SplitMix64_SeedZero_PrimeiroValorDeReferencia()
    {
        ulong x = 0;

        Assert.Equal(0xE220A8397B1DCDAFUL, GeradorAleatorio.SplitMix64(ref x));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 21)]
    [InlineData(-10, 10)]
    [InlineData(int.MinValue, int.MaxValue)]
    public void Proximo_SempreDentroDaFaixa(int minimo, int maximoExclusivo)
    {
        var gerador = new GeradorAleatorio(7);

        for (var i = 0; i < 10_000; i++)
        {
            var valor = gerador.Proximo(minimo, maximoExclusivo);
            Assert.InRange(valor, minimo, maximoExclusivo - 1);
        }
    }

    [Theory]
    [InlineData(5, 5)]
    [InlineData(5, 4)]
    public void Proximo_FaixaVaziaOuInvertida_Lanca(int minimo, int maximoExclusivo)
    {
        var gerador = new GeradorAleatorio(7);

        Assert.Throws<ArgumentOutOfRangeException>(() => gerador.Proximo(minimo, maximoExclusivo));
    }

    [Fact]
    public void Proximo_DistribuicaoAproximadamenteUniforme()
    {
        const int Faces = 6;
        const int Sorteios = 60_000;
        var gerador = new GeradorAleatorio(99);
        var contagem = new int[Faces];

        for (var i = 0; i < Sorteios; i++)
        {
            contagem[gerador.Proximo(0, Faces)]++;
        }

        // Cada face deve ficar a menos de 5% da frequência esperada (10.000).
        Assert.All(contagem, c => Assert.InRange(c, 9_500, 10_500));
    }

    [Fact]
    public void ProximoReal_SempreEntreZeroEUm()
    {
        var gerador = new GeradorAleatorio(5);

        for (var i = 0; i < 10_000; i++)
        {
            Assert.InRange(gerador.ProximoReal(), 0.0, 0.9999999999999999);
        }
    }

    [Fact]
    public void Chance_ZeroNuncaAcontece_UmSempreAcontece()
    {
        var gerador = new GeradorAleatorio(5);

        for (var i = 0; i < 1_000; i++)
        {
            Assert.False(gerador.Chance(0));
            Assert.True(gerador.Chance(1));
        }
    }

    [Fact]
    public void Chance_Nan_Lanca()
    {
        var gerador = new GeradorAleatorio(5);

        Assert.Throws<ArgumentOutOfRangeException>(() => gerador.Chance(double.NaN));
    }

    [Fact]
    public void CapturarEstado_PermiteRetomarExatamenteDeOndeParou()
    {
        var original = new GeradorAleatorio(314);
        for (var i = 0; i < 37; i++)
        {
            original.ProximoReal();
        }

        var retomado = new GeradorAleatorio(original.CapturarEstado());

        for (var i = 0; i < 100; i++)
        {
            Assert.Equal(original.Proximo(0, 1_000), retomado.Proximo(0, 1_000));
        }
    }

    [Fact]
    public void EstadoZerado_Lanca()
    {
        Assert.Throws<ArgumentException>(() => new GeradorAleatorio(new EstadoGerador(0, 0, 0, 0)));
    }
}
