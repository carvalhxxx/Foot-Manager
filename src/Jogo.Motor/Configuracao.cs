namespace Jogo.Motor;

/// <summary>
/// Condições do jogo além dos times. No MVP só o mando; na Fase 2 entram gramado, clima e torcida (D-014).
/// </summary>
public sealed record CondicoesPartida(bool CampoNeutro = false)
{
    public static CondicoesPartida Padrao { get; } = new();
}

/// <summary>
/// Parâmetros de equilíbrio do motor. Os valores padrão saem da calibração (tools/Jogo.Calibracao);
/// mudar qualquer um exige rodar a calibração de novo.
/// </summary>
public sealed record ConfiguracaoMotor
{
    public static ConfiguracaoMotor Padrao { get; } = new();

    // Setores ---------------------------------------------------------------

    /// <summary>Retorno decrescente de colocar mais gente num setor (0 = número não importa, 1 = linear).</summary>
    public double ExpoenteOcupacao { get; init; } = 0.5;

    /// <summary>Bônus de força do mandante em todos os setores.</summary>
    public double BonusMando { get; init; } = 0.05;

    // Posse e lances --------------------------------------------------------

    /// <summary>Quanto a diferença de meio-campo pesa na posse (expoente da disputa).</summary>
    public double ExpoentePosse { get; init; } = 1.5;

    /// <summary>Chance, a cada minuto, de o time com a bola criar um lance de perigo.</summary>
    public double ChanceDeLance { get; init; } = 0.42;

    /// <summary>Quanto a diferença ataque × defesa pesa no duelo.</summary>
    public double ExpoenteDuelo { get; init; } = 2.0;

    // Finalização -----------------------------------------------------------

    public double ChanceNoAlvoBase { get; init; } = 0.34;

    /// <summary>Chance de gol num chute no alvo quando atacante e goleiro têm o mesmo nível.</summary>
    public double ChanceGolNoAlvoBase { get; init; } = 0.285;

    public double ExpoenteGol { get; init; } = 1.2;

    /// <summary>Multiplicador da chance de gol para o time que já vence por 3+ (reaplicado a cada gol a mais).</summary>
    public double TravaGoleada { get; init; } = 0.75;

    // Lances secundários (quando o ataque perde o duelo) ---------------------

    public double ChanceEscanteio { get; init; } = 0.33;
    public double ChanceFalta { get; init; } = 0.30;
    public double ChanceFinalizacaoDeEscanteio { get; init; } = 0.25;
    public double ChanceFinalizacaoDeFalta { get; init; } = 0.15;

    // Disciplina e físico ---------------------------------------------------

    /// <summary>Chance, a cada minuto, de uma falta "de meio de campo" (fora dos lances de perigo).</summary>
    public double ChanceFaltaPorMinuto { get; init; } = 0.17;

    public double ChanceAmareloPorFalta { get; init; } = 0.17;
    public double ChanceVermelhoDiretoPorFalta { get; init; } = 0.005;

    /// <summary>Multiplicador da chance de amarelo para quem já tem um (segundo amarelo é mais raro).</summary>
    public double CuidadoDeQuemTemAmarelo { get; init; } = 0.4;

    /// <summary>Chance de lesão por jogador por minuto, para propensão 10 e condição 100.</summary>
    public double ChanceLesaoPorMinuto { get; init; } = 0.00022;

    /// <summary>Pontos de condição perdidos por minuto por um jogador com Resistência 10.</summary>
    public double DesgastePorMinuto { get; init; } = 0.30;

    // Mentalidade (por nível, de −2 a +2) -----------------------------------

    public double MentalidadeAtaque { get; init; } = 0.07;
    public double MentalidadeDefesa { get; init; } = 0.05;
    public double MentalidadeLances { get; init; } = 0.04;

    // Modificadores individuais ---------------------------------------------

    /// <summary>Variação máxima de rendimento pela moral (moral 0 = −x, moral 100 = +x).</summary>
    public double EfeitoMoral { get; init; } = 0.06;

    /// <summary>Rendimento de um jogador sem nenhuma condição física (cresce linearmente até 1 com condição 100).</summary>
    public double RendimentoSemCondicao { get; init; } = 0.75;

    public int MaximoSubstituicoes { get; init; } = 5;
}
