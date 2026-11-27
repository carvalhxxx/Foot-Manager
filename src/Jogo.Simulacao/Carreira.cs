using Jogo.Dominio;
using Jogo.Dominio.Clubes;
using Jogo.Dominio.Jogadores;
using Jogo.Motor;
using Jogo.Simulacao.Geracao;
using Jogo.Simulacao.Ia;
using Jogo.Simulacao.Temporadas;

namespace Jogo.Simulacao;

/// <summary>A tática que o jogador escolheu: formação, mentalidade, titulares (na ordem das posições) e reservas.</summary>
public sealed record TaticaDoJogador(Formacao Formacao, Mentalidade Mentalidade, IReadOnlyList<int> Titulares, IReadOnlyList<int> Reservas);

/// <summary>Escalação que vai a campo nesta rodada e o que o auxiliar teve de mudar.</summary>
public sealed record EscalacaoDaRodada(TimeEscalado Escalacao, IReadOnlyList<string> Ajustes);

/// <summary>
/// A carreira do jogador: o mundo, o clube que ele treina, a temporada em andamento e a tática escolhida.
/// Os outros clubes seguem com a IA.
/// </summary>
public sealed class Carreira
{
    public Carreira(Mundo mundo, string clubeId, ulong seed)
    {
        ArgumentNullException.ThrowIfNull(mundo);
        Mundo = mundo;
        Clube = mundo.Clube(clubeId);
        Seed = seed;
        Temporada = NovaTemporada();
        UsarEscalacaoAutomatica();
    }

    public Mundo Mundo { get; }
    public Clube Clube { get; }
    public ulong Seed { get; }
    public Temporada Temporada { get; private set; }
    public TaticaDoJogador Tatica { get; private set; } = null!;

    /// <summary>Relatório da última rodada jogada (nulo no começo da temporada).</summary>
    public RelatorioRodada? UltimaRodada { get; private set; }

    /// <summary>Divisão em que o clube do jogador está nesta temporada.</summary>
    public CompeticaoDaTemporada MinhaCompeticao =>
        Temporada.Competicoes.First(c => c.Divisao.ClubeIds.Contains(Clube.Id));

    /// <summary>Próximo jogo do clube do jogador, ou nulo se a temporada acabou.</summary>
    public JogoAgendado? ProximoJogo
    {
        get
        {
            var calendario = MinhaCompeticao.Calendario;
            return Temporada.RodadaAtual < calendario.Count
                ? calendario[Temporada.RodadaAtual].First(j => j.MandanteId == Clube.Id || j.VisitanteId == Clube.Id)
                : null;
        }
    }

    /// <summary>Problemas estruturais da tática (posições, jogadores repetidos ou de fora do elenco).</summary>
    public IReadOnlyList<string> ValidarTatica(TaticaDoJogador tatica)
    {
        ArgumentNullException.ThrowIfNull(tatica);
        var erros = new List<string>();
        var elenco = Clube.Elenco.Select(j => j.Id).ToHashSet();

        if (tatica.Titulares.Count != Formacao.JogadoresDeLinha + 1)
        {
            erros.Add("Escale exatamente 11 titulares.");
        }

        if (tatica.Reservas.Count > Escalador.Reservas)
        {
            erros.Add($"No máximo {Escalador.Reservas} reservas.");
        }

        var todos = tatica.Titulares.Concat(tatica.Reservas).ToArray();
        if (todos.Distinct().Count() != todos.Length)
        {
            erros.Add("Um jogador não pode aparecer duas vezes.");
        }

        if (todos.Any(id => !elenco.Contains(id)))
        {
            erros.Add("Há jogadores que não são do elenco.");
        }

        return erros;
    }

    public void DefinirTatica(TaticaDoJogador tatica)
    {
        var erros = ValidarTatica(tatica);
        if (erros.Count > 0)
        {
            throw new ArgumentException(string.Join(" ", erros), nameof(tatica));
        }

        Tatica = tatica;
    }

    /// <summary>Deixa a IA escolher formação e jogadores (o "auxiliar técnico").</summary>
    public void UsarEscalacaoAutomatica()
    {
        var sugestao = Temporada.Escalar(Clube);
        Tatica = new TaticaDoJogador(sugestao.Formacao, Tatica?.Mentalidade ?? Mentalidade.Equilibrada,
            [.. sugestao.Titulares.Select(j => j.Id)], [.. sugestao.Reservas.Select(j => j.Id)]);
    }

