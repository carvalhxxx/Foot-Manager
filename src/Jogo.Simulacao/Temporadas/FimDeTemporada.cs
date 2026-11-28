using Jogo.Dominio;
using Jogo.Dominio.Aleatoriedade;
using Jogo.Dominio.Clubes;
using Jogo.Dominio.Competicoes;
using Jogo.Dominio.Jogadores;
using Jogo.Simulacao.Financas;
using Jogo.Simulacao.Geracao;
using Jogo.Simulacao.Mercado;

namespace Jogo.Simulacao.Temporadas;

/// <summary>Resumo de uma divisão ao fim da temporada.</summary>
public sealed record ResumoDivisao(
    string DivisaoId,
    string Nome,
    string CampeaoId,
    IReadOnlyList<string> Promovidos,
    IReadOnlyList<string> Rebaixados,
    Artilheiro? Artilheiro,
    IReadOnlyList<LinhaTabela> TabelaFinal);

/// <summary>O que aconteceu na virada de temporada.</summary>
public sealed record ResultadoTemporada(
    int Ano,
    IReadOnlyList<ResumoDivisao> Divisoes,
    IReadOnlyList<int> Aposentados,
    IReadOnlyList<int> Revelacoes,
    IReadOnlyDictionary<string, IReadOnlyDictionary<CategoriaFinanceira, decimal>> Extratos,
    string? CampeaoCopa = null,
    string? ViceCopa = null);

/// <summary>
/// Fecha a temporada: acesso e rebaixamento, evolução, aposentadorias, revelações da base,
/// envelhecimento e preparação do elenco para o ano seguinte.
/// </summary>
public static class FimDeTemporada
{
    public const int ElencoMinimo = 25;
    public const int ElencoMaximo = 30;
    public const int IdadeLimite = 39;

    /// <param name="clubeDoJogador">Clube controlado pelo jogador: os contratos dele que vencem não são renovados pela IA.</param>
    public static ResultadoTemporada Encerrar(Temporada temporada, Mundo mundo, ulong seed, string? clubeDoJogador = null)
    {
        ArgumentNullException.ThrowIfNull(temporada);
        ArgumentNullException.ThrowIfNull(mundo);
        if (!temporada.Encerrada)
        {
            throw new InvalidOperationException("A temporada ainda não terminou.");
        }

        if (temporada.Ano != mundo.TemporadaAtual)
        {
            throw new InvalidOperationException("Esta temporada já foi encerrada.");
        }

        var aleatorio = new GeradorAleatorio(seed ^ GeradorDeMundo.HashEstavel($"fim/{temporada.Ano}"));
        PagarPremiacao(temporada, mundo);

        // O extrato da temporada vai para o resultado e é zerado para o ano seguinte.
        var extratos = mundo.Clubes.ToDictionary(
            c => c.Id,
            c => (IReadOnlyDictionary<CategoriaFinanceira, decimal>)new SortedDictionary<CategoriaFinanceira, decimal>(c.Extrato.ToDictionary()),
            StringComparer.Ordinal);
        foreach (var clube in mundo.Clubes)
        {
            clube.ComecarNovaTemporadaFinanceira();
        }

        var resumos = Movimentar(temporada, mundo);
        var aposentados = new List<int>();
        var revelacoes = new List<int>();
        var proximoId = mundo.MaiorIdDeJogador + 1;

        foreach (var clube in mundo.Clubes)
        {
            foreach (var jogador in clube.Elenco.ToArray())
            {
                if (Aposenta(jogador, aleatorio))
                {
                    clube.RemoverDoElenco(jogador.Id);
                    jogador.EncerrarContrato();
                    aposentados.Add(jogador.Id);
                    continue;
                }

                Evolucao.Aplicar(jogador, Evolucao.SortearVariacao(jogador, aleatorio), aleatorio);
                jogador.Envelhecer();
                jogador.ComecarNovaTemporada();
            }
        }

        // Agentes livres envelhecem e se aposentam como os demais (antes de chegarem os novos).
        foreach (var livre in mundo.AgentesLivres.ToArray())
        {
            if (Aposenta(livre, aleatorio))
            {
                mundo.RemoverAgenteLivre(livre.Id);
                aposentados.Add(livre.Id);
                continue;
            }

            Evolucao.Aplicar(livre, Evolucao.SortearVariacao(livre, aleatorio), aleatorio);
            livre.Envelhecer();
            livre.ComecarNovaTemporada();
        }

        Contratos.ResolverVencimentos(mundo, temporada.Ano, clubeDoJogador);
        Contratos.LimitarAgentesLivres(mundo);

        foreach (var clube in mundo.Clubes)
        {
            revelacoes.AddRange(RevelarJovens(clube, aleatorio, temporada.Ano + 1, ref proximoId));
        }

        mundo.AvancarTemporada();
        return new ResultadoTemporada(temporada.Ano, resumos, aposentados, revelacoes, extratos,
            temporada.Copa?.Campeao, temporada.Copa?.Vice);
    }

    private static void PagarPremiacao(Temporada temporada, Mundo mundo)
    {
        foreach (var divisao in mundo.Divisoes)
        {
            var tabela = temporada.Tabela(divisao.Id);
            foreach (var linha in tabela)
            {
                mundo.Clube(linha.ClubeId).Lancar(CategoriaFinanceira.Premiacao,
                    Financas.Financas.Premio(linha.Posicao, tabela.Count, divisao, temporada.RegrasFinanceiras));
            }
        }
    }

