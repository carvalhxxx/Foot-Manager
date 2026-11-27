using System.Globalization;
using Jogo.Dados.Definicoes;
using Jogo.Dominio;
using Jogo.Dominio.Clubes;
using Jogo.Dominio.Jogadores;
using Jogo.Motor;
using Jogo.Simulacao;
using Jogo.Simulacao.Temporadas;

namespace Jogo.Aplicacao;

/// <summary>
/// Os casos de uso da interface. Guarda o estado do jogo (uma carreira por vez) e devolve DTOs prontos para a tela.
/// Nenhuma exceção escapa: tudo vira <see cref="Resposta{T}"/>.
/// </summary>
public sealed class ServicoDeJogo
{
    /// <summary>Força geral a partir da qual o jogador ganha a estrela de destaque.</summary>
    public const int ForcaDeEstrela = 85;

    private IReadOnlyDictionary<string, string>? _arquivos;
    private Carreira? _carreira;

    public Carreira? Carreira => _carreira;

    /// <summary>Lê as definições (jogo base ou patch) e devolve o resumo para escolher o clube.</summary>
    public Resposta<ResumoMundoDto> CarregarDefinicoes(IReadOnlyDictionary<string, string> arquivos)
    {
        ArgumentNullException.ThrowIfNull(arquivos);
        try
        {
            var (manifesto, mundo) = LeitorDefinicoes.Carregar(new FonteMemoria(arquivos));
            _arquivos = arquivos;
            return Resposta<ResumoMundoDto>.Sucesso(new ResumoMundoDto(
                manifesto.Nome,
                mundo.TemporadaAtual,
                mundo.Clubes.Sum(c => c.Elenco.Count),
                [.. mundo.Divisoes.Select(d => new DivisaoResumoDto(d.Id, d.Nome,
                    [.. d.ClubeIds.Select(mundo.Clube).Select(Resumir).OrderByDescending(c => c.ForcaTop11)]))]));
        }
        catch (DefinicoesInvalidasException ex)
        {
            return Resposta<ResumoMundoDto>.Falha([.. ex.Erros.Select(e => new ErroDto(e.Arquivo, e.Local, e.Mensagem))]);
        }
    }

    /// <summary>Começa uma carreira nova no clube escolhido, a partir de um mundo recém-carregado.</summary>
    public Resposta<EstadoDto> NovaCarreira(string clubeId, ulong seed)
    {
        if (_arquivos is null)
        {
            return Resposta<EstadoDto>.Falha("Carregue as definições antes de começar.");
        }

        var mundo = LeitorDefinicoes.Carregar(new FonteMemoria(_arquivos)).Mundo;
        if (!mundo.Clubes.Any(c => c.Id == clubeId))
        {
            return Resposta<EstadoDto>.Falha($"Clube \"{clubeId}\" não existe.");
        }

        _carreira = new Carreira(mundo, clubeId, seed);
        return Estado();
    }

    public Resposta<EstadoDto> Estado() => ComCarreira(c =>
    {
        var competicao = c.MinhaCompeticao;
        var tabela = c.Temporada.Tabela(competicao.Divisao.Id);
        var minhaLinha = tabela.First(l => l.ClubeId == c.Clube.Id);

        var proximo = c.ProximoJogo is { } jogo
            ? new ProximoJogoDto(jogo.Rodada,
                Resumir(c.Mundo.Clube(jogo.MandanteId == c.Clube.Id ? jogo.VisitanteId : jogo.MandanteId)),
                jogo.MandanteId == c.Clube.Id)
            : null;

        var ultimos = competicao.Disputados
            .Where(j => j.Placar.Jogo.MandanteId == c.Clube.Id || j.Placar.Jogo.VisitanteId == c.Clube.Id)
            .TakeLast(5)
            .Select(j =>
            {
                var emCasa = j.Placar.Jogo.MandanteId == c.Clube.Id;
                var adversario = c.Mundo.Clube(emCasa ? j.Placar.Jogo.VisitanteId : j.Placar.Jogo.MandanteId);
                return new ResultadoRecenteDto(j.Placar.Jogo.Rodada, adversario.Sigla, emCasa,
                    emCasa ? j.Placar.GolsMandante : j.Placar.GolsVisitante,
                    emCasa ? j.Placar.GolsVisitante : j.Placar.GolsMandante);
            })
            .ToArray();

        var ajustes = c.Temporada.Encerrada ? [] : c.MontarEscalacao().Ajustes;

        return new EstadoDto(c.Temporada.Ano, c.Temporada.RodadaAtual, c.Temporada.TotalRodadas, c.Temporada.Encerrada,
            Resumir(c.Clube), competicao.Divisao.Id, competicao.Divisao.Nome, minhaLinha.Posicao, minhaLinha.Pontos,
            proximo, ultimos, ajustes);
    });

