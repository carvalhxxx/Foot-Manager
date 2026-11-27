using Jogo.Dominio;
using Jogo.Dominio.Aleatoriedade;
using Jogo.Dominio.Clubes;
using Jogo.Dominio.Competicoes;
using Jogo.Dominio.Jogadores;
using Jogo.Motor;
using Jogo.Simulacao.Financas;
using Jogo.Simulacao.Geracao;
using Jogo.Simulacao.Ia;

namespace Jogo.Simulacao.Temporadas;

/// <summary>Um jogo disputado: placar (para a tabela) e o resultado completo do motor (eventos, estatísticas).</summary>
public sealed record JogoDisputado(PlacarJogo Placar, ResultadoPartida Resultado);

/// <summary>Os jogos de uma rodada, por divisão.</summary>
public sealed record RelatorioRodada(int Rodada, IReadOnlyDictionary<string, IReadOnlyList<JogoDisputado>> JogosPorDivisao);

/// <summary>
/// Um confronto do playoff de acesso (ida e volta). Gols somados nos dois jogos; empate no agregado
/// classifica o melhor colocado na fase de pontos corridos (D-035). Vencedor nulo enquanto não acabou.
/// </summary>
public sealed record ConfrontoPlayoff(
    string MelhorId, int MelhorPosicao, string PiorId, int PiorPosicao,
    PlacarJogo? Ida, PlacarJogo? Volta, string? VencedorId)
{
    public int GolsMelhor => (Ida?.GolsVisitante ?? 0) + (Volta?.GolsMandante ?? 0);
    public int GolsPior => (Ida?.GolsMandante ?? 0) + (Volta?.GolsVisitante ?? 0);
}

/// <summary>Um goleador da competição.</summary>
public sealed record Artilheiro(int JogadorId, string ClubeId, int Gols, int Jogos);

/// <summary>Uma divisão dentro da temporada: calendário e jogos já disputados.</summary>
public sealed class CompeticaoDaTemporada
{
    private readonly List<JogoDisputado> _disputados = [];
    private readonly List<IReadOnlyList<JogoAgendado>> _calendario;

    internal CompeticaoDaTemporada(Divisao divisao, IReadOnlyList<IReadOnlyList<JogoAgendado>> calendario)
    {
        Divisao = divisao;
        _calendario = [.. calendario];
        RodadasRegulares = calendario.Count;
    }

    public Divisao Divisao { get; }

    /// <summary>Rodadas de pontos corridos e, depois de terminadas, as do playoff (se houver).</summary>
    public IReadOnlyList<IReadOnlyList<JogoAgendado>> Calendario => _calendario;

    /// <summary>Rodadas da fase de pontos corridos (as que contam para a tabela).</summary>
    public int RodadasRegulares { get; }

    /// <summary>Rodadas extras de playoff (ida e volta) que esta divisão terá.</summary>
    public int RodadasDePlayoff => Divisao.TemPlayoff ? 2 : 0;

    public IReadOnlyList<JogoDisputado> Disputados => _disputados;

    /// <summary>Os confrontos do playoff já foram definidos (a fase de pontos corridos acabou).</summary>
    public bool PlayoffDefinido => _calendario.Count > RodadasRegulares;

    internal void Registrar(JogoDisputado jogo) => _disputados.Add(jogo);

    internal void AdicionarRodada(IReadOnlyList<JogoAgendado> jogos) => _calendario.Add(jogos);
}

/// <summary>
/// Uma temporada de pontos corridos em todas as divisões do mundo, rodada a rodada.
/// Determinística: mesmo mundo + mesma seed = mesma temporada.
/// </summary>
public sealed class Temporada
{
    private readonly Mundo _mundo;
    private readonly ulong _seed;

    public Temporada(
        Mundo mundo,
        ulong seed,
        RegrasTemporada? regras = null,
        ConfiguracaoMotor? configuracao = null,
        RegrasFinanceiras? financas = null)
    {
        ArgumentNullException.ThrowIfNull(mundo);

        _mundo = mundo;
        _seed = seed;
        Regras = regras ?? RegrasTemporada.Padrao;
        RegrasFinanceiras = financas ?? RegrasFinanceiras.Padrao;
        Configuracao = configuracao ?? ConfiguracaoMotor.Padrao;
        Ano = mundo.TemporadaAtual;

        Competicoes = [.. mundo.Divisoes.Select(d => new CompeticaoDaTemporada(
            d, Temporadas.Calendario.PontosCorridos(d.ClubeIds, new GeradorAleatorio(Semente(d.Id, 0, 0)))))];
        TotalRodadas = Competicoes.Max(c => c.RodadasRegulares + c.RodadasDePlayoff);
    }

