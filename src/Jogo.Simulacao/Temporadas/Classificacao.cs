using Jogo.Simulacao.Geracao;

namespace Jogo.Simulacao.Temporadas;

/// <summary>Placar de um jogo já disputado, com os cartões (que contam no desempate).</summary>
public sealed record PlacarJogo(
    JogoAgendado Jogo,
    int GolsMandante,
    int GolsVisitante,
    int AmarelosMandante = 0,
    int AmarelosVisitante = 0,
    int VermelhosMandante = 0,
    int VermelhosVisitante = 0);

/// <summary>Uma linha da tabela.</summary>
public sealed record LinhaTabela(
    int Posicao,
    string ClubeId,
    int Jogos,
    int Vitorias,
    int Empates,
    int Derrotas,
    int GolsPro,
    int GolsContra,
    int Amarelos,
    int Vermelhos)
{
    public int Pontos => (3 * Vitorias) + Empates;
    public int Saldo => GolsPro - GolsContra;

    /// <summary>Aproveitamento de 0 a 1.</summary>
    public double Aproveitamento => Jogos == 0 ? 0 : Pontos / (3.0 * Jogos);
}

/// <summary>
/// Classificação de pontos corridos com os critérios do Brasileirão:
/// pontos, vitórias, saldo, gols pró, confronto direto (só entre 2 clubes), menos vermelhos,
/// menos amarelos e sorteio (determinístico pela seed).
/// </summary>
public static class Classificacao
{
    public static IReadOnlyList<LinhaTabela> Calcular(IEnumerable<string> clubes, IEnumerable<PlacarJogo> placares, ulong seedSorteio = 0)
    {
        ArgumentNullException.ThrowIfNull(clubes);
        ArgumentNullException.ThrowIfNull(placares);

        var lista = placares.ToArray();
        var acumulado = clubes.ToDictionary(c => c, c => new Acumulado(c), StringComparer.Ordinal);

        foreach (var p in lista)
        {
            acumulado[p.Jogo.MandanteId].Somar(p.GolsMandante, p.GolsVisitante, p.AmarelosMandante, p.VermelhosMandante);
            acumulado[p.Jogo.VisitanteId].Somar(p.GolsVisitante, p.GolsMandante, p.AmarelosVisitante, p.VermelhosVisitante);
        }

        var ordenados = acumulado.Values
            .OrderByDescending(a => a.Pontos)
            .ThenByDescending(a => a.Vitorias)
            .ThenByDescending(a => a.GolsPro - a.GolsContra)
            .ThenByDescending(a => a.GolsPro)
            .ThenBy(a => a.Vermelhos)
            .ThenBy(a => a.Amarelos)
            .ThenBy(a => GeradorDeMundo.HashEstavel(a.ClubeId) ^ seedSorteio)
            .ToList();

        AplicarConfrontoDireto(ordenados, lista);

        return [.. ordenados.Select((a, i) => new LinhaTabela(
            i + 1, a.ClubeId, a.Jogos, a.Vitorias, a.Empates, a.Derrotas, a.GolsPro, a.GolsContra, a.Amarelos, a.Vermelhos))];
    }

    /// <summary>
    /// O confronto direto vem antes de cartões e sorteio, mas só vale quando exatamente 2 clubes
    /// empatam em pontos, vitórias, saldo e gols pró.
    /// </summary>
    private static void AplicarConfrontoDireto(List<Acumulado> ordem, PlacarJogo[] placares)
    {
        var i = 0;
        while (i < ordem.Count)
        {
            var fim = i + 1;
            while (fim < ordem.Count && ordem[fim].MesmosCriteriosPrincipais(ordem[i]))
            {
                fim++;
            }

            if (fim - i == 2)
            {
                var (a, b) = (ordem[i], ordem[i + 1]);
                var (pontosA, pontosB, saldoA) = (0, 0, 0);
                foreach (var p in placares)
                {
                    if (p.Jogo.MandanteId == a.ClubeId && p.Jogo.VisitanteId == b.ClubeId)
                    {
                        (pontosA, pontosB) = Somar(pontosA, pontosB, p.GolsMandante, p.GolsVisitante);
                        saldoA += p.GolsMandante - p.GolsVisitante;
                    }
                    else if (p.Jogo.MandanteId == b.ClubeId && p.Jogo.VisitanteId == a.ClubeId)
                    {
                        (pontosB, pontosA) = Somar(pontosB, pontosA, p.GolsMandante, p.GolsVisitante);
                        saldoA -= p.GolsMandante - p.GolsVisitante;
                    }
                }

                if (pontosB > pontosA || (pontosB == pontosA && saldoA < 0))
                {
                    (ordem[i], ordem[i + 1]) = (b, a);
                }
            }

            i = fim;
        }
    }

    private static (int, int) Somar(int pontosMandante, int pontosVisitante, int golsMandante, int golsVisitante) =>
        golsMandante > golsVisitante ? (pontosMandante + 3, pontosVisitante)
        : golsMandante < golsVisitante ? (pontosMandante, pontosVisitante + 3)
        : (pontosMandante + 1, pontosVisitante + 1);

    private sealed class Acumulado(string clubeId)
    {
        public string ClubeId { get; } = clubeId;
        public int Jogos { get; private set; }
        public int Vitorias { get; private set; }
        public int Empates { get; private set; }
        public int Derrotas { get; private set; }
        public int GolsPro { get; private set; }
        public int GolsContra { get; private set; }
        public int Amarelos { get; private set; }
        public int Vermelhos { get; private set; }
        public int Pontos => (3 * Vitorias) + Empates;

        public void Somar(int pro, int contra, int amarelos, int vermelhos)
        {
            Jogos++;
            GolsPro += pro;
            GolsContra += contra;
            Amarelos += amarelos;
            Vermelhos += vermelhos;
            if (pro > contra)
            {
                Vitorias++;
            }
            else if (pro == contra)
            {
                Empates++;
            }
            else
            {
                Derrotas++;
            }
        }

        public bool MesmosCriteriosPrincipais(Acumulado outro) =>
            Pontos == outro.Pontos && Vitorias == outro.Vitorias
            && GolsPro - GolsContra == outro.GolsPro - outro.GolsContra && GolsPro == outro.GolsPro;
    }
}