    public Resposta<IReadOnlyList<JogadorDto>> Elenco() => ComCarreira<IReadOnlyList<JogadorDto>>(c =>
    {
        var numeros = NumerosDaTemporada(c);
        return [.. c.Clube.Elenco
            .OrderBy(j => j.PosicaoPrincipal)
            .ThenByDescending(j => j.ForcaGeral)
            .ThenBy(j => j.Id)
            .Select(j => ParaDto(j, numeros.GetValueOrDefault(j.Id)))];
    });

    public Resposta<TaticaDto> Tatica() => ComCarreira(c => ParaDto(c));

    public Resposta<TaticaDto> DefinirTatica(DefinicaoTaticaDto definicao)
    {
        ArgumentNullException.ThrowIfNull(definicao);
        return ComCarreira(c =>
        {
            var formacao = FormacoesPadrao.Todas.FirstOrDefault(f => f.Nome == definicao.Formacao)
                ?? throw new ArgumentException($"Formação \"{definicao.Formacao}\" desconhecida.");
            if (!Enum.TryParse<Mentalidade>(definicao.Mentalidade, out var mentalidade) || !Enum.IsDefined(mentalidade))
            {
                throw new ArgumentException($"Mentalidade \"{definicao.Mentalidade}\" desconhecida.");
            }

            c.DefinirTatica(new TaticaDoJogador(formacao, mentalidade, definicao.Titulares, definicao.Reservas));
            return ParaDto(c);
        });
    }

    public Resposta<TaticaDto> EscalacaoAutomatica() => ComCarreira(c =>
    {
        c.UsarEscalacaoAutomatica();
        return ParaDto(c);
    });

    public Resposta<RodadaDto> AvancarRodada() => ComCarreira(c =>
    {
        if (c.Temporada.Encerrada)
        {
            throw new InvalidOperationException("A temporada acabou: encerre-a para começar a próxima.");
        }

        return ParaDto(c, c.AvancarRodada());
    });

    public Resposta<RodadaDto> UltimaRodada() => ComCarreira(c =>
        c.UltimaRodada is { } rodada ? ParaDto(c, rodada) : new RodadaDto(0, null, []));

    public Resposta<ClassificacaoDto> Classificacao(string divisaoId) => ComCarreira(c =>
    {
        var competicao = c.Temporada.Competicao(divisaoId);
        var nomes = NomesDosJogadores(c.Mundo);
        return new ClassificacaoDto(
            competicao.Divisao.Id, competicao.Divisao.Nome, competicao.Divisao.VagasAcesso, competicao.Divisao.VagasRebaixamento,
            [.. c.Temporada.Tabela(divisaoId).Select(l =>
            {
                var clube = c.Mundo.Clube(l.ClubeId);
                return new LinhaTabelaDto(l.Posicao, clube.Id, clube.Nome, clube.Sigla, clube.Cores.Primaria, clube.Cores.Secundaria,
                    l.Jogos, l.Vitorias, l.Empates, l.Derrotas, l.GolsPro, l.GolsContra, l.Saldo, l.Pontos);
            })],
            [.. c.Temporada.Artilharia(divisaoId, 10).Select(a => ParaDto(a, c.Mundo, nomes))]);
    });

