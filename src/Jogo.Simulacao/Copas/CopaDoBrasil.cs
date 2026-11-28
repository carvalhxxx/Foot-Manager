using Jogo.Dominio;
using Jogo.Dominio.Aleatoriedade;
using Jogo.Motor;
using Jogo.Simulacao.Geracao;

namespace Jogo.Simulacao.Copas;

public enum FaseCopa
{
    PrimeiraFase,
    SegundaFase,
    Oitavas,
    Quartas,
    Semifinal,
    Final,
}

/// <summary>
/// Um jogo da Copa. Pênaltis só no jogo que decide (jogo único ou volta empatada no agregado).
/// O resultado completo do motor só existe em memória (o save guarda o placar).
/// </summary>
public sealed record JogoCopa(
    FaseCopa Fase, int Confronto, int Perna, int Rodada, string MandanteId, string VisitanteId,
    int GolsMandante, int GolsVisitante, int? PenaltisMandante = null, int? PenaltisVisitante = null,
    ResultadoPartida? Resultado = null);

/// <summary>Um confronto: <see cref="ClubeA"/> manda o primeiro jogo (ou o único).</summary>
public sealed class ConfrontoCopa
{
    internal ConfrontoCopa(FaseCopa fase, int indice, string clubeA, string clubeB)
    {
        Fase = fase;
        Indice = indice;
        ClubeA = clubeA;
        ClubeB = clubeB;
    }

    public FaseCopa Fase { get; }
    public int Indice { get; }
    public string ClubeA { get; }
    public string ClubeB { get; }
    public JogoCopa? Ida { get; internal set; }
    public JogoCopa? Volta { get; internal set; }

    public bool JogoUnico => CopaDoBrasil.JogoUnico(Fase);

    /// <summary>Gols de A e de B somados nos jogos já disputados.</summary>
    public (int A, int B) Agregado =>
        ((Ida?.GolsMandante ?? 0) + (Volta?.GolsVisitante ?? 0), (Ida?.GolsVisitante ?? 0) + (Volta?.GolsMandante ?? 0));

    /// <summary>Quem passou (nulo enquanto o confronto não terminou).</summary>
    public string? Vencedor
    {
        get
        {
            var decisivo = JogoUnico ? Ida : Volta;
            if (decisivo is null)
            {
                return null;
            }

            var (a, b) = Agregado;
            if (a != b)
            {
                return a > b ? ClubeA : ClubeB;
            }

            // Pênaltis no jogo decisivo: no jogo único A é o mandante; na volta, o mandante é B.
            var (penMandante, penVisitante) = (decisivo.PenaltisMandante ?? 0, decisivo.PenaltisVisitante ?? 0);
            var mandanteVenceu = penMandante > penVisitante;
            return mandanteVenceu ? decisivo.MandanteId : decisivo.VisitanteId;
        }
    }
}

/// <summary>
/// Copa do Brasil simplificada (D-040): todos os clubes do jogo. Os 4 mais fortes da Série A entram na
/// 2ª fase; os outros jogam a 1ª fase em jogo único, com mando do clube de divisão mais baixa.
/// Da 2ª fase à final, ida e volta. Empate no confronto vai para os pênaltis (sem gol fora).
/// Os jogos são "de meio de semana": acontecem logo depois de certas rodadas do Brasileirão.
/// </summary>
public sealed class CopaDoBrasil
{
    /// <summary>Clubes na 2ª fase (16 confrontos até a final, que fecha o calendário de 11 datas).</summary>
    public const int ClubesNaSegundaFase = 32;

    /// <summary>A Copa precisa de mais de 32 e no máximo 64 clubes (com 60, 4 cabeças e 28 jogos na 1ª fase).</summary>
    public static bool Cabe(int clubes) => clubes > ClubesNaSegundaFase && clubes <= 2 * ClubesNaSegundaFase;

    /// <summary>Depois de qual rodada do Brasileirão acontece cada jogo da Copa.</summary>
    public static IReadOnlyList<(int Rodada, FaseCopa Fase, int Perna)> Calendario { get; } =
    [
        (3, FaseCopa.PrimeiraFase, 1),
        (7, FaseCopa.SegundaFase, 1), (9, FaseCopa.SegundaFase, 2),
        (13, FaseCopa.Oitavas, 1), (15, FaseCopa.Oitavas, 2),
        (19, FaseCopa.Quartas, 1), (21, FaseCopa.Quartas, 2),
        (25, FaseCopa.Semifinal, 1), (27, FaseCopa.Semifinal, 2),
        (31, FaseCopa.Final, 1), (33, FaseCopa.Final, 2),
    ];

