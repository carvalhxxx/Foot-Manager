namespace Jogo.Simulacao.Temporadas;

/// <summary>Regras do que acontece com o elenco entre uma rodada e outra.</summary>
public sealed record RegrasTemporada
{
    public static RegrasTemporada Padrao { get; } = new();

    /// <summary>Amarelos que geram 1 jogo de suspensão (regra do Brasileirão).</summary>
    public int LimiteAmarelos { get; init; } = 3;

    /// <summary>Jogos de suspensão por cartão vermelho.</summary>
    public int SuspensaoPorVermelho { get; init; } = 1;

    /// <summary>Condição recuperada entre rodadas: base + metade da Resistência (Resistência 10 → +20).</summary>
    public int RecuperacaoBase { get; init; } = 15;

    // Moral (0-100) ---------------------------------------------------------

    public int MoralVitoria { get; init; } = 3;
    public int MoralEmpate { get; init; } = 0;
    public int MoralDerrota { get; init; } = -3;
    public int MoralJogou { get; init; } = 1;
    public int MoralNaoJogou { get; init; } = -1;

    /// <summary>Fração da distância até a moral neutra (50) recuperada a cada rodada.</summary>
    public double RegressaoMoral { get; init; } = 0.1;

    // Entrosamento ----------------------------------------------------------

    /// <summary>Bônus máximo de força quando todos os titulares jogam juntos há várias rodadas.</summary>
    public double EntrosamentoMaximo { get; init; } = 0.03;

    /// <summary>Rodadas seguidas como titular para o jogador contar 100% no entrosamento.</summary>
    public int RodadasParaEntrosamentoMaximo { get; init; } = 5;
}