    private static List<ResumoDivisao> Movimentar(Temporada temporada, Mundo mundo)
    {
        var divisoes = mundo.Divisoes;
        var tabelas = divisoes.Select(d => temporada.Tabela(d.Id)).ToArray();
        var promovidos = new List<string>[divisoes.Count];
        var rebaixados = new List<string>[divisoes.Count];

        for (var i = 0; i < divisoes.Count; i++)
        {
            promovidos[i] = [.. tabelas[i].Take(divisoes[i].VagasDiretas).Select(l => l.ClubeId)];
            if (divisoes[i].TemPlayoff)
            {
                var playoff = temporada.Playoff(divisoes[i].Id);
                // Sem playoff jogado (não deveria acontecer), as vagas vão para os melhores colocados da faixa.
                promovidos[i].AddRange(playoff.Count > 0
                    ? playoff.Select(c => c.VencedorId ?? c.MelhorId)
                    : divisoes[i].ConfrontosDoPlayoff.Select(c => tabelas[i][c.MelhorPosicao - 1].ClubeId));
            }
            rebaixados[i] = [.. tabelas[i].TakeLast(divisoes[i].VagasRebaixamento).Select(l => l.ClubeId)];
        }

        var novas = new List<Divisao>();
        for (var i = 0; i < divisoes.Count; i++)
        {
            var saem = promovidos[i].Concat(rebaixados[i]).ToHashSet(StringComparer.Ordinal);
            var ficam = tabelas[i].Select(l => l.ClubeId).Where(id => !saem.Contains(id));
            var descem = i > 0 ? rebaixados[i - 1] : [];
            var sobem = i < divisoes.Count - 1 ? promovidos[i + 1] : [];
            var d = divisoes[i];
            novas.Add(new Divisao(d.Id, d.Nome, d.Nivel, [.. ficam, .. sobem, .. descem], d.VagasAcesso, d.VagasRebaixamento, d.VagasDiretas));
        }

        var resumos = divisoes.Select((d, i) =>
        {
            var artilharia = temporada.Artilharia(d.Id, 1);
            return new ResumoDivisao(d.Id, d.Nome, tabelas[i][0].ClubeId, promovidos[i], rebaixados[i],
                artilharia.Count > 0 ? artilharia[0] : null, tabelas[i]);
        }).ToList();

        mundo.ReorganizarDivisoes(novas);
        return resumos;
    }

    /// <summary>A partir dos 34 a chance cresce a cada ano; aos 39 todos param.</summary>
    private static bool Aposenta(Jogador jogador, IGeradorAleatorio aleatorio) =>
        jogador.Idade >= IdadeLimite || (jogador.Idade >= 34 && aleatorio.Chance((jogador.Idade - 33) * 0.18));

    /// <summary>
    /// Revelações da base: 1 ou 2 jovens por ano (se houver espaço) e quantos forem precisos para o elenco
    /// voltar a ter 25. A posição é a que mais falta em relação ao elenco-modelo.
    /// </summary>
    private static List<int> RevelarJovens(Clube clube, IGeradorAleatorio aleatorio, int temporada, ref int proximoId)
    {
        var novos = new List<int>();
        var anuais = aleatorio.Proximo(1, 3);
        var quantidade = Math.Max(ElencoMinimo - clube.Elenco.Count, Math.Min(anuais, ElencoMaximo - clube.Elenco.Count));
        if (quantidade <= 0)
        {
            return novos;
        }

        var nivel = clube.Elenco.OrderByDescending(j => j.ForcaGeral).Take(11).Select(j => j.ForcaGeral).DefaultIfEmpty(50).Average();
        var nomes = new GeradorDeNomes(aleatorio);
        var usados = clube.Elenco.Select(j => j.Nome).ToHashSet(StringComparer.Ordinal);
        var gerador = new GeradorDeJogadores(aleatorio);

        for (var i = 0; i < quantidade; i++)
        {
            var posicao = PosicaoQueMaisFalta(clube, aleatorio);
            var jovem = gerador.Gerar(proximoId++, nomes.Gerar(usados), posicao, (int)Math.Round(nivel) - 8, clube.Id, temporada,
                idade: aleatorio.Proximo(17, 20));
            clube.AdicionarAoElenco(jovem);
            novos.Add(jovem.Id);
        }

        return novos;
    }

    private static Posicao PosicaoQueMaisFalta(Clube clube, IGeradorAleatorio aleatorio)
    {
        var deficits = GeradorDeMundo.ElencoBase
            .GroupBy(p => p)
            .Select(g => (Posicao: g.Key, Falta: g.Count() - clube.Elenco.Count(j => j.PosicaoPrincipal == g.Key)))
            .Where(x => x.Falta > 0)
            .OrderByDescending(x => x.Falta)
            .ThenBy(x => x.Posicao)
            .ToArray();

        return deficits.Length > 0
            ? deficits[0].Posicao
            : GeradorDeMundo.ElencoBase[aleatorio.Proximo(0, GeradorDeMundo.ElencoBase.Length)];
    }
}