    public static bool JogoUnico(FaseCopa fase) => fase == FaseCopa.PrimeiraFase;

    /// <summary>Prêmio para quem passa de fase (o da final é o do campeão).</summary>
    public static decimal PremioPorAvancar(FaseCopa fase) => fase switch
    {
        FaseCopa.PrimeiraFase => 100_000m,
        FaseCopa.SegundaFase => 250_000m,
        FaseCopa.Oitavas => 500_000m,
        FaseCopa.Quartas => 1_000_000m,
        FaseCopa.Semifinal => 1_500_000m,
        _ => 4_000_000m,
    };

    public const decimal PremioDoVice = 1_500_000m;

    private readonly ulong _seed;
    private readonly int _ano;
    private readonly Dictionary<FaseCopa, List<ConfrontoCopa>> _fases = [];

    public CopaDoBrasil(Mundo mundo, ulong seed)
    {
        ArgumentNullException.ThrowIfNull(mundo);
        if (!Cabe(mundo.Clubes.Count))
        {
            throw new ArgumentException($"A Copa precisa de 33 a 64 clubes (o mundo tem {mundo.Clubes.Count}).", nameof(mundo));
        }

        _seed = seed;
        _ano = mundo.TemporadaAtual;

        var nivel = mundo.Divisoes.SelectMany(d => d.ClubeIds.Select(id => (id, d.Nivel))).ToDictionary(x => x.id, x => x.Nivel, StringComparer.Ordinal);
        // Cabeças: os de maior reputação, da divisão de cima para baixo, até a 2ª fase fechar 32 (60 clubes → 4).
        // Reputação (e não força do elenco) porque não muda durante a temporada: o sorteio refeito ao carregar
        // um save precisa sair igual mesmo depois de transferências.
        var cabecas = (2 * ClubesNaSegundaFase) - mundo.Clubes.Count;
        Cabecas = [.. mundo.Clubes.OrderBy(c => nivel[c.Id]).ThenByDescending(c => c.Reputacao).ThenBy(c => c.Id, StringComparer.Ordinal)
            .Take(cabecas).Select(c => c.Id)];

        var outros = mundo.Clubes.Select(c => c.Id).Where(id => !Cabecas.Contains(id)).Order(StringComparer.Ordinal).ToList();
        var sorteados = Sortear(outros, FaseCopa.PrimeiraFase);
        var primeira = new List<ConfrontoCopa>();
        for (var i = 0; i + 1 < sorteados.Count; i += 2)
        {
            var (a, b) = (sorteados[i], sorteados[i + 1]);
            // Mando do clube de divisão mais baixa (nível maior).
            var (mandante, visitante) = nivel[b] > nivel[a] ? (b, a) : (a, b);
            primeira.Add(new ConfrontoCopa(FaseCopa.PrimeiraFase, primeira.Count, mandante, visitante));
        }

        _fases[FaseCopa.PrimeiraFase] = primeira;
    }

    /// <summary>Clubes que só entram na 2ª fase.</summary>
    public IReadOnlyList<string> Cabecas { get; }

    public IReadOnlyList<ConfrontoCopa> Confrontos(FaseCopa fase) => _fases.GetValueOrDefault(fase) ?? [];

    public IEnumerable<FaseCopa> FasesDefinidas => _fases.Keys.Order();

    public string? Campeao => Confrontos(FaseCopa.Final).FirstOrDefault()?.Vencedor;

    public string? Vice => Confrontos(FaseCopa.Final).FirstOrDefault() is { Vencedor: { } v } f ? (v == f.ClubeA ? f.ClubeB : f.ClubeA) : null;

    /// <summary>Etapa da Copa que acontece depois da rodada <paramref name="rodada"/> do Brasileirão.</summary>
    public static (FaseCopa Fase, int Perna)? EtapaDepoisDa(int rodada) =>
        Calendario.FirstOrDefault(c => c.Rodada == rodada) is { Rodada: > 0 } e ? (e.Fase, e.Perna) : null;

    /// <summary>Rodada do Brasileirão depois da qual acontece a etapa.</summary>
    public static int RodadaDaEtapa(FaseCopa fase, int perna) => Calendario.First(c => c.Fase == fase && c.Perna == perna).Rodada;