    public int Ano { get; }
    public RegrasTemporada Regras { get; }
    public RegrasFinanceiras RegrasFinanceiras { get; }
    public ConfiguracaoMotor Configuracao { get; }
    public IReadOnlyList<CompeticaoDaTemporada> Competicoes { get; }

    /// <summary>Última rodada jogada (0 = nenhuma).</summary>
    public int RodadaAtual { get; private set; }

    public int TotalRodadas { get; }
    public bool Encerrada => RodadaAtual >= TotalRodadas;

    /// <summary>
    /// Recoloca as rodadas já disputadas (vindas de um save). O calendário é recalculado pela seed,
    /// então cada jogo precisa existir nele; se não bater, o save é de outro mundo ou foi corrompido.
    /// </summary>
    public void Restaurar(int rodadaAtual, IEnumerable<(string DivisaoId, JogoDisputado Jogo)> disputados)
    {
        ArgumentNullException.ThrowIfNull(disputados);
        if (RodadaAtual != 0 || EmAndamento is not null || Competicoes.Any(c => c.Disputados.Count > 0))
        {
            throw new InvalidOperationException("Só dá para restaurar uma temporada recém-criada.");
        }

        if (rodadaAtual < 0 || rodadaAtual > TotalRodadas)
        {
            throw new ArgumentOutOfRangeException(nameof(rodadaAtual), rodadaAtual, "Rodada fora do calendário.");
        }

        var ordenados = disputados.OrderBy(d => d.Jogo.Placar.Jogo.Rodada).ToArray();
        foreach (var (divisaoId, jogo) in ordenados)
        {
            var competicao = Competicao(divisaoId);
            if (jogo.Placar.Jogo.Rodada > competicao.RodadasRegulares)
            {
                DefinirPlayoffSePronto(competicao);
            }

            var agendado = jogo.Placar.Jogo;
            if (agendado.Rodada > rodadaAtual || agendado.Rodada > competicao.Calendario.Count
                || !competicao.Calendario[agendado.Rodada - 1].Contains(agendado))
            {
                throw new InvalidOperationException(
                    $"O jogo {agendado.MandanteId} x {agendado.VisitanteId} (rodada {agendado.Rodada}) não bate com o calendário.");
            }

            competicao.Registrar(jogo);
        }

        foreach (var competicao in Competicoes)
        {
            if (rodadaAtual >= competicao.RodadasRegulares)
            {
                DefinirPlayoffSePronto(competicao);
            }

            var esperados = competicao.Calendario.Take(rodadaAtual).Sum(r => r.Count);
            if (competicao.Disputados.Count != esperados)
            {
                throw new InvalidOperationException($"Faltam jogos de {competicao.Divisao.Nome} no save.");
            }
        }

        RodadaAtual = rodadaAtual;
    }

    /// <summary>Fração dos pontos do clube nos últimos 5 jogos antes da rodada (0,5 se ainda não jogou).</summary>
    private static double AproveitamentoRecente(CompeticaoDaTemporada competicao, string clubeId, int rodada)
    {
        var ultimos = competicao.Disputados
            .Where(d => d.Placar.Jogo.Rodada < rodada && (d.Placar.Jogo.MandanteId == clubeId || d.Placar.Jogo.VisitanteId == clubeId))
            .TakeLast(5)
            .ToArray();
        if (ultimos.Length == 0)
        {
            return 0.5;
        }

        var pontos = ultimos.Sum(d =>
        {
            var (pro, contra) = d.Placar.Jogo.MandanteId == clubeId
                ? (d.Placar.GolsMandante, d.Placar.GolsVisitante)
                : (d.Placar.GolsVisitante, d.Placar.GolsMandante);
            return pro > contra ? 3 : pro == contra ? 1 : 0;
        });
        return pontos / (3.0 * ultimos.Length);
    }

