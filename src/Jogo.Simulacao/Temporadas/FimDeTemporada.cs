using Jogo.Dominio;
using Jogo.Dominio.Aleatoriedade;
using Jogo.Dominio.Clubes;
using Jogo.Dominio.Competicoes;
using Jogo.Dominio.Jogadores;
using Jogo.Simulacao.Geracao;

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
    IReadOnlyList<int> Revelacoes);

/// <summary>
/// Fecha a temporada: acesso e rebaixamento, evolução, aposentadorias, revelações da base,
/// envelhecimento e preparação do elenco para o ano seguinte.
/// </summary>
public static class FimDeTemporada
{
    public const int ElencoMinimo = 25;
    public const int ElencoMaximo = 30;
    public const int IdadeLimite = 39;

    public static ResultadoTemporada Encerrar(Temporada temporada, Mundo mundo, ulong seed)
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
                RenovarSeVencer(jogador, temporada.Ano);
                jogador.ComecarNovaTemporada();
            }

            revelacoes.AddRange(RevelarJovens(clube, aleatorio, temporada.Ano + 1, ref proximoId));
        }

        mundo.AvancarTemporada();
        return new ResultadoTemporada(temporada.Ano, resumos, aposentados, revelacoes);
    }

    private static List<ResumoDivisao> Movimentar(Temporada temporada, Mundo mundo)
    {
        var divisoes = mundo.Divisoes;
        var tabelas = divisoes.Select(d => temporada.Tabela(d.Id)).ToArray();
        var promovidos = new List<string>[divisoes.Count];
        var rebaixados = new List<string>[divisoes.Count];

        for (var i = 0; i < divisoes.Count; i++)
        {
            promovidos[i] = [.. tabelas[i].Take(divisoes[i].VagasAcesso).Select(l => l.ClubeId)];
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
            novas.Add(new Divisao(d.Id, d.Nome, d.Nivel, [.. ficam, .. sobem, .. descem], d.VagasAcesso, d.VagasRebaixamento));
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

    /// <summary>Até o mercado existir (E8), contratos vencidos são renovados por 2 anos com o mesmo salário.</summary>
    private static void RenovarSeVencer(Jogador jogador, int ano)
    {
        if (jogador.Contrato is { } contrato && contrato.VenceAoFimDe(ano))
        {
            jogador.AssinarContrato(new Contrato(contrato.ClubeId, contrato.SalarioMensal, ano + 2));
        }
    }

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
