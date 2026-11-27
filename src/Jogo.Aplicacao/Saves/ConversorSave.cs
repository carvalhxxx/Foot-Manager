using System.Globalization;
using System.Reflection;
using System.Text.Json;
using Jogo.Dominio;
using Jogo.Dominio.Clubes;
using Jogo.Dominio.Competicoes;
using Jogo.Dominio.Jogadores;
using Jogo.Motor;
using Jogo.Simulacao;
using Jogo.Simulacao.Temporadas;

namespace Jogo.Aplicacao.Saves;

/// <summary>Lançada quando um save não pode ser lido (corrompido, de versão mais nova ou inconsistente).</summary>
public sealed class SaveInvalidoException(string mensagem, Exception? causa = null) : Exception(mensagem, causa);

/// <summary>Converte uma <see cref="Carreira"/> em texto JSON e de volta, sem perder nada que afete o jogo.</summary>
public static class ConversorSave
{
    /// <summary>Versão 2: finanças do clube (extrato, ingresso, último jogo em casa). Saves da versão 1 continuam carregando.</summary>
    public const int VersaoAtual = 2;

    private static readonly TipoAtributo[] Atributos = Enum.GetValues<TipoAtributo>();

    public static string Salvar(Carreira carreira)
    {
        ArgumentNullException.ThrowIfNull(carreira);
        if (carreira.AoVivo is not null)
        {
            throw new InvalidOperationException("Não dá para salvar no meio de uma rodada ao vivo.");
        }

        var mundo = carreira.Mundo;
        var temporada = carreira.Temporada;
        var save = new SaveJson
        {
            VersaoFormato = VersaoAtual,
            VersaoJogo = typeof(ConversorSave).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,
            ClubeId = carreira.Clube.Id,
            Seed = carreira.Seed.ToString(CultureInfo.InvariantCulture),
            Ano = mundo.TemporadaAtual,
            Divisoes = [.. mundo.Divisoes.Select(d => new DivisaoSave
            {
                Id = d.Id, Nome = d.Nome, Nivel = d.Nivel, Clubes = [.. d.ClubeIds],
                VagasAcesso = d.VagasAcesso, VagasRebaixamento = d.VagasRebaixamento,
            })],
            Clubes = [.. mundo.Clubes.Select(ParaSave)],
            Temporada = new TemporadaSave
            {
                RodadaAtual = temporada.RodadaAtual,
                Jogos = [.. temporada.Competicoes.SelectMany(c => c.Disputados.Select(j =>
                    ParaSave(c.Divisao.Id, j, detalhado: j.Placar.Jogo.Rodada == temporada.RodadaAtual
                        && (j.Placar.Jogo.MandanteId == carreira.Clube.Id || j.Placar.Jogo.VisitanteId == carreira.Clube.Id))))],
            },
            Tatica = new TaticaSave
            {
                Formacao = carreira.Tatica.Formacao.Nome,
                Mentalidade = carreira.Tatica.Mentalidade.ToString(),
                Titulares = [.. carreira.Tatica.Titulares],
                Reservas = [.. carreira.Tatica.Reservas],
            },
        };

        return JsonSerializer.Serialize(save, ContextoSave.Default.SaveJson);
    }

    public static Carreira Carregar(string json)
    {
        SaveJson save;
        try
        {
            save = JsonSerializer.Deserialize(json, ContextoSave.Default.SaveJson)
                ?? throw new SaveInvalidoException("O arquivo de save está vazio.");
        }
        catch (JsonException ex)
        {
            throw new SaveInvalidoException("O arquivo não é um save válido do Foot Manager BR.", ex);
        }

        if (save.VersaoFormato > VersaoAtual)
        {
            throw new SaveInvalidoException("Este save foi feito numa versão mais nova do jogo. Atualize a página e tente de novo.");
        }

        if (save.VersaoFormato < 1 || save.Clubes is null || save.Divisoes is null || save.Temporada is null || save.Tatica is null)
        {
            throw new SaveInvalidoException("O arquivo não é um save válido do Foot Manager BR.");
        }

        try
        {
            var clubes = save.Clubes.Select(ParaDominio).ToArray();
            var divisoes = save.Divisoes.Select(d => new Divisao(d.Id!, d.Nome!, d.Nivel, d.Clubes ?? [], d.VagasAcesso, d.VagasRebaixamento));
            var mundo = new Mundo(save.Ano, divisoes, clubes);

            var tatica = new TaticaDoJogador(
                FormacoesPadrao.Todas.FirstOrDefault(f => f.Nome == save.Tatica.Formacao)
                    ?? throw new SaveInvalidoException($"Formação \"{save.Tatica.Formacao}\" desconhecida."),
                Enum.Parse<Mentalidade>(save.Tatica.Mentalidade!),
                save.Tatica.Titulares ?? [],
                save.Tatica.Reservas ?? []);

            var temporadaSave = save.Temporada;
            return Carreira.Restaurar(
                mundo,
                save.ClubeId!,
                ulong.Parse(save.Seed!, CultureInfo.InvariantCulture),
                t => t.Restaurar(temporadaSave.RodadaAtual, (temporadaSave.Jogos ?? []).Select(j => (j.Divisao!, ParaDominio(j)))),
                tatica);
        }
        catch (SaveInvalidoException)
        {
            throw;
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or KeyNotFoundException
                                       or FormatException or OverflowException or NullReferenceException or IndexOutOfRangeException)
        {
            throw new SaveInvalidoException($"O save está corrompido: {ex.Message}", ex);
        }
    }