    /// <summary>Relatório de uma rodada já disputada, montado a partir dos jogos registrados.</summary>
    public RelatorioRodada? RelatorioDaRodada(int rodada)
    {
        var jogos = Competicoes
            .Select(c => (c.Divisao.Id, Jogos: c.Disputados.Where(j => j.Placar.Jogo.Rodada == rodada).ToArray()))
            .Where(x => x.Jogos.Length > 0)
            .ToDictionary(x => x.Id, x => (IReadOnlyList<JogoDisputado>)x.Jogos, StringComparer.Ordinal);
        return jogos.Count == 0 ? null : new RelatorioRodada(rodada, jogos);
    }

    public CompeticaoDaTemporada Competicao(string divisaoId) =>
        Competicoes.FirstOrDefault(c => c.Divisao.Id == divisaoId)
        ?? throw new KeyNotFoundException($"Divisão \"{divisaoId}\" não está nesta temporada.");

    public IReadOnlyList<LinhaTabela> Tabela(string divisaoId)
    {
        var competicao = Competicao(divisaoId);
        return Classificacao.Calcular(competicao.Divisao.ClubeIds,
            competicao.Disputados.Where(j => j.Placar.Jogo.Rodada <= competicao.RodadasRegulares).Select(j => j.Placar), _seed);
    }

    /// <summary>
    /// Confrontos do playoff de acesso da divisão (vazio se ela não tem playoff ou a fase de pontos
    /// corridos ainda não acabou).
    /// </summary>
    public IReadOnlyList<ConfrontoPlayoff> Playoff(string divisaoId)
    {
        var competicao = Competicao(divisaoId);
        if (!competicao.PlayoffDefinido)
        {
            return [];
        }

        var tabela = Tabela(divisaoId);
        var ida = competicao.RodadasRegulares + 1;
        return [.. competicao.Divisao.ConfrontosDoPlayoff.Select(c =>
        {
            var melhor = tabela[c.MelhorPosicao - 1].ClubeId;
            var pior = tabela[c.PiorPosicao - 1].ClubeId;
            PlacarJogo? Jogo(int rodada) => competicao.Disputados
                .Select(d => d.Placar)
                .FirstOrDefault(p => p.Jogo.Rodada == rodada && (p.Jogo.MandanteId == melhor || p.Jogo.VisitanteId == melhor));

            var (jogoIda, jogoVolta) = (Jogo(ida), Jogo(ida + 1));
            string? vencedor = null;
            if (jogoIda is not null && jogoVolta is not null)
            {
                var golsMelhor = jogoIda.GolsVisitante + jogoVolta.GolsMandante;
                var golsPior = jogoIda.GolsMandante + jogoVolta.GolsVisitante;
                vencedor = golsPior > golsMelhor ? pior : melhor;
            }

            return new ConfrontoPlayoff(melhor, c.MelhorPosicao, pior, c.PiorPosicao, jogoIda, jogoVolta, vencedor);
        })];
    }

    /// <summary>
    /// Ao fim dos pontos corridos, cria as duas rodadas do playoff pela tabela final:
    /// ida na casa do pior colocado, volta na casa do melhor.
    /// </summary>
    private void DefinirPlayoffSePronto(CompeticaoDaTemporada competicao)
    {
        if (!competicao.Divisao.TemPlayoff || competicao.PlayoffDefinido)
        {
            return;
        }

        var jogosRegulares = competicao.Calendario.Take(competicao.RodadasRegulares).Sum(r => r.Count);
        if (competicao.Disputados.Count(d => d.Placar.Jogo.Rodada <= competicao.RodadasRegulares) < jogosRegulares)
        {
            return;
        }

        var tabela = Tabela(competicao.Divisao.Id);
        var confrontos = competicao.Divisao.ConfrontosDoPlayoff
            .Select(c => (Melhor: tabela[c.MelhorPosicao - 1].ClubeId, Pior: tabela[c.PiorPosicao - 1].ClubeId))
            .ToArray();
        var ida = competicao.RodadasRegulares + 1;
        competicao.AdicionarRodada([.. confrontos.Select(c => new JogoAgendado(ida, c.Pior, c.Melhor))]);
        competicao.AdicionarRodada([.. confrontos.Select(c => new JogoAgendado(ida + 1, c.Melhor, c.Pior))]);
    }

