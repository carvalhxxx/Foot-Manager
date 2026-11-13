namespace Jogo.Dominio.Jogadores;

/// <summary>
/// Calcula a força geral (1-99) a partir dos atributos (1-20) ponderados pela posição.
/// Fórmula: média ponderada m (1-20) → força = 1 + (m − 1) × 98/19, arredondada.
/// Tudo em aritmética inteira para ser idêntico em qualquer máquina.
/// Tabela aprovada em docs/DECISOES.md (D-015).
/// </summary>
public static class CalculadoraForca
{
    public const int ForcaMinima = 1;
    public const int ForcaMaxima = 99;
    private const int SomaDosPesos = 100;

    // Ordem das colunas: Finalização, Passe, Desarme, Drible, Velocidade, Resistência,
    // Decisão, Determinação, Reflexos, Posicionamento. Cada linha soma 100.
    private static readonly Dictionary<Posicao, int[]> PesosPorFuncao = new()
    {
        [Posicao.Goleiro] =         [0, 5, 0, 0, 5, 0, 15, 10, 35, 30],
        [Posicao.Zagueiro] =        [0, 10, 35, 5, 10, 10, 15, 15, 0, 0],
        [Posicao.LateralDireito] =  [0, 15, 20, 10, 20, 15, 10, 10, 0, 0],
        [Posicao.AlaDireito] =      [0, 15, 10, 15, 20, 20, 10, 10, 0, 0],
        [Posicao.Volante] =         [0, 20, 25, 5, 5, 15, 15, 15, 0, 0],
        [Posicao.MeiaCentral] =     [5, 30, 10, 10, 0, 15, 20, 10, 0, 0],
        [Posicao.MeiaDireito] =     [5, 20, 10, 15, 15, 15, 10, 10, 0, 0],
        [Posicao.MeiaOfensivo] =    [20, 25, 0, 20, 5, 5, 20, 5, 0, 0],
        [Posicao.PontaDireita] =    [15, 15, 0, 25, 25, 5, 10, 5, 0, 0],
        [Posicao.SegundoAtacante] = [25, 15, 0, 20, 15, 5, 15, 5, 0, 0],
        [Posicao.Centroavante] =    [35, 5, 0, 10, 15, 10, 15, 10, 0, 0],
    };

    private static readonly TipoAtributo[] Colunas = Enum.GetValues<TipoAtributo>();

    /// <summary>Peso (0-100) do atributo na força da posição.</summary>
    public static int Peso(Posicao posicao, TipoAtributo atributo) =>
        PesosPorFuncao[posicao.Funcao()][(int)atributo];

    public static int Calcular(Atributos atributos, Posicao posicao)
    {
        ArgumentNullException.ThrowIfNull(atributos);

        var pesos = PesosPorFuncao[posicao.Funcao()];
        var somaPonderada = 0;
        for (var i = 0; i < Colunas.Length; i++)
        {
            somaPonderada += pesos[i] * atributos[Colunas[i]];
        }

        // somaPonderada vai de 100 (tudo 1) a 2000 (tudo 20).
        // força = 1 + (soma/100 − 1) × 98/19 = 1 + (soma − 100) × 98 / 1900, com arredondamento.
        const int Denominador = SomaDosPesos * (Atributos.Maximo - Atributos.Minimo);
        var numerador = (somaPonderada - SomaDosPesos) * (ForcaMaxima - ForcaMinima);
        return ForcaMinima + ((numerador + (Denominador / 2)) / Denominador);
    }
}
