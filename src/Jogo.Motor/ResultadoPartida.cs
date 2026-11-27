namespace Jogo.Motor;

/// <summary>O que cada jogador levou da partida (aplicado ao elenco pela simulação).</summary>
public sealed record ParticipacaoJogador(
    int JogadorId,
    Equipe Equipe,
    int MinutosJogados,
    int CondicaoFinal,
    int Gols,
    int Amarelos,
    bool Expulso,
    int RodadasDeLesao);

/// <summary>Resultado final de uma partida.</summary>
public sealed record ResultadoPartida(
    string MandanteId,
    string VisitanteId,
    int GolsMandante,
    int GolsVisitante,
    IReadOnlyList<EventoPartida> Eventos,
    EstatisticasTime EstatisticasMandante,
    EstatisticasTime EstatisticasVisitante,
    IReadOnlyList<ParticipacaoJogador> Participacoes)
{
    /// <summary>Posse de bola do mandante, de 0 a 100.</summary>
    public int PosseMandante
    {
        get
        {
            var total = EstatisticasMandante.MinutosComPosse + EstatisticasVisitante.MinutosComPosse;
            return total == 0 ? 50 : (int)Math.Round(100.0 * EstatisticasMandante.MinutosComPosse / total);
        }
    }

    public static ResultadoPartida De(Partida partida)
    {
        ArgumentNullException.ThrowIfNull(partida);
        if (!partida.Encerrada)
        {
            throw new InvalidOperationException("A partida ainda não terminou.");
        }

        var participacoes = new[] { partida.Mandante, partida.Visitante }
            .SelectMany(t => t.Participantes.Select(j => new ParticipacaoJogador(
                j.Jogador.Id,
                t.Equipe,
                j.MinutosJogados(partida.MinutosCorridos),
                (int)Math.Round(j.Condicao),
                j.Gols,
                j.Amarelos,
                j.Expulso,
                j.RodadasDeLesao)))
            .ToArray();

        return new ResultadoPartida(
            partida.Mandante.ClubeId,
            partida.Visitante.ClubeId,
            partida.Mandante.Estatisticas.Gols,
            partida.Visitante.Estatisticas.Gols,
            partida.Eventos,
            partida.Mandante.Estatisticas,
            partida.Visitante.Estatisticas,
            participacoes);
    }
}