    // ------------------------------------------------------------------ mundo

    private static ClubeSave ParaSave(Clube c) => new()
    {
        Id = c.Id, Nome = c.Nome, Sigla = c.Sigla, Cidade = c.Cidade, Uf = c.Uf,
        CorPrimaria = c.Cores.Primaria, CorSecundaria = c.Cores.Secundaria,
        Estadio = c.Estadio.Nome, Capacidade = c.Estadio.Capacidade,
        Torcida = c.Torcida, Reputacao = c.Reputacao, Caixa = c.Caixa,
        Elenco = [.. c.Elenco.Select(ParaSave)],
        Extrato = c.Extrato.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value, StringComparer.Ordinal),
        PrecoIngresso = c.PrecoIngresso,
        UltimoPublico = c.UltimoJogoEmCasa?.Publico,
        UltimaRenda = c.UltimoJogoEmCasa?.Renda,
    };

    private static JogadorSave ParaSave(Jogador j) => new()
    {
        Id = j.Id,
        Nome = j.Nome,
        Idade = j.Idade,
        Posicao = j.PosicaoPrincipal.Sigla(),
        Secundarias = [.. j.PosicoesSecundarias.Select(p => p.Sigla())],
        Atributos = [.. Atributos.Select(a => j.Atributos[a])],
        Ocultos = [j.Ocultos.Potencial, j.Ocultos.PropensaoLesao, j.Ocultos.Profissionalismo],
        Salario = j.Contrato?.SalarioMensal ?? 0,
        ContratoAte = j.Contrato?.TemporadaFinal ?? 0,
        Condicao = j.Condicao,
        Moral = j.Moral,
        Lesao = j.RodadasLesionado,
        Suspensao = j.RodadasSuspenso,
        Amarelos = j.AmarelosAcumulados,
        Sequencia = j.SequenciaComoTitular,
    };

    private static Clube ParaDominio(ClubeSave s)
    {
        var clube = new Clube(s.Id!, s.Nome!, s.Sigla!, s.Cidade!, s.Uf!, new Cores(s.CorPrimaria!, s.CorSecundaria!),
            new Estadio(s.Estadio!, s.Capacidade), s.Torcida, s.Reputacao, s.Caixa);
        foreach (var j in s.Elenco ?? [])
        {
            clube.AdicionarAoElenco(ParaDominio(j, clube.Id));
        }

        clube.RestaurarFinancas(
            (s.Extrato ?? []).ToDictionary(kv => Enum.Parse<CategoriaFinanceira>(kv.Key), kv => kv.Value),
            s.PrecoIngresso,
            s.UltimoPublico is { } publico ? (publico, s.UltimaRenda ?? 0) : null);
        return clube;
    }

    private static Jogador ParaDominio(JogadorSave s, string clubeId)
    {
        var a = s.Atributos!;
        var atributos = new Atributos
        {
            Finalizacao = a[(int)TipoAtributo.Finalizacao],
            Passe = a[(int)TipoAtributo.Passe],
            Desarme = a[(int)TipoAtributo.Desarme],
            Drible = a[(int)TipoAtributo.Drible],
            Velocidade = a[(int)TipoAtributo.Velocidade],
            Resistencia = a[(int)TipoAtributo.Resistencia],
            Decisao = a[(int)TipoAtributo.Decisao],
            Determinacao = a[(int)TipoAtributo.Determinacao],
            Reflexos = a[(int)TipoAtributo.Reflexos],
            Posicionamento = a[(int)TipoAtributo.Posicionamento],
        };
        var ocultos = new AtributosOcultos { Potencial = s.Ocultos![0], PropensaoLesao = s.Ocultos[1], Profissionalismo = s.Ocultos[2] };

        var jogador = new Jogador(s.Id, s.Nome!, s.Idade, Sigla(s.Posicao), (s.Secundarias ?? []).Select(Sigla), atributos, ocultos);
        if (s.Salario > 0)
        {
            jogador.AssinarContrato(new Contrato(clubeId, s.Salario, s.ContratoAte));
        }

        jogador.RestaurarEstado(s.Condicao, s.Moral, s.Lesao, s.Suspensao, s.Amarelos, s.Sequencia);
        return jogador;
    }

    private static Posicao Sigla(string? sigla) =>
        PosicaoExtensoes.TentarDaSigla(sigla, out var p) ? p : throw new SaveInvalidoException($"Posição \"{sigla}\" desconhecida.");

    // ------------------------------------------------------------------ jogos

    private static JogoSave ParaSave(string divisaoId, JogoDisputado j, bool detalhado)
    {
        var r = j.Resultado;
        var placar = j.Placar;
        int[] Participantes(Equipe e) => [.. r.Participacoes.Where(p => p.Equipe == e).Select(p => p.JogadorId)];
        int[] Autores(Equipe e) => [.. r.Participacoes.Where(p => p.Equipe == e).SelectMany(p => Enumerable.Repeat(p.JogadorId, p.Gols))];

        return new JogoSave
        {
            Divisao = divisaoId,
            Rodada = placar.Jogo.Rodada,
            Mandante = placar.Jogo.MandanteId,
            Visitante = placar.Jogo.VisitanteId,
            GolsMandante = placar.GolsMandante,
            GolsVisitante = placar.GolsVisitante,
            AmarelosMandante = placar.AmarelosMandante,
            AmarelosVisitante = placar.AmarelosVisitante,
            VermelhosMandante = placar.VermelhosMandante,
            VermelhosVisitante = placar.VermelhosVisitante,
            ParticipantesMandante = Participantes(Equipe.Mandante),
            ParticipantesVisitante = Participantes(Equipe.Visitante),
            AutoresMandante = Autores(Equipe.Mandante),
            AutoresVisitante = Autores(Equipe.Visitante),
            Detalhe = detalhado
                ? new DetalheSave
                {
                    EstatisticasMandante = Numeros(r.EstatisticasMandante),
                    EstatisticasVisitante = Numeros(r.EstatisticasVisitante),
                    Eventos = [.. r.Eventos.Select(e => new EventoSave
                    {
                        Minuto = e.Minuto, Acrescimo = e.Acrescimo, Tipo = e.Tipo.ToString(), Equipe = e.Equipe?.ToString(),
                        Jogador = e.JogadorId, Outro = e.OutroJogadorId, Origem = e.Origem?.ToString(), Detalhe = e.Detalhe,
                    })],
                }
                : null,
        };
    }

    private static int[] Numeros(EstatisticasTime e) =>
        [e.Gols, e.MinutosComPosse, e.Finalizacoes, e.NoAlvo, e.Escanteios, e.Faltas, e.Amarelos, e.Vermelhos];

    private static JogoDisputado ParaDominio(JogoSave s)
    {
        var jogo = new JogoAgendado(s.Rodada, s.Mandante!, s.Visitante!);
        var placar = new PlacarJogo(jogo, s.GolsMandante, s.GolsVisitante, s.AmarelosMandante, s.AmarelosVisitante,
            s.VermelhosMandante, s.VermelhosVisitante);

        IEnumerable<ParticipacaoJogador> Participacoes(int[]? ids, int[]? autores, Equipe equipe) =>
            (ids ?? []).Select(id => new ParticipacaoJogador(id, equipe, 0, 0, (autores ?? []).Count(a => a == id), 0, false, 0));

        var participacoes = Participacoes(s.ParticipantesMandante, s.AutoresMandante, Equipe.Mandante)
            .Concat(Participacoes(s.ParticipantesVisitante, s.AutoresVisitante, Equipe.Visitante))
            .ToArray();

        EstatisticasTime Estatisticas(int[]? n, int gols, int amarelos, int vermelhos) =>
            n is { Length: 8 }
                ? new EstatisticasTime(n[0], n[1], n[2], n[3], n[4], n[5], n[6], n[7])
                : new EstatisticasTime(gols, 0, 0, 0, 0, 0, amarelos, vermelhos);

        var eventos = s.Detalhe?.Eventos?.Select(e => new EventoPartida(
            e.Minuto, e.Acrescimo, Enum.Parse<TipoEvento>(e.Tipo!),
            e.Equipe is null ? null : Enum.Parse<Equipe>(e.Equipe),
            e.Jogador, e.Outro,
            e.Origem is null ? null : Enum.Parse<OrigemLance>(e.Origem),
            e.Detalhe)).ToArray() ?? [];

        var resultado = new ResultadoPartida(s.Mandante!, s.Visitante!, s.GolsMandante, s.GolsVisitante, eventos,
            Estatisticas(s.Detalhe?.EstatisticasMandante, s.GolsMandante, s.AmarelosMandante, s.VermelhosMandante),
            Estatisticas(s.Detalhe?.EstatisticasVisitante, s.GolsVisitante, s.AmarelosVisitante, s.VermelhosVisitante),
            participacoes);

        return new JogoDisputado(placar, resultado);
    }
}