    public IReadOnlyList<Artilheiro> Artilharia(string divisaoId, int quantidade = 10)
    {
        var competicao = Competicao(divisaoId);
        return [.. competicao.Disputados
            .SelectMany(j => j.Resultado.Participacoes.Select(p => (
                p,
                ClubeId: p.Equipe == Equipe.Mandante ? j.Resultado.MandanteId : j.Resultado.VisitanteId)))
            .GroupBy(x => x.p.JogadorId)
            .Select(g => new Artilheiro(g.Key, g.Last().ClubeId, g.Sum(x => x.p.Gols), g.Count()))
            .Where(a => a.Gols > 0)
            .OrderByDescending(a => a.Gols)
            .ThenBy(a => a.Jogos)
            .ThenBy(a => a.JogadorId)
            .Take(quantidade)];
    }

    /// <summary>Rodada que está sendo jogada ao vivo (nula fora da rodada).</summary>
    public RodadaEmAndamento? EmAndamento { get; private set; }

    /// <summary>Joga a próxima rodada inteira de uma vez (modo rápido) e aplica o pós-jogo aos elencos.</summary>
    /// <param name="escalacoesFixas">
    /// Escalações escolhidas fora da IA (o clube do jogador), por id de clube. O bônus de entrosamento é recalculado aqui.
    /// </param>
    public RelatorioRodada JogarRodada(IReadOnlyDictionary<string, TimeEscalado>? escalacoesFixas = null)
    {
        var rodada = IniciarRodada(escalacoesFixas);
        rodada.AvancarAteOFim();
        return ConcluirRodada();
    }

    /// <summary>
    /// Começa a próxima rodada para ser jogada minuto a minuto (todos os jogos juntos).
    /// </summary>
    /// <param name="clubesManuais">Clubes cujas decisões no jogo vêm de fora (o jogador); os demais usam o treinador automático.</param>
    public RodadaEmAndamento IniciarRodada(
        IReadOnlyDictionary<string, TimeEscalado>? escalacoesFixas = null,
        IReadOnlySet<string>? clubesManuais = null)
    {
        if (Encerrada)
        {
            throw new InvalidOperationException("A temporada já terminou.");
        }

        if (EmAndamento is not null)
        {
            throw new InvalidOperationException("Já há uma rodada em andamento.");
        }

        var rodada = RodadaAtual + 1;
        var jogos = new List<JogoEmAndamento>();

        foreach (var competicao in Competicoes.Where(c => rodada <= c.Calendario.Count))
        {
            var agenda = competicao.Calendario[rodada - 1];
            for (var i = 0; i < agenda.Count; i++)
            {
                var jogo = agenda[i];
                var mandante = _mundo.Clube(jogo.MandanteId);
                var visitante = _mundo.Clube(jogo.VisitanteId);
                var escalacaoMandante = EscalacaoDaRodada(mandante, escalacoesFixas);
                var escalacaoVisitante = EscalacaoDaRodada(visitante, escalacoesFixas);

                var partida = MotorDePartida.Iniciar(escalacaoMandante, escalacaoVisitante,
                    new GeradorAleatorio(Semente(competicao.Divisao.Id, rodada, i + 1)), configuracao: Configuracao,
                    mandanteAutomatico: clubesManuais?.Contains(mandante.Id) != true,
                    visitanteAutomatico: clubesManuais?.Contains(visitante.Id) != true);

                jogos.Add(new JogoEmAndamento(competicao, jogo, mandante, visitante, escalacaoMandante, escalacaoVisitante, partida));
            }
        }

        EmAndamento = new RodadaEmAndamento(rodada, jogos);
        return EmAndamento;
    }