    public Resposta<FimDeTemporadaDto> EncerrarTemporada() => ComCarreira(c =>
    {
        if (!c.Temporada.Encerrada)
        {
            throw new InvalidOperationException("A temporada ainda não terminou.");
        }

        var nomes = NomesDosJogadores(c.Mundo);
        var minha = c.MinhaCompeticao;
        var minhaLinha = c.Temporada.Tabela(minha.Divisao.Id).First(l => l.ClubeId == c.Clube.Id);
        var resultado = c.EncerrarTemporada();

        var meuResumo = resultado.Divisoes.First(d => d.DivisaoId == minha.Divisao.Id);
        var movimento = meuResumo.Promovidos.Contains(c.Clube.Id) ? "acesso"
            : meuResumo.Rebaixados.Contains(c.Clube.Id) ? "rebaixamento"
            : meuResumo.CampeaoId == c.Clube.Id ? "titulo"
            : null;

        return new FimDeTemporadaDto(
            resultado.Ano,
            [.. resultado.Divisoes.Select(d => new ResumoDivisaoDto(
                d.Nome,
                c.Mundo.Clube(d.CampeaoId).Nome,
                [.. d.Promovidos.Select(id => c.Mundo.Clube(id).Nome)],
                [.. d.Rebaixados.Select(id => c.Mundo.Clube(id).Nome)],
                d.Artilheiro is { } a ? ParaDto(a, c.Mundo, nomes) : null))],
            minhaLinha.Posicao,
            minha.Divisao.Nome,
            movimento,
            resultado.Aposentados.Count,
            resultado.Revelacoes.Count,
            c.Mundo.TemporadaAtual);
    });

    // ------------------------------------------------------------------ apoio

    private Resposta<T> ComCarreira<T>(Func<Carreira, T> acao)
    {
        if (_carreira is null)
        {
            return Resposta<T>.Falha("Nenhuma carreira em andamento.");
        }

        try
        {
            return Resposta<T>.Sucesso(acao(_carreira));
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or KeyNotFoundException)
        {
            return Resposta<T>.Falha(ex.Message);
        }
    }

    internal static ClubeResumoDto Resumir(Clube c) => new(
        c.Id, c.Nome, c.Sigla, c.Cidade, c.Uf, c.Cores.Primaria, c.Cores.Secundaria,
        Math.Round(c.Elenco.OrderByDescending(j => j.ForcaGeral).Take(11).Select(j => (double)j.ForcaGeral).DefaultIfEmpty(0).Average(), 1),
        c.Elenco.Count,
        c.Torcida);

    private static JogadorDto ParaDto(Jogador j, (int Jogos, int Gols) numeros)
    {
        var a = j.Atributos;
        return new JogadorDto(
            j.Id, j.Nome, j.Idade, j.PosicaoPrincipal.Sigla(), [.. j.PosicoesSecundarias.Select(p => p.Sigla())],
            j.ForcaGeral, j.ForcaGeral >= ForcaDeEstrela, j.Condicao, j.Moral, j.Disponivel,
            j.RodadasLesionado, j.RodadasSuspenso, j.AmarelosAcumulados,
            j.Contrato?.SalarioMensal ?? 0, j.Contrato?.TemporadaFinal ?? 0, numeros.Jogos, numeros.Gols,
            new AtributosDto(a.Finalizacao, a.Passe, a.Desarme, a.Drible, a.Velocidade, a.Resistencia, a.Decisao, a.Determinacao,
                a.Reflexos, a.Posicionamento));
    }

    private static TaticaDto ParaDto(Carreira c)
    {
        var porId = c.Clube.Elenco.ToDictionary(j => j.Id);
        var posicoes = c.Tatica.Formacao.PosicoesComGoleiro;
        return new TaticaDto(
            c.Tatica.Formacao.Nome,
            c.Tatica.Mentalidade.ToString(),
            [.. posicoes.Select((p, i) =>
            {
                var id = c.Tatica.Titulares[i];
                var jogador = porId.GetValueOrDefault(id);
                // Força efetiva: já com a penalidade de improviso que o motor aplica.
                var forca = jogador is null ? 0 : (int)Math.Round(jogador.ForcaNaPosicao(p) * Setores.FatorImproviso(jogador, p));
                return new TitularDto(p.Sigla(), id, forca, jogador is not null && !jogador.JogaNaPosicao(p));
            })],
            c.Tatica.Reservas,
            [.. FormacoesPadrao.Todas.Select(f => new FormacaoDto(f.Nome, [.. f.PosicoesComGoleiro.Select(p => p.Sigla())]))],
            // Em ordem de valor (da mais defensiva à mais ofensiva); GetNames não garante isso com valores negativos.
            [.. Enum.GetValues<Mentalidade>().OrderBy(m => (int)m).Select(m => m.ToString())]);
    }

