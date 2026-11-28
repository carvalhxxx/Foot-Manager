using Jogo.Dominio.Aleatoriedade;
using Jogo.Dominio.Jogadores;
using Jogo.Motor;

namespace Jogo.Simulacao.Copas;

/// <summary>Uma cobrança: quem bateu, por qual equipe e se foi gol.</summary>
public sealed record CobrancaDePenalti(Equipe Equipe, int JogadorId, bool Gol);

/// <summary>
/// Disputa de pênaltis (D-040): 5 cobranças alternadas e, se empatar, alternadas até alguém errar.
/// Chance de gol = 75% + 1,5 ponto por ponto de Finalização do batedor acima dos Reflexos do goleiro
/// (entre 55% e 92%). Bate primeiro o cobrador de faltas, depois os melhores finalizadores.
/// Para quando a disputa já está decidida.
/// </summary>
public static class Penaltis
{
    public const int CobrancasIniciais = 5;

    public static (int Mandante, int Visitante, IReadOnlyList<CobrancaDePenalti> Cobrancas) Disputar(
        IReadOnlyList<Jogador> mandante, int? cobradorMandante,
        IReadOnlyList<Jogador> visitante, int? cobradorVisitante,
        IGeradorAleatorio aleatorio)
    {
        ArgumentNullException.ThrowIfNull(mandante);
        ArgumentNullException.ThrowIfNull(visitante);
        ArgumentNullException.ThrowIfNull(aleatorio);

        var batedoresM = Ordem(mandante, cobradorMandante);
        var batedoresV = Ordem(visitante, cobradorVisitante);
        var goleiroM = Goleiro(mandante);
        var goleiroV = Goleiro(visitante);
        var cobrancas = new List<CobrancaDePenalti>();
        int golsM = 0, golsV = 0;

        for (var rodada = 0; ; rodada++)
        {
            var batedorM = batedoresM[rodada % batedoresM.Count];
            var golM = aleatorio.Chance(Chance(batedorM, goleiroV));
            golsM += golM ? 1 : 0;
            cobrancas.Add(new(Equipe.Mandante, batedorM.Id, golM));
            if (rodada < CobrancasIniciais && Decidida(golsM, golsV, rodada + 1, rodada))
            {
                break;
            }

            var batedorV = batedoresV[rodada % batedoresV.Count];
            var golV = aleatorio.Chance(Chance(batedorV, goleiroM));
            golsV += golV ? 1 : 0;
            cobrancas.Add(new(Equipe.Visitante, batedorV.Id, golV));

            var feitas = rodada + 1;
            if (feitas < CobrancasIniciais ? Decidida(golsM, golsV, feitas, feitas) : golsM != golsV)
            {
                break;
            }
        }

        return (golsM, golsV, cobrancas);
    }

    /// <summary>Probabilidade de converter: Finalização do batedor contra Reflexos do goleiro.</summary>
    public static double Chance(Jogador batedor, Jogador? goleiro) =>
        Math.Clamp(0.75 + (0.015 * (batedor.Atributos.Finalizacao - (goleiro?.Atributos.Reflexos ?? 8))), 0.55, 0.92);

    /// <summary>Alguém já não alcança o outro nas cobranças que restam (das 5 iniciais)?</summary>
    private static bool Decidida(int golsM, int golsV, int batidasM, int batidasV)
    {
        var restamM = CobrancasIniciais - batidasM;
        var restamV = CobrancasIniciais - batidasV;
        return golsM + restamM < golsV || golsV + restamV < golsM;
    }

    private static List<Jogador> Ordem(IReadOnlyList<Jogador> time, int? cobrador)
    {
        var deLinha = time.Where(j => j.PosicaoPrincipal != Posicao.Goleiro).ToList();
        var lista = deLinha.Count > 0 ? deLinha : [.. time];
        return [.. lista.OrderByDescending(j => j.Id == cobrador).ThenByDescending(j => j.Atributos.Finalizacao).ThenBy(j => j.Id)];
    }

    private static Jogador? Goleiro(IReadOnlyList<Jogador> time) =>
        time.FirstOrDefault(j => j.PosicaoPrincipal == Posicao.Goleiro) ?? time.FirstOrDefault();
}