    /// <summary>
    /// Monta a escalação da rodada a partir da tática. Quem estiver lesionado, suspenso ou fora do elenco
    /// é trocado pelo melhor disponível para a posição, e o banco é completado.
    /// </summary>
    public EscalacaoDaRodada MontarEscalacao()
    {
        var ajustes = new List<string>();
        var porId = Clube.Elenco.ToDictionary(j => j.Id);
        var usados = new HashSet<int>();
        var posicoes = Tatica.Formacao.PosicoesComGoleiro;
        var titulares = new Jogador[posicoes.Count];

        // Primeiro mantém quem pode jogar; depois preenche as vagas.
        for (var i = 0; i < posicoes.Count; i++)
        {
            if (i < Tatica.Titulares.Count && porId.TryGetValue(Tatica.Titulares[i], out var jogador) && jogador.Disponivel && usados.Add(jogador.Id))
            {
                titulares[i] = jogador;
            }
        }

        for (var i = 0; i < posicoes.Count; i++)
        {
            if (titulares[i] is not null)
            {
                continue;
            }

            var candidatos = Clube.Elenco.Where(j => j.Disponivel && !usados.Contains(j.Id)).ToArray();
            var substituto = TreinadorAutomatico.MelhorReserva(candidatos, posicoes[i])
                             ?? candidatos.OrderByDescending(j => j.ForcaNaPosicao(posicoes[i])).ThenBy(j => j.Id).FirstOrDefault()
                             ?? Clube.Elenco.First(j => !usados.Contains(j.Id));
            titulares[i] = substituto;
            usados.Add(substituto.Id);

            var saiu = i < Tatica.Titulares.Count && porId.TryGetValue(Tatica.Titulares[i], out var original) ? original : null;
            ajustes.Add(saiu is null
                ? $"{substituto.Nome} entra como {posicoes[i].Sigla()}."
                : $"{substituto.Nome} entra no lugar de {saiu.Nome} ({Motivo(saiu)}).");
        }

        var reservas = Tatica.Reservas
            .Where(id => porId.TryGetValue(id, out var r) && r.Disponivel && !usados.Contains(id))
            .Select(id => porId[id])
            .ToList();
        reservas.ForEach(r => usados.Add(r.Id));

        // Completa o banco (com um goleiro, se possível).
        var sobra = Clube.Elenco.Where(j => j.Disponivel && !usados.Contains(j.Id)).ToList();
        if (!reservas.Exists(r => r.PosicaoPrincipal == Posicao.Goleiro)
            && sobra.Where(j => j.PosicaoPrincipal == Posicao.Goleiro).OrderByDescending(j => j.ForcaGeral).FirstOrDefault() is { } goleiro
            && reservas.Count < Escalador.Reservas)
        {
            reservas.Add(goleiro);
            sobra.Remove(goleiro);
        }

        reservas.AddRange(sobra.OrderByDescending(j => j.ForcaGeral).ThenBy(j => j.Id).Take(Escalador.Reservas - reservas.Count));

        var escalacao = new TimeEscalado(Clube.Id, Tatica.Formacao, titulares, reservas, Tatica.Mentalidade);
        return new EscalacaoDaRodada(escalacao, ajustes);
    }

    /// <summary>Joga a próxima rodada com a escalação do jogador (ajustada se preciso).</summary>
    public RelatorioRodada AvancarRodada()
    {
        var (escalacao, _) = MontarEscalacao();
        UltimaRodada = Temporada.JogarRodada(new Dictionary<string, TimeEscalado>(StringComparer.Ordinal) { [Clube.Id] = escalacao });

        // A tática passa a ser a escalação que de fato jogou (sem lesionados/suspensos da rodada anterior).
        Tatica = Tatica with { Titulares = [.. escalacao.Titulares.Select(j => j.Id)], Reservas = [.. escalacao.Reservas.Select(j => j.Id)] };
        return UltimaRodada;
    }

    /// <summary>Fecha a temporada (acesso, evolução, base...) e começa a próxima.</summary>
    public ResultadoTemporada EncerrarTemporada()
    {
        var resultado = FimDeTemporada.Encerrar(Temporada, Mundo, SementeDoAno(Temporada.Ano));
        Temporada = NovaTemporada();
        UltimaRodada = null;

        // Aposentados e novos jovens: refaz a escalação, mantendo a mentalidade.
        UsarEscalacaoAutomatica();
        return resultado;
    }

    private Temporada NovaTemporada() => new(Mundo, SementeDoAno(Mundo.TemporadaAtual));

    private ulong SementeDoAno(int ano) => Seed ^ GeradorDeMundo.HashEstavel($"temporada/{ano}");

    private static string Motivo(Jogador jogador) =>
        jogador.RodadasLesionado > 0 ? "lesionado"
        : jogador.RodadasSuspenso > 0 ? "suspenso"
        : "indisponível";
}