    /// <summary>Fecha a rodada em andamento (todos os jogos precisam ter terminado) e aplica o pós-jogo.</summary>
    public RelatorioRodada ConcluirRodada()
    {
        var emAndamento = EmAndamento ?? throw new InvalidOperationException("Não há rodada em andamento.");
        if (!emAndamento.Encerrada)
        {
            throw new InvalidOperationException("Ainda há jogos em andamento nesta rodada.");
        }

        var jogosPorDivisao = new Dictionary<string, IReadOnlyList<JogoDisputado>>(StringComparer.Ordinal);
        var partidas = new List<(Clube Clube, TimeEscalado Escalacao, ResultadoPartida Resultado, Equipe Equipe)>();

        foreach (var grupo in emAndamento.Jogos.GroupBy(j => j.Competicao))
        {
            var jogos = new List<JogoDisputado>();
            foreach (var j in grupo)
            {
                var resultado = ResultadoPartida.De(j.Partida);
                var disputado = new JogoDisputado(
                    new PlacarJogo(j.Jogo, resultado.GolsMandante, resultado.GolsVisitante,
                        resultado.EstatisticasMandante.Amarelos, resultado.EstatisticasVisitante.Amarelos,
                        resultado.EstatisticasMandante.Vermelhos, resultado.EstatisticasVisitante.Vermelhos),
                    resultado);
                grupo.Key.Registrar(disputado);
                jogos.Add(disputado);
                partidas.Add((j.Mandante, j.EscalacaoMandante, resultado, Equipe.Mandante));
                partidas.Add((j.Visitante, j.EscalacaoVisitante, resultado, Equipe.Visitante));
            }

            jogosPorDivisao[grupo.Key.Divisao.Id] = jogos;
        }

        // 1. Quem estava fora cumpre a rodada (antes de aplicar as novas lesões e suspensões).
        foreach (var jogador in _mundo.Clubes.SelectMany(c => c.Elenco))
        {
            jogador.CumprirRodada();
        }

        // 2. Pós-jogo de cada clube que jogou.
        foreach (var (clube, escalacao, resultado, equipe) in partidas)
        {
            PosJogo.Aplicar(clube, escalacao, resultado, equipe, Regras);
        }

        // 3. Dinheiro: bilheteria de quem jogou em casa; TV, patrocínio e salários de todos.
        foreach (var j in emAndamento.Jogos)
        {
            Financas.Financas.ProcessarJogoEmCasa(j.Mandante, j.Visitante,
                AproveitamentoRecente(j.Competicao, j.Mandante.Id, emAndamento.Rodada), RegrasFinanceiras);
            Financas.Financas.ProcessarRodada(j.Mandante, j.Competicao.Divisao, j.Competicao.RodadasRegulares, RegrasFinanceiras);
            Financas.Financas.ProcessarRodada(j.Visitante, j.Competicao.Divisao, j.Competicao.RodadasRegulares, RegrasFinanceiras);
        }

        // 4. Recuperação física até a próxima rodada.
        foreach (var jogador in _mundo.Clubes.SelectMany(c => c.Elenco))
        {
            jogador.AjustarCondicao(Regras.RecuperacaoBase + (jogador.Atributos.Resistencia / 2));
        }

        RodadaAtual = emAndamento.Rodada;
        EmAndamento = null;
        foreach (var competicao in Competicoes.Where(c => c.RodadasRegulares == RodadaAtual))
        {
            DefinirPlayoffSePronto(competicao);
        }

        return new RelatorioRodada(emAndamento.Rodada, jogosPorDivisao);
    }

    public void JogarAteOFim()
    {
        while (!Encerrada)
        {
            JogarRodada();
        }
    }

    private TimeEscalado EscalacaoDaRodada(Clube clube, IReadOnlyDictionary<string, TimeEscalado>? fixas) =>
        fixas is not null && fixas.TryGetValue(clube.Id, out var fixa)
            ? fixa.ComEntrosamento(PosJogo.BonusEntrosamento(fixa.Titulares, Regras))
            : Escalar(clube);

    /// <summary>Escalação da IA: sem lesionados e suspensos, com o bônus de entrosamento dos titulares.</summary>
    public TimeEscalado Escalar(Clube clube)
    {
        ArgumentNullException.ThrowIfNull(clube);

        // Normalmente ficam fora lesionados e suspensos. Se sobrarem menos de 11 (caso extremo),
        // a IA usa lesionados e, por último, suspensos, para o jogo acontecer.
        var foraDeCombate = clube.Elenco.Where(j => !j.Disponivel).Select(j => j.Id).ToHashSet();
        var soSuspensos = clube.Elenco.Where(j => j.RodadasSuspenso > 0).Select(j => j.Id).ToHashSet();
        var indisponiveis = new[] { foraDeCombate, soSuspensos, new HashSet<int>() }
            .First(conjunto => clube.Elenco.Count - conjunto.Count >= Formacao.JogadoresDeLinha + 1);

        var escalacao = Escalador.Escalar(clube, indisponiveis: indisponiveis);
        return escalacao.ComEntrosamento(PosJogo.BonusEntrosamento(escalacao.Titulares, Regras));
    }

    /// <summary>Semente própria de cada jogo: mudar um jogo não altera os sorteios dos outros.</summary>
    private ulong Semente(string divisaoId, int rodada, int jogo) =>
        _seed ^ GeradorDeMundo.HashEstavel($"{Ano}/{divisaoId}/{rodada}/{jogo}");
}