    private static RodadaDto ParaDto(Carreira c, RelatorioRodada rodada)
    {
        var nomes = NomesDosJogadores(c.Mundo);
        PartidaDto? minha = null;
        var divisoes = new List<RodadaDivisaoDto>();

        foreach (var competicao in c.Temporada.Competicoes)
        {
            if (!rodada.JogosPorDivisao.TryGetValue(competicao.Divisao.Id, out var jogos))
            {
                continue;
            }

            divisoes.Add(new RodadaDivisaoDto(competicao.Divisao.Id, competicao.Divisao.Nome, [.. jogos.Select(j =>
            {
                var mandante = c.Mundo.Clube(j.Placar.Jogo.MandanteId);
                var visitante = c.Mundo.Clube(j.Placar.Jogo.VisitanteId);
                if (mandante.Id == c.Clube.Id || visitante.Id == c.Clube.Id)
                {
                    minha = ParaDto(rodada.Rodada, mandante, visitante, j.Resultado, nomes);
                }

                return new PlacarDto(mandante.Id, mandante.Sigla, mandante.Nome, j.Placar.GolsMandante,
                    visitante.Id, visitante.Sigla, visitante.Nome, j.Placar.GolsVisitante);
            })]));
        }

        return new RodadaDto(rodada.Rodada, minha, divisoes);
    }

    private static PartidaDto ParaDto(int rodada, Clube mandante, Clube visitante, ResultadoPartida r, IReadOnlyDictionary<int, string> nomes) => new(
        rodada,
        Resumir(mandante),
        Resumir(visitante),
        r.GolsMandante,
        r.GolsVisitante,
        r.PosseMandante,
        ParaDto(r.EstatisticasMandante),
        ParaDto(r.EstatisticasVisitante),
        [.. r.Eventos.Select(e => new EventoDto(
            e.Minuto, e.Acrescimo, e.Tipo.ToString(), e.Equipe?.ToString(),
            e.JogadorId is { } a ? nomes.GetValueOrDefault(a) : null,
            e.OutroJogadorId is { } b ? nomes.GetValueOrDefault(b) : null,
            e.Origem?.ToString(),
            e.Detalhe))]);

    private static EstatisticasDto ParaDto(EstatisticasTime e) =>
        new(e.Finalizacoes, e.NoAlvo, e.Escanteios, e.Faltas, e.Amarelos, e.Vermelhos);

    private static ArtilheiroDto ParaDto(Artilheiro a, Mundo mundo, IReadOnlyDictionary<int, string> nomes) =>
        new(nomes.GetValueOrDefault(a.JogadorId) ?? $"#{a.JogadorId.ToString(CultureInfo.InvariantCulture)}",
            mundo.Clube(a.ClubeId).Sigla, a.Gols, a.Jogos);

    private static Dictionary<int, string> NomesDosJogadores(Mundo mundo) =>
        mundo.Clubes.SelectMany(c => c.Elenco).ToDictionary(j => j.Id, j => j.Nome);

    /// <summary>Jogos e gols de cada jogador na temporada atual (todas as divisões).</summary>
    private static Dictionary<int, (int Jogos, int Gols)> NumerosDaTemporada(Carreira c) =>
        c.Temporada.Competicoes
            .SelectMany(comp => comp.Disputados)
            .SelectMany(j => j.Resultado.Participacoes)
            .GroupBy(p => p.JogadorId)
            .ToDictionary(g => g.Key, g => (g.Count(), g.Sum(p => p.Gols)));
}
