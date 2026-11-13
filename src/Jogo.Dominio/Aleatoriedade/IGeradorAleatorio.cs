namespace Jogo.Dominio.Aleatoriedade;

/// <summary>
/// Fonte única de sorteio do jogo (o "IRandom" do documento de projeto).
/// Todo sorteio do motor e da simulação passa por aqui, recebido por injeção.
/// Mesma seed + mesmo estado = mesmo resultado.
/// </summary>
public interface IGeradorAleatorio
{
    /// <summary>Inteiro uniforme em [<paramref name="minimo"/>, <paramref name="maximoExclusivo"/>).</summary>
    int Proximo(int minimo, int maximoExclusivo);

    /// <summary>Número real uniforme em [0, 1).</summary>
    double ProximoReal();

    /// <summary>Retorna verdadeiro com a probabilidade informada (0 a 1).</summary>
    bool Chance(double probabilidade);

    /// <summary>Fotografia do estado interno, para salvar e retomar a sequência exatamente de onde parou.</summary>
    EstadoGerador CapturarEstado();
}

/// <summary>Estado interno serializável do gerador.</summary>
public readonly record struct EstadoGerador(ulong S0, ulong S1, ulong S2, ulong S3);
