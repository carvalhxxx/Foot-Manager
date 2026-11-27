namespace Jogo.Motor;

public enum Equipe
{
    Mandante,
    Visitante,
}

public enum TipoEvento
{
    InicioDeJogo,
    Gol,
    FinalizacaoDefendida,
    FinalizacaoNaTrave,
    FinalizacaoNoTravessao,
    FinalizacaoParaFora,
    FinalizacaoPorCima,
    Escanteio,
    Falta,
    CartaoAmarelo,
    CartaoVermelho,
    Lesao,
    Substituicao,
    TrocaDeMentalidade,
    TrocaDeFormacao,
    Intervalo,
    FimDeJogo,
}

/// <summary>De onde saiu a finalização (para a narração e as estatísticas).</summary>
public enum OrigemLance
{
    JogadaTrabalhada,
    Escanteio,
    Falta,
}

/// <summary>
/// Algo que aconteceu na partida. <see cref="Minuto"/> é o minuto do relógio (1-90) e
/// <see cref="Acrescimo"/> os minutos extras (ex.: 45+2 → Minuto 45, Acréscimo 2).
/// </summary>
/// <param name="JogadorId">Protagonista (autor do gol, quem finalizou, quem levou o cartão, quem saiu).</param>
/// <param name="OutroJogadorId">Coadjuvante (goleiro que defendeu, quem entrou na substituição).</param>
public sealed record EventoPartida(
    int Minuto,
    int Acrescimo,
    TipoEvento Tipo,
    Equipe? Equipe = null,
    int? JogadorId = null,
    int? OutroJogadorId = null,
    OrigemLance? Origem = null,
    string? Detalhe = null);

/// <summary>Números de um time na partida.</summary>
public sealed class EstatisticasTime
{
    public int Gols { get; internal set; }
    public int MinutosComPosse { get; internal set; }
    public int Finalizacoes { get; internal set; }
    public int NoAlvo { get; internal set; }
    public int Escanteios { get; internal set; }
    public int Faltas { get; internal set; }
    public int Amarelos { get; internal set; }
    public int Vermelhos { get; internal set; }
}
