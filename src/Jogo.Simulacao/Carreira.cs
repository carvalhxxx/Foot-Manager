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
    /// <summary>Começa uma carreira nova (cria os agentes livres iniciais se o mundo não tiver nenhum).</summary>
    public Carreira(Mundo mundo, string clubeId, ulong seed)
        : this(mundo, clubeId, seed, novaCarreira: true)
    {
    }

    private Carreira(Mundo mundo, string clubeId, ulong seed, bool novaCarreira)
    {
        ArgumentNullException.ThrowIfNull(mundo);
        Mundo = mundo;
        Clube = mundo.Clube(clubeId);
        Seed = seed;
        if (novaCarreira && mundo.AgentesLivres.Count == 0)
        {
            Mercado.AgentesLivresIniciais.Gerar(mundo, seed);
        }

        Temporada = NovaTemporada();
        UsarEscalacaoAutomatica();
    }

    /// <summary>
    /// Retoma uma carreira salva: o mundo já restaurado, a temporada recriada pela seed com as rodadas
    /// disputadas (via <paramref name="restaurarTemporada"/>) e a tática do jogador.
    /// </summary>
    public static Carreira Restaurar(
        Mundo mundo,
        string clubeId,
        ulong seed,
        Action<Temporada> restaurarTemporada,
        TaticaDoJogador tatica)
    {
        ArgumentNullException.ThrowIfNull(restaurarTemporada);
        var carreira = new Carreira(mundo, clubeId, seed, novaCarreira: false);
        restaurarTemporada(carreira.Temporada);
        carreira.UltimaRodada = carreira.Temporada.RelatorioDaRodada(carreira.Temporada.RodadaAtual);
        carreira.DefinirTatica(tatica);
        return carreira;
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

    /// <summary>Renova o contrato de um jogador do elenco pelo salário que ele pede.</summary>
    public void RenovarContrato(int jogadorId, int anos)
    {
        ExigirForaDoAoVivo();
        Mercado.Contratos.Renovar(Clube, JogadorDoElenco(jogadorId), Mundo.TemporadaAtual, anos);
    }

    /// <summary>Dispensa um jogador (paga a rescisão) e refaz a escalação se ele estava nela.</summary>
    public void Dispensar(int jogadorId)
    {
        ExigirForaDoAoVivo();
        Mercado.Contratos.Dispensar(Mundo, Clube, JogadorDoElenco(jogadorId));
        if (Tatica.Titulares.Contains(jogadorId) || Tatica.Reservas.Contains(jogadorId))
        {
            UsarEscalacaoAutomatica();
        }
    }

    private Jogador JogadorDoElenco(int jogadorId) =>
        Clube.Elenco.FirstOrDefault(j => j.Id == jogadorId)
            ?? throw new KeyNotFoundException("Esse jogador não é do seu elenco.");

    private void ExigirForaDoAoVivo()
    {
        if (AoVivo is not null)
        {
            throw new InvalidOperationException("Espere a rodada ao vivo terminar.");
        }
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

    /// <summary>Joga a próxima rodada de uma vez (modo rápido) com a escalação do jogador (ajustada se preciso).</summary>
    public RelatorioRodada AvancarRodada()
    {
        var (escalacao, _) = MontarEscalacao();
        UltimaRodada = Temporada.JogarRodada(new Dictionary<string, TimeEscalado>(StringComparer.Ordinal) { [Clube.Id] = escalacao });
        FixarEscalacaoQueJogou(escalacao);
        MercadoDaIa();
        return UltimaRodada;
    }

    // ------------------------------------------------------------------ mercado

    /// <summary>A janela de transferências está aberta agora (antes da próxima rodada)?</summary>
    public bool JanelaAberta => !Temporada.Encerrada && Mercado.Transferencias.JanelaAberta(Temporada.RodadaAtual);

    /// <summary>Compra um jogador de outro clube (janela aberta) ou contrata um agente livre.</summary>
    public void Comprar(int jogadorId)
    {
        ExigirForaDoAoVivo();
        var jogador = Mundo.Clubes.SelectMany(c => c.Elenco).Concat(Mundo.AgentesLivres).FirstOrDefault(j => j.Id == jogadorId)
            ?? throw new KeyNotFoundException("Jogador não encontrado.");
        if (Temporada.Encerrada)
        {
            throw new InvalidOperationException("A temporada acabou: comece a próxima para negociar.");
        }

        Mercado.Transferencias.Comprar(Mundo, Clube, jogador, Temporada.RodadaAtual);
    }

    /// <summary>Vende um jogador do elenco para a melhor oferta.</summary>
    public Mercado.Oferta Vender(int jogadorId)
    {
        ExigirForaDoAoVivo();
        if (Temporada.Encerrada)
        {
            throw new InvalidOperationException("A temporada acabou: comece a próxima para negociar.");
        }

        var oferta = Mercado.Transferencias.Vender(Mundo, Clube, JogadorDoElenco(jogadorId), Temporada.RodadaAtual);
        if (Tatica.Titulares.Contains(jogadorId) || Tatica.Reservas.Contains(jogadorId))
        {
            UsarEscalacaoAutomatica();
        }

        return oferta;
    }

    /// <summary>Negócios da IA logo depois da última rodada (só para exibir; não vai para o save).</summary>
    public IReadOnlyList<Mercado.Transferencia> UltimasTransferencias { get; private set; } = [];

    /// <summary>Depois de cada rodada, com a janela aberta, a IA compra e vende (sorteio pela seed da rodada).</summary>
    private void MercadoDaIa()
    {
        UltimasTransferencias = [];
        if (Temporada.Encerrada)
        {
            return;
        }

        var aleatorio = new Dominio.Aleatoriedade.GeradorAleatorio(
            SementeDoAno(Temporada.Ano) ^ GeradorDeMundo.HashEstavel($"mercado/{Temporada.RodadaAtual}"));
        UltimasTransferencias = Mercado.Transferencias.MovimentarIa(Mundo, Clube.Id, Temporada.RodadaAtual, aleatorio);
    }

    // ------------------------------------------------------------------ ao vivo

    /// <summary>Rodada sendo jogada ao vivo (nula fora dela).</summary>
    public RodadaEmAndamento? AoVivo => Temporada.EmAndamento;

    /// <summary>O jogo do clube do jogador na rodada ao vivo.</summary>
    public JogoEmAndamento? MeuJogoAoVivo => AoVivo?.JogoDo(Clube.Id);

    /// <summary>Time do jogador dentro da partida ao vivo.</summary>
    public TimeEmJogo? MeuTimeAoVivo =>
        MeuJogoAoVivo is { } jogo ? (jogo.Mandante.Id == Clube.Id ? jogo.Partida.Mandante : jogo.Partida.Visitante) : null;

    private TimeEscalado? _escalacaoAoVivo;

    /// <summary>
    /// Começa a rodada ao vivo: o jogador decide pelo seu time (sem treinador automático); os outros clubes seguem com a IA.
    /// </summary>
    public RodadaEmAndamento IniciarRodadaAoVivo()
    {
        var (escalacao, _) = MontarEscalacao();
        _escalacaoAoVivo = escalacao;
        return Temporada.IniciarRodada(
            new Dictionary<string, TimeEscalado>(StringComparer.Ordinal) { [Clube.Id] = escalacao },
            new HashSet<string>(StringComparer.Ordinal) { Clube.Id });
    }

    /// <summary>Avança um minuto em todos os jogos da rodada ao vivo e devolve os eventos novos do jogo do jogador.</summary>
    public IReadOnlyList<EventoPartida> AvancarMinutoAoVivo()
    {
        var rodada = AoVivo ?? throw new InvalidOperationException("Não há rodada ao vivo.");
        var partida = MeuJogoAoVivo?.Partida;
        var antes = partida?.Eventos.Count ?? 0;
        rodada.AvancarMinuto();
        return partida is null ? [] : [.. partida.Eventos.Skip(antes)];
    }

    /// <summary>Aplica uma decisão do jogador no jogo dele.</summary>
    public EventoPartida DecidirAoVivo(Decisao decisao)
    {
        var jogo = MeuJogoAoVivo ?? throw new InvalidOperationException("Não há jogo ao vivo.");
        var equipe = jogo.Mandante.Id == Clube.Id ? Equipe.Mandante : Equipe.Visitante;
        return MotorDePartida.AplicarDecisao(jogo.Partida, equipe, decisao);
    }

    /// <summary>
    /// Troca a formação durante o jogo, reposicionando quem está em campo onde cada um rende mais.
    /// Com jogadores a menos, as últimas posições da formação (as de ataque) ficam vazias.
    /// </summary>
    public EventoPartida TrocarFormacaoAoVivo(Formacao formacao)
    {
        ArgumentNullException.ThrowIfNull(formacao);
        var time = MeuTimeAoVivo ?? throw new InvalidOperationException("Não há jogo ao vivo.");
        var deLinha = time.EmCampo.Where(j => j.Posicao != Posicao.Goleiro).Select(j => j.Jogador).ToArray();
        var posicoes = formacao.Posicoes.Take(deLinha.Length).ToArray();
        var ordem = Escalador.Distribuir(deLinha, posicoes);
        return DecidirAoVivo(new TrocaDeFormacao(formacao, [.. ordem.Select(j => j.Id)]));
    }

    /// <summary>Fecha a rodada ao vivo (todos os jogos precisam ter acabado) e aplica o pós-jogo.</summary>
    public RelatorioRodada ConcluirRodadaAoVivo()
    {
        var escalacao = _escalacaoAoVivo ?? throw new InvalidOperationException("Não há rodada ao vivo.");
        var mentalidadeFinal = MeuTimeAoVivo?.Mentalidade ?? Tatica.Mentalidade;
        UltimaRodada = Temporada.ConcluirRodada();
        _escalacaoAoVivo = null;
        FixarEscalacaoQueJogou(escalacao);
        Tatica = Tatica with { Mentalidade = mentalidadeFinal };
        MercadoDaIa();
        return UltimaRodada;
    }

    /// <summary>A tática passa a ser a escalação que de fato começou o jogo (sem lesionados/suspensos da rodada anterior).</summary>
    private void FixarEscalacaoQueJogou(TimeEscalado escalacao) =>
        Tatica = Tatica with { Titulares = [.. escalacao.Titulares.Select(j => j.Id)], Reservas = [.. escalacao.Reservas.Select(j => j.Id)] };

    /// <summary>Fecha a temporada (acesso, evolução, base...) e começa a próxima.</summary>
    public ResultadoTemporada EncerrarTemporada()
    {
        var resultado = FimDeTemporada.Encerrar(Temporada, Mundo, SementeDoAno(Temporada.Ano), Clube.Id);
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
