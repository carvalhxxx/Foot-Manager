namespace Jogo.Motor;

/// <summary>Uma decisão do treinador durante a partida.</summary>
public abstract record Decisao;

/// <summary>Troca um jogador em campo (ou um lesionado que já saiu) por um reserva, que assume a mesma posição.</summary>
public sealed record Substituicao(int SaiId, int EntraId) : Decisao;

public sealed record TrocaDeMentalidade(Mentalidade Nova) : Decisao;

/// <summary>Muda a marcação e/ou a concentração dos ataques.</summary>
public sealed record TrocaDeEstilo(Marcacao Marcacao, Concentracao Concentracao) : Decisao;

/// <summary>
/// Muda a formação. <paramref name="JogadoresNaOrdem"/> são os ids dos jogadores de linha em campo,
/// na ordem das posições da nova formação.
/// </summary>
public sealed record TrocaDeFormacao(Formacao Nova, IReadOnlyList<int> JogadoresNaOrdem) : Decisao;
