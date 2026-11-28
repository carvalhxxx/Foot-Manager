using System.Globalization;
using Jogo.Aplicacao.Saves;
using Jogo.Dados.Definicoes;
using Jogo.Dominio;
using Jogo.Dominio.Clubes;
using Jogo.Dominio.Jogadores;
using Jogo.Motor;
using Jogo.Simulacao.Copas;
using Jogo.Simulacao;
using Jogo.Simulacao.Mercado;
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

        var ajustes = c.Temporada.Encerrada || c.AoVivo is not null ? [] : c.MontarEscalacao().Ajustes;

        return new EstadoDto(c.Temporada.Ano, c.Temporada.RodadaAtual, c.Temporada.TotalRodadas, c.Temporada.Encerrada,
            Resumir(c.Clube), competicao.Divisao.Id, competicao.Divisao.Nome, minhaLinha.Posicao, minhaLinha.Pontos,
            proximo, ultimos, ajustes, c.AoVivo is not null, c.JanelaAberta,
            // Aviso só na reta final (últimas 8 rodadas), quando ainda dá para renovar.
            c.Temporada.RodadaAtual >= c.Temporada.TotalRodadas - 8
                ? c.Clube.Elenco.Count(j => Contratos.PodeRenovar(j, c.Mundo.TemporadaAtual))
                : 0,
            CopaDe(c));
    });

    private static CopaEstadoDto? CopaDe(Carreira c)
    {
        if (c.Temporada.Copa is not { } copa)
        {
            return null;
        }

        if (copa.Campeao == c.Clube.Id)
        {
            return new CopaEstadoDto("campeao", null, null);
        }

        if (copa.EliminadoEm(c.Clube.Id) is { } fase)
        {
            return new CopaEstadoDto("eliminado", fase.ToString(), null);
        }

        var proximo = copa.ProximoJogo(c.Clube.Id) is { } p
            ? new ProximoJogoCopaDto(p.Fase.ToString(), p.Perna, CopaDoBrasil.RodadaDaEtapa(p.Fase, p.Perna),
                Resumir(c.Mundo.Clube(p.Adversario)), p.EmCasa)
            : null;
        return new CopaEstadoDto("vivo", null, proximo);
    }

    /// <summary>Chaveamento da Copa do Brasil da temporada, fase a fase.</summary>
    public Resposta<CopaDto> Copa() => ComCarreira(c =>
    {
        var copa = c.Temporada.Copa ?? throw new InvalidOperationException("Esta temporada não tem Copa do Brasil.");
        var fases = copa.FasesDefinidas.Select(f => new FaseCopaDto(
            f.ToString(), CopaDoBrasil.JogoUnico(f), CopaDoBrasil.RodadaDaEtapa(f, 1),
            CopaDoBrasil.JogoUnico(f) ? null : CopaDoBrasil.RodadaDaEtapa(f, 2),
            [.. copa.Confrontos(f).Select(x =>
            {
                var a = c.Mundo.Clube(x.ClubeA);
                var b = c.Mundo.Clube(x.ClubeB);
                var decisivo = x.JogoUnico ? x.Ida : x.Volta;
                int? penA = null, penB = null;
                if (decisivo?.PenaltisMandante is { } pm && decisivo.PenaltisVisitante is { } pv)
                {
                    (penA, penB) = decisivo.MandanteId == x.ClubeA ? (pm, pv) : (pv, pm);
                }

                return new ConfrontoCopaDto(a.Nome, a.Sigla, b.Nome, b.Sigla,
                    x.Ida?.GolsMandante, x.Ida?.GolsVisitante, x.Volta?.GolsVisitante, x.Volta?.GolsMandante,
                    penA, penB, x.Vencedor is { } v ? c.Mundo.Clube(v).Sigla : null,
                    x.ClubeA == c.Clube.Id || x.ClubeB == c.Clube.Id);
            })])).ToArray();
        return new CopaDto(fases, [.. copa.Cabecas.Select(id => c.Mundo.Clube(id).Nome)],
            copa.Campeao is { } campeao ? c.Mundo.Clube(campeao).Nome : null);
    });

    public Resposta<IReadOnlyList<JogadorDto>> Elenco() => ComCarreira<IReadOnlyList<JogadorDto>>(c =>
    {
        var numeros = NumerosDaTemporada(c);
        return [.. c.Clube.Elenco
            .OrderBy(j => j.PosicaoPrincipal)
            .ThenByDescending(j => j.ForcaGeral)
            .ThenBy(j => j.Id)
            .Select(j => ParaDto(j, numeros.GetValueOrDefault(j.Id), c.Mundo.TemporadaAtual))];
    });

    /// <summary>Renova o contrato (1 a 4 anos) pelo salário pedido e devolve o elenco atualizado.</summary>
    public Resposta<IReadOnlyList<JogadorDto>> RenovarContrato(int jogadorId, int anos)
    {
        var r = ComCarreira(c =>
        {
            c.RenovarContrato(jogadorId, anos);
            return 0;
        });
        return r.Ok ? Elenco() : Resposta<IReadOnlyList<JogadorDto>>.Falha([.. r.Erros!]);
    }

    /// <summary>Dispensa o jogador (paga 3 salários) e devolve o elenco atualizado.</summary>
    public Resposta<IReadOnlyList<JogadorDto>> Dispensar(int jogadorId)
    {
        var r = ComCarreira(c =>
        {
            c.Dispensar(jogadorId);
            return 0;
        });
        return r.Ok ? Elenco() : Resposta<IReadOnlyList<JogadorDto>>.Falha([.. r.Erros!]);
    }

    /// <summary>Jogadores à venda (outros clubes e agentes livres) que passam nos filtros; os 60 mais fortes.</summary>
    public Resposta<MercadoDto> Mercado(BuscaMercadoDto busca)
    {
        ArgumentNullException.ThrowIfNull(busca);
        return ComCarreira(c =>
        {
            Posicao? posicao = null;
            if (!string.IsNullOrWhiteSpace(busca.Posicao))
            {
                posicao = PosicaoExtensoes.TentarDaSigla(busca.Posicao, out var p) ? p : throw new ArgumentException($"Posição \"{busca.Posicao}\" desconhecida.");
            }

            var ano = c.Mundo.TemporadaAtual;
            var candidatos = (busca.ApenasLivres ? [] : c.Mundo.Clubes.Where(cl => cl != c.Clube).SelectMany(cl => cl.Elenco))
                .Concat(c.Mundo.AgentesLivres)
                .Where(j => posicao is null || j.PosicaoPrincipal == posicao)
                .Where(j => j.ForcaGeral >= busca.ForcaMinima)
                .Where(j => busca.PrecoMaximo is not { } max || Transferencias.Preco(j, ano) <= max)
                .OrderByDescending(j => j.ForcaGeral).ThenBy(j => j.Id)
                .Take(60)
                .Select(j =>
                {
                    var dono = j.Contrato is { } ct ? c.Mundo.Clube(ct.ClubeId) : null;
                    return new JogadorMercadoDto(j.Id, j.Nome, j.Idade, j.PosicaoPrincipal.Sigla(), j.ForcaGeral, j.ForcaGeral >= ForcaDeEstrela,
                        dono?.Nome, dono?.Sigla, dono is null, Transferencias.Preco(j, ano), Valores.SalarioPedido(j),
                        Valores.ValorDeMercado(j, ano), Transferencias.AnosDeContrato(j),
                        c.Temporada.Encerrada ? "A temporada acabou: comece a próxima para negociar."
                            : Transferencias.Impedimento(c.Mundo, c.Clube, j, c.Temporada.RodadaAtual));
                });

            return new MercadoDto(c.JanelaAberta, c.Clube.Caixa, c.Clube.Elenco.Count,
                [.. Enum.GetValues<Posicao>().Select(p => p.Sigla())], [.. candidatos]);
        });
    }

    /// <summary>Compra (ou contrata o agente livre) e devolve o elenco atualizado.</summary>
    public Resposta<IReadOnlyList<JogadorDto>> Comprar(int jogadorId)
    {
        var r = ComCarreira(c =>
        {
            c.Comprar(jogadorId);
            return 0;
        });
        return r.Ok ? Elenco() : Resposta<IReadOnlyList<JogadorDto>>.Falha([.. r.Erros!]);
    }

    /// <summary>Melhor proposta da IA por um jogador do elenco (sem vender).</summary>
    public Resposta<OfertaDto> OfertaPor(int jogadorId) => ComCarreira(c =>
    {
        var jogador = c.Clube.Elenco.FirstOrDefault(j => j.Id == jogadorId) ?? throw new KeyNotFoundException("Esse jogador não é do seu elenco.");
        var oferta = Transferencias.MelhorOferta(c.Mundo, c.Clube, jogador);
        return new OfertaDto(jogadorId, oferta?.Clube.Nome, oferta?.Valor ?? 0, c.JanelaAberta);
    });

    /// <summary>Vende para a melhor proposta e devolve o elenco atualizado.</summary>
    public Resposta<IReadOnlyList<JogadorDto>> Vender(int jogadorId)
    {
        var r = ComCarreira(c =>
        {
            c.Vender(jogadorId);
            return 0;
        });
        return r.Ok ? Elenco() : Resposta<IReadOnlyList<JogadorDto>>.Falha([.. r.Erros!]);
    }

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

            var marcacao = definicao.Marcacao is null ? c.Tatica.Marcacao : LerEnum<Marcacao>(definicao.Marcacao, "Marcação");
            var concentracao = definicao.Concentracao is null ? c.Tatica.Concentracao : LerEnum<Concentracao>(definicao.Concentracao, "Concentração");
            c.DefinirTatica(new TaticaDoJogador(formacao, mentalidade, definicao.Titulares, definicao.Reservas,
                marcacao, concentracao, definicao.CapitaoId, definicao.CobradorId));
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
        c.UltimaRodada is { } rodada ? ParaDto(c, rodada) : new RodadaDto(0, null, [], []));

    // ------------------------------------------------------------------ finanças

    public Resposta<FinancasDto> Financas() => ComCarreira(FinancasDe);

    /// <summary>Define o preço do ingresso dos jogos em casa (nulo volta ao preço sugerido).</summary>
    public Resposta<FinancasDto> DefinirPrecoIngresso(decimal? preco) => ComCarreira(c =>
    {
        c.Clube.DefinirPrecoIngresso(preco);
        return FinancasDe(c);
    });

    private static FinancasDto FinancasDe(Carreira c)
    {
        var clube = c.Clube;
        var regras = c.Temporada.RegrasFinanceiras;
        return new FinancasDto(
            clube.Caixa,
            clube.Extrato.Values.Sum(),
            [.. clube.Extrato.Select(kv => new LancamentoDto(kv.Key.ToString(), kv.Value))],
            Simulacao.Financas.Financas.FolhaMensal(clube),
            Simulacao.Financas.Financas.CotaTvAnual(clube, c.MinhaCompeticao.Divisao, regras),
            Simulacao.Financas.Financas.PatrocinioAnual(clube, regras),
            Simulacao.Financas.Financas.PrecoDoIngresso(clube, regras),
            Simulacao.Financas.Financas.PrecoSugerido(clube, regras),
            clube.PrecoIngresso is not null,
            clube.Estadio.Capacidade,
            clube.UltimoJogoEmCasa?.Publico,
            clube.UltimoJogoEmCasa?.Renda);
    }

    // ------------------------------------------------------------------ saves

    /// <summary>Gera o save da carreira atual (não dá durante uma rodada ao vivo).</summary>
    public Resposta<SaveDto> ExportarSave() => ComCarreira(c => new SaveDto(
        ConversorSave.Salvar(c),
        c.Clube.Id,
        c.Clube.Nome,
        c.Clube.Cores.Primaria,
        c.Clube.Cores.Secundaria,
        c.Temporada.Ano,
        c.Temporada.RodadaAtual,
        c.MinhaCompeticao.Divisao.Nome));

    /// <summary>Retoma uma carreira a partir de um save (não precisa das definições: o save tem o mundo inteiro).</summary>
    public Resposta<EstadoDto> CarregarSave(string conteudo)
    {
        try
        {
            _carreira = ConversorSave.Carregar(conteudo);
            return Estado();
        }
        catch (SaveInvalidoException ex)
        {
            return Resposta<EstadoDto>.Falha(ex.Message);
        }
    }

    // ------------------------------------------------------------------ ao vivo

    /// <summary>Começa a rodada ao vivo (o jogador decide pelo seu time durante o jogo).</summary>
    public Resposta<AoVivoDto> IniciarAoVivo() => ComCarreira(c =>
    {
        if (c.Temporada.Encerrada)
        {
            throw new InvalidOperationException("A temporada acabou: encerre-a para começar a próxima.");
        }

        if (c.AoVivo is null)
        {
            c.IniciarRodadaAoVivo();
        }

        return FotoAoVivo(c, null);
    });

    public Resposta<AoVivoDto> AoVivo() => ComCarreira(c =>
        c.AoVivo is null ? throw new InvalidOperationException("Não há rodada ao vivo.") : FotoAoVivo(c, null));

    /// <summary>
    /// Avança até <paramref name="minutos"/> minutos. Para antes se algo pedir a atenção do jogador
    /// no jogo dele (intervalo, lesão, expulsão, fim do jogo) ou se a rodada acabar.
    /// </summary>
    public Resposta<AoVivoDto> AvancarAoVivo(int minutos) => ComCarreira(c =>
    {
        var rodada = c.AoVivo ?? throw new InvalidOperationException("Não há rodada ao vivo.");
        string? pausa = null;

        for (var i = 0; i < Math.Clamp(minutos, 1, 300) && pausa is null && !rodada.Encerrada; i++)
        {
            var meuJogoJaAcabou = c.MeuJogoAoVivo?.Partida.Encerrada ?? true;
            var novos = c.AvancarMinutoAoVivo();
            pausa = MotivoDePausa(c, novos)
                    ?? (!meuJogoJaAcabou && c.MeuJogoAoVivo!.Partida.Encerrada ? "fim" : null);
        }

        return FotoAoVivo(c, pausa ?? (rodada.Encerrada ? "fim" : null));
    });

    public Resposta<AoVivoDto> DecidirAoVivo(DecisaoDto decisao)
    {
        ArgumentNullException.ThrowIfNull(decisao);
        return ComCarreira(c =>
        {
            switch (decisao.Tipo)
            {
                case "substituicao":
                    c.DecidirAoVivo(new Substituicao(
                        decisao.SaiId ?? throw new ArgumentException("Informe quem sai."),
                        decisao.EntraId ?? throw new ArgumentException("Informe quem entra.")));
                    break;
                case "mentalidade":
                    c.DecidirAoVivo(new TrocaDeMentalidade(LerMentalidade(decisao.Mentalidade)));
                    break;
                case "estilo":
                    c.DecidirAoVivo(new TrocaDeEstilo(
                        LerEnum<Marcacao>(decisao.Marcacao, "Marcação"),
                        LerEnum<Concentracao>(decisao.Concentracao, "Concentração")));
                    break;
                case "formacao":
                    c.TrocarFormacaoAoVivo(FormacoesPadrao.Todas.FirstOrDefault(f => f.Nome == decisao.Formacao)
                        ?? throw new ArgumentException($"Formação \"{decisao.Formacao}\" desconhecida."));
                    break;
                default:
                    throw new ArgumentException($"Decisão \"{decisao.Tipo}\" desconhecida.");
            }

            return FotoAoVivo(c, null);
        });
    }

    /// <summary>Termina o que faltar dos jogos e fecha a rodada ao vivo.</summary>
    public Resposta<RodadaDto> ConcluirAoVivo() => ComCarreira(c =>
    {
        var rodada = c.AoVivo ?? throw new InvalidOperationException("Não há rodada ao vivo.");
        rodada.AvancarAteOFim();
        return ParaDto(c, c.ConcluirRodadaAoVivo());
    });

    private static string? MotivoDePausa(Carreira c, IReadOnlyList<EventoPartida> novos)
    {
        // Rodada sem jogo do meu clube (ex.: playoff da Série B para quem está na A): nada pede pausa.
        if (c.MeuJogoAoVivo is not { } meu)
        {
            return null;
        }

        var minhaEquipe = meu.Mandante.Id == c.Clube.Id ? Equipe.Mandante : Equipe.Visitante;
        foreach (var e in novos)
        {
            switch (e.Tipo)
            {
                case TipoEvento.Intervalo:
                    return "intervalo";
                case TipoEvento.Lesao when e.Equipe == minhaEquipe:
                    return "lesao";
                case TipoEvento.CartaoVermelho when e.Equipe == minhaEquipe:
                    return "expulsao";
            }
        }

        return null;
    }

    private static AoVivoDto FotoAoVivo(Carreira c, string? pausa)
    {
        var rodada = c.AoVivo!;
        var nomes = NomesDosJogadores(c.Mundo);

        MinhaPartidaAoVivoDto? minha = null;
        if (c.MeuJogoAoVivo is { } jogo && c.MeuTimeAoVivo is { } time)
        {
            var p = jogo.Partida;
            var (minuto, acrescimo) = p.Relogio;
            var posseTotal = p.Mandante.Estatisticas.MinutosComPosse + p.Visitante.Estatisticas.MinutosComPosse;
            minha = new MinhaPartidaAoVivoDto(
                Resumir(jogo.Mandante),
                Resumir(jogo.Visitante),
                jogo.Mandante.Id == c.Clube.Id,
                p.Mandante.Estatisticas.Gols,
                p.Visitante.Estatisticas.Gols,
                p.Tempo,
                minuto,
                acrescimo,
                p.Encerrada,
                posseTotal == 0 ? 50 : (int)Math.Round(100.0 * p.Mandante.Estatisticas.MinutosComPosse / posseTotal),
                ParaDto(p.Mandante.Estatisticas),
                ParaDto(p.Visitante.Estatisticas),
                time.Formacao.Nome,
                time.Mentalidade.ToString(),
                time.Marcacao.ToString(),
                time.Concentracao.ToString(),
                p.Configuracao.MaximoSubstituicoes - time.SubstituicoesFeitas,
                [.. time.EmCampo.Select(j => new JogadorEmCampoDto(j.Jogador.Id, j.Jogador.Nome, j.Posicao.Sigla(),
                    (int)Math.Round(j.ForcaBase), (int)Math.Round(j.Condicao), j.Amarelos, j.Gols))],
                [.. time.Reservas.Select(r => new ReservaDto(r.Id, r.Nome, r.PosicaoPrincipal.Sigla(), r.ForcaGeral, r.Condicao))],
                [.. time.VagasEmAberto.Select(v => new VagaDto(v.JogadorId, nomes.GetValueOrDefault(v.JogadorId) ?? "", v.Posicao.Sigla()))],
                [.. p.Eventos.Select(e => ParaDto(e, nomes))]);
        }

        var placares = rodada.Jogos
            .GroupBy(j => j.Competicao)
            .Select(g => new RodadaDivisaoAoVivoDto(g.Key.Divisao.Id, g.Key.Divisao.Nome, [.. g.Select(j =>
            {
                var (m, a) = j.Partida.Relogio;
                return new PlacarAoVivoDto(j.Mandante.Id, j.Mandante.Sigla, j.Partida.Mandante.Estatisticas.Gols,
                    j.Visitante.Id, j.Visitante.Sigla, j.Partida.Visitante.Estatisticas.Gols, m, a, j.Partida.Encerrada);
            })]))
            .ToArray();

        return new AoVivoDto(rodada.Rodada, rodada.Encerrada, pausa, minha, placares);
    }

    private static T LerEnum<T>(string? nome, string rotulo)
        where T : struct, Enum =>
        Enum.TryParse<T>(nome, out var valor) && Enum.IsDefined(valor) && !int.TryParse(nome, out _)
            ? valor
            : throw new ArgumentException($"{rotulo} \"{nome}\" desconhecida.");

    private static Mentalidade LerMentalidade(string? nome) =>
        Enum.TryParse<Mentalidade>(nome, out var m) && Enum.IsDefined(m)
            ? m
            : throw new ArgumentException($"Mentalidade \"{nome}\" desconhecida.");

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
            [.. c.Temporada.Artilharia(divisaoId, 10).Select(a => ParaDto(a, c.Mundo, nomes))],
            competicao.Divisao.VagasDiretas,
            [.. c.Temporada.Playoff(divisaoId).Select(p =>
            {
                var melhor = c.Mundo.Clube(p.MelhorId);
                var pior = c.Mundo.Clube(p.PiorId);
                return new ConfrontoPlayoffDto(p.MelhorPosicao, melhor.Nome, melhor.Sigla, p.PiorPosicao, pior.Nome, pior.Sigla,
                    p.Ida?.GolsVisitante, p.Ida?.GolsMandante, p.Volta?.GolsMandante, p.Volta?.GolsVisitante,
                    p.VencedorId is { } v ? c.Mundo.Clube(v).Nome : null);
            })],
            [.. ZonasDaTabela.De(competicao.Divisao).Select(z => new ZonaDto(z.Tipo.ToString(), z.De, z.Ate))],
            ZonasDaTabela.CriteriosDeDesempate);
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
            c.Mundo.TemporadaAtual,
            resultado.Extratos[c.Clube.Id].Values.Sum(),
            resultado.CampeaoCopa is { } cc ? c.Mundo.Clube(cc).Nome : null,
            resultado.ViceCopa is { } vc ? c.Mundo.Clube(vc).Nome : null);
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

    private static JogadorDto ParaDto(Jogador j, (int Jogos, int Gols) numeros, int temporadaAtual)
    {
        var a = j.Atributos;
        return new JogadorDto(
            j.Id, j.Nome, j.Idade, j.PosicaoPrincipal.Sigla(), [.. j.PosicoesSecundarias.Select(p => p.Sigla())],
            j.ForcaGeral, j.ForcaGeral >= ForcaDeEstrela, j.Condicao, j.Moral, j.Disponivel,
            j.RodadasLesionado, j.RodadasSuspenso, j.AmarelosAcumulados,
            j.Contrato?.SalarioMensal ?? 0, j.Contrato?.TemporadaFinal ?? 0, numeros.Jogos, numeros.Gols,
            new AtributosDto(a.Finalizacao, a.Passe, a.Desarme, a.Drible, a.Velocidade, a.Resistencia, a.Decisao, a.Determinacao,
                a.Reflexos, a.Posicionamento),
            Valores.ValorDeMercado(j, temporadaAtual),
            Contratos.PodeRenovar(j, temporadaAtual),
            Valores.SalarioPedido(j),
            Contratos.CustoDaDispensa(j));
    }

    private static TaticaDto ParaDto(Carreira c)
    {
        var porId = c.Clube.Elenco.ToDictionary(j => j.Id);
        var posicoes = c.Tatica.Formacao.PosicoesComGoleiro;
        // Na ordem da formação (o primeiro é o goleiro), só quem ainda é do elenco.
        var titularesDaTatica = c.Tatica.Titulares.Select(id => porId.GetValueOrDefault(id)).OfType<Jogador>().ToArray();
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
            [.. Enum.GetValues<Mentalidade>().OrderBy(m => (int)m).Select(m => m.ToString())],
            c.Tatica.Marcacao.ToString(),
            c.Tatica.Concentracao.ToString(),
            [.. Enum.GetValues<Marcacao>().OrderBy(m => (int)m).Select(m => m.ToString())],
            [.. Enum.GetValues<Concentracao>().Select(m => m.ToString())],
            c.Tatica.CapitaoId,
            c.Tatica.CobradorId,
            c.Tatica.CapitaoId ?? TimeEscalado.CapitaoAutomatico(titularesDaTatica)?.Id,
            // Os 3 melhores em Finalização + Passe, para ajudar a escolher.
            [.. titularesDaTatica.Skip(1).OrderByDescending(j => j.Atributos.Finalizacao + j.Atributos.Passe).ThenBy(j => j.Id).Take(3).Select(j => j.Id)]);
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

        JogoCopaDto? minhaCopa = null;
        var placaresCopa = new List<PlacarDto>();
        foreach (var j in rodada.JogosDaCopa)
        {
            var mandante = c.Mundo.Clube(j.MandanteId);
            var visitante = c.Mundo.Clube(j.VisitanteId);
            placaresCopa.Add(new PlacarDto(mandante.Id, mandante.Sigla, mandante.Nome, j.GolsMandante,
                visitante.Id, visitante.Sigla, visitante.Nome, j.GolsVisitante));
            if ((mandante.Id == c.Clube.Id || visitante.Id == c.Clube.Id) && j.Resultado is { } resultado)
            {
                var confronto = c.Temporada.Copa!.Confrontos(j.Fase)[j.Confronto];
                minhaCopa = new JogoCopaDto(j.Fase.ToString(), j.Perna, ParaDto(rodada.Rodada, mandante, visitante, resultado, nomes),
                    j.PenaltisMandante, j.PenaltisVisitante, confronto.Vencedor == c.Clube.Id, confronto.Vencedor is not null);
            }
        }

        return new RodadaDto(rodada.Rodada, minha, divisoes, [.. c.UltimasTransferencias.Select(t =>
        {
            var para = c.Mundo.Clube(t.ParaClubeId);
            return new TransferenciaDto(t.Jogador, t.DeClubeId is { } de ? c.Mundo.Clube(de).Sigla : null, para.Sigla, para.Nome, t.Valor);
        })], minhaCopa, placaresCopa);
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
        [.. r.Eventos.Select(e => ParaDto(e, nomes))]);

    private static EventoDto ParaDto(EventoPartida e, IReadOnlyDictionary<int, string> nomes) => new(
        e.Minuto, e.Acrescimo, e.Tipo.ToString(), e.Equipe?.ToString(),
        e.JogadorId is { } a ? nomes.GetValueOrDefault(a) : null,
        e.OutroJogadorId is { } b ? nomes.GetValueOrDefault(b) : null,
        e.Origem?.ToString(),
        e.Detalhe);

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
