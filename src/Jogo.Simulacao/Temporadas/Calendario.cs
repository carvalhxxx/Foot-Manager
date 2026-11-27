using Jogo.Dominio.Aleatoriedade;

namespace Jogo.Simulacao.Temporadas;

/// <summary>Um jogo marcado no calendário.</summary>
public sealed record JogoAgendado(int Rodada, string MandanteId, string VisitanteId);

/// <summary>Monta calendários de competições.</summary>
public static class Calendario
{
    /// <summary>
    /// Pontos corridos em turno e returno (todos contra todos, ida e volta).
    /// Turno pela fatoração canônica (método do círculo), com mandos alternados; o returno repete o turno
    /// na mesma ordem com mandos invertidos.
    /// A ordem dos clubes é sorteada pela seed, para cada temporada ter um calendário diferente.
    /// </summary>
    /// <returns>Lista de rodadas; cada rodada tem N/2 jogos.</returns>
    public static IReadOnlyList<IReadOnlyList<JogoAgendado>> PontosCorridos(IReadOnlyList<string> clubes, IGeradorAleatorio aleatorio)
    {
        ArgumentNullException.ThrowIfNull(clubes);
        ArgumentNullException.ThrowIfNull(aleatorio);
        if (clubes.Count < 2 || clubes.Count % 2 != 0)
        {
            throw new ArgumentException("O calendário precisa de um número par de clubes (mínimo 2).", nameof(clubes));
        }

        // Embaralha (Fisher-Yates) para o calendário mudar a cada temporada.
        var ordem = clubes.ToArray();
        for (var i = ordem.Length - 1; i > 0; i--)
        {
            var j = aleatorio.Proximo(0, i + 1);
            (ordem[i], ordem[j]) = (ordem[j], ordem[i]);
        }

        var n = ordem.Length;
        var rodadasPorTurno = n - 1;
        var turno = new List<IReadOnlyList<JogoAgendado>>(rodadasPorTurno);

        // Fatoração canônica (de Werra): o último clube é fixo; na rodada r ele enfrenta o clube r,
        // e os demais formam os pares (r + i, r − i). O mando segue a paridade de i (e de r para o fixo),
        // o que dá o menor número possível de jogos seguidos no mesmo mando.
        var fixo = ordem[n - 1];
        for (var r = 0; r < rodadasPorTurno; r++)
        {
            var jogos = new List<JogoAgendado>(n / 2);
            var rodada = r + 1;

            jogos.Add(r % 2 == 0
                ? new JogoAgendado(rodada, ordem[r], fixo)
                : new JogoAgendado(rodada, fixo, ordem[r]));

            for (var i = 1; i < n / 2; i++)
            {
                var a = ordem[(r + i) % rodadasPorTurno];
                var b = ordem[((r - i) % rodadasPorTurno + rodadasPorTurno) % rodadasPorTurno];
                jogos.Add(i % 2 == 1 ? new JogoAgendado(rodada, a, b) : new JogoAgendado(rodada, b, a));
            }

            turno.Add(jogos);
        }

        var returno = turno.Select((jogos, r) => (IReadOnlyList<JogoAgendado>)jogos
            .Select(j => new JogoAgendado(rodadasPorTurno + r + 1, j.VisitanteId, j.MandanteId))
            .ToArray());

        return [.. turno, .. returno];
    }
}
