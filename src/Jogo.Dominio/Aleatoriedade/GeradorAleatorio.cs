using System.Numerics;

namespace Jogo.Dominio.Aleatoriedade;

/// <summary>
/// Gerador determinístico baseado no algoritmo xoshiro256** (semeado com SplitMix64).
/// Implementado aqui em vez de usar <see cref="System.Random"/> porque o algoritmo do
/// Random não tem garantia de ser o mesmo entre versões do .NET, e saves/replays
/// precisam reproduzir exatamente a mesma sequência para sempre.
/// </summary>
/// <remarks>Não é thread-safe: cada simulação usa a sua própria instância.</remarks>
public sealed class GeradorAleatorio : IGeradorAleatorio
{
    private const double UmSobre2Elevado53 = 1.0 / (1UL << 53);

    private ulong _s0;
    private ulong _s1;
    private ulong _s2;
    private ulong _s3;

    public GeradorAleatorio(ulong seed)
    {
        var x = seed;
        _s0 = SplitMix64(ref x);
        _s1 = SplitMix64(ref x);
        _s2 = SplitMix64(ref x);
        _s3 = SplitMix64(ref x);
    }

    /// <summary>Retoma um gerador a partir de um estado salvo.</summary>
    public GeradorAleatorio(EstadoGerador estado)
    {
        if (estado is { S0: 0, S1: 0, S2: 0, S3: 0 })
        {
            throw new ArgumentException("Estado todo zerado é inválido para o xoshiro256**.", nameof(estado));
        }

        (_s0, _s1, _s2, _s3) = (estado.S0, estado.S1, estado.S2, estado.S3);
    }

    public int Proximo(int minimo, int maximoExclusivo)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(maximoExclusivo, minimo);

        // Método de Lemire: sorteio inteiro sem viés de módulo.
        var faixa = (ulong)((long)maximoExclusivo - minimo);
        var alto = Math.BigMul(ProximoUInt64(), faixa, out var baixo);
        if (baixo < faixa)
        {
            var limite = unchecked(0UL - faixa) % faixa;
            while (baixo < limite)
            {
                alto = Math.BigMul(ProximoUInt64(), faixa, out baixo);
            }
        }

        return (int)(minimo + (long)alto);
    }

    public double ProximoReal() => (ProximoUInt64() >> 11) * UmSobre2Elevado53;

    public bool Chance(double probabilidade)
    {
        if (double.IsNaN(probabilidade))
        {
            throw new ArgumentOutOfRangeException(nameof(probabilidade), "Probabilidade não pode ser NaN.");
        }

        return ProximoReal() < probabilidade;
    }

    public EstadoGerador CapturarEstado() => new(_s0, _s1, _s2, _s3);

    internal ulong ProximoUInt64()
    {
        var resultado = BitOperations.RotateLeft(_s1 * 5, 7) * 9;
        var t = _s1 << 17;

        _s2 ^= _s0;
        _s3 ^= _s1;
        _s1 ^= _s2;
        _s0 ^= _s3;
        _s2 ^= t;
        _s3 = BitOperations.RotateLeft(_s3, 45);

        return resultado;
    }

    internal static ulong SplitMix64(ref ulong x)
    {
        var z = x += 0x9E3779B97F4A7C15UL;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }
}