    /// <summary>Jogos de uma etapa: (confronto, mandante, visitante).</summary>
    public IReadOnlyList<(ConfrontoCopa Confronto, string Mandante, string Visitante)> Jogos(FaseCopa fase, int perna) =>
        [.. Confrontos(fase).Where(c => perna == 1 ? c.Ida is null : !c.JogoUnico && c.Volta is null)
            .Select(c => perna == 1 ? (c, c.ClubeA, c.ClubeB) : (c, c.ClubeB, c.ClubeA))];

    /// <summary>Próximo jogo de um clube na Copa (nulo se eliminado, campeão ou sem jogo definido).</summary>
    public (FaseCopa Fase, int Perna, string Adversario, bool EmCasa)? ProximoJogo(string clubeId)
    {
        foreach (var fase in FasesDefinidas.Reverse())
        {
            var c = Confrontos(fase).FirstOrDefault(x => x.ClubeA == clubeId || x.ClubeB == clubeId);
            if (c is null)
            {
                continue;
            }

            if (c.Vencedor is not null)
            {
                return null;
            }

            var perna = c.Ida is null ? 1 : 2;
            var mandante = perna == 1 ? c.ClubeA : c.ClubeB;
            return (fase, perna, c.ClubeA == clubeId ? c.ClubeB : c.ClubeA, mandante == clubeId);
        }

        return null;
    }

    /// <summary>Em que fase o clube caiu (nulo se ainda está vivo).</summary>
    public FaseCopa? EliminadoEm(string clubeId) =>
        FasesDefinidas.Select(f => Confrontos(f).FirstOrDefault(c => c.ClubeA == clubeId || c.ClubeB == clubeId))
            .LastOrDefault(c => c is not null) is { Vencedor: { } v } c2 && v != clubeId ? c2.Fase : null;

    /// <summary>Registra um jogo e, se a fase acabou, sorteia a seguinte.</summary>
    public void Registrar(JogoCopa jogo)
    {
        ArgumentNullException.ThrowIfNull(jogo);
        var confronto = Confrontos(jogo.Fase).ElementAtOrDefault(jogo.Confronto)
            ?? throw new InvalidOperationException($"Confronto {jogo.Confronto} da fase {jogo.Fase} não existe.");
        var (mandanteEsperado, visitanteEsperado) = jogo.Perna == 1 ? (confronto.ClubeA, confronto.ClubeB) : (confronto.ClubeB, confronto.ClubeA);
        if (jogo.MandanteId != mandanteEsperado || jogo.VisitanteId != visitanteEsperado)
        {
            throw new InvalidOperationException("O jogo da Copa não bate com o sorteio.");
        }

        if (jogo.Perna == 1)
        {
            confronto.Ida = jogo;
        }
        else
        {
            confronto.Volta = jogo;
        }

        SortearProximaFaseSePronta(jogo.Fase);
    }

    /// <summary>Todos os jogos já disputados, na ordem em que aconteceram (para o save).</summary>
    public IEnumerable<JogoCopa> Disputados =>
        FasesDefinidas.SelectMany(f => Confrontos(f).SelectMany(c => new[] { c.Ida, c.Volta }))
            .Where(j => j is not null).Select(j => j!).OrderBy(j => j.Rodada).ThenBy(j => j.Confronto);

    private void SortearProximaFaseSePronta(FaseCopa fase)
    {
        if (fase == FaseCopa.Final || _fases.ContainsKey(fase + 1) || Confrontos(fase).Any(c => c.Vencedor is null))
        {
            return;
        }

        var classificados = Confrontos(fase).Select(c => c.Vencedor!).ToList();
        if (fase == FaseCopa.PrimeiraFase)
        {
            classificados.AddRange(Cabecas);
        }

        var sorteados = Sortear(classificados.Order(StringComparer.Ordinal).ToList(), fase + 1);
        _fases[fase + 1] = [.. Enumerable.Range(0, sorteados.Count / 2)
            .Select(i => new ConfrontoCopa(fase + 1, i, sorteados[2 * i], sorteados[(2 * i) + 1]))];
    }

    private List<string> Sortear(List<string> clubes, FaseCopa fase)
    {
        var aleatorio = new GeradorAleatorio(_seed ^ GeradorDeMundo.HashEstavel($"{_ano}/copa/sorteio/{fase}"));
        var lista = clubes.ToList();
        for (var i = lista.Count - 1; i > 0; i--)
        {
            var j = aleatorio.Proximo(0, i + 1);
            (lista[i], lista[j]) = (lista[j], lista[i]);
        }

        return lista;
    }
}
