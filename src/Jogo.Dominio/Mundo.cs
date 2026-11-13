using Jogo.Dominio.Clubes;
using Jogo.Dominio.Competicoes;

namespace Jogo.Dominio;

/// <summary>
/// O estado completo do mundo do jogo: temporada atual, divisões e clubes (com seus elencos).
/// Garante a consistência entre as partes (ex.: todo clube de uma divisão existe e está em uma só divisão).
/// </summary>
public sealed class Mundo
{
    private readonly Dictionary<string, Clube> _clubesPorId;

    public Mundo(int temporadaAtual, IEnumerable<Divisao> divisoes, IEnumerable<Clube> clubes)
    {
        ArgumentNullException.ThrowIfNull(divisoes);
        ArgumentNullException.ThrowIfNull(clubes);

        var listaDivisoes = divisoes.OrderBy(d => d.Nivel).ToArray();
        var listaClubes = clubes.ToArray();

        var inconsistencias = Inconsistencias(temporadaAtual, listaDivisoes, listaClubes);
        if (inconsistencias.Count > 0)
        {
            throw new ArgumentException("Mundo inconsistente:\n- " + string.Join("\n- ", inconsistencias));
        }

        TemporadaAtual = temporadaAtual;
        Divisoes = listaDivisoes;
        Clubes = listaClubes;
        _clubesPorId = listaClubes.ToDictionary(c => c.Id, StringComparer.Ordinal);
    }

    public int TemporadaAtual { get; private set; }

    /// <summary>Divisões ordenadas por nível (Série A primeiro).</summary>
    public IReadOnlyList<Divisao> Divisoes { get; }

    /// <summary>Clubes na ordem em que foram definidos.</summary>
    public IReadOnlyList<Clube> Clubes { get; }

    public Clube Clube(string id) =>
        _clubesPorId.TryGetValue(id, out var clube)
            ? clube
            : throw new KeyNotFoundException($"Clube \"{id}\" não existe.");

    /// <summary>
    /// Lista todos os problemas de consistência de uma vez (em vez de parar no primeiro),
    /// para que quem edita um patch veja tudo o que precisa corrigir.
    /// </summary>
    public static IReadOnlyList<string> Inconsistencias(
        int temporadaAtual,
        IReadOnlyList<Divisao> divisoes,
        IReadOnlyList<Clube> clubes)
    {
        ArgumentNullException.ThrowIfNull(divisoes);
        ArgumentNullException.ThrowIfNull(clubes);

        var problemas = new List<string>();

        if (temporadaAtual is < 1900 or > 9999)
        {
            problemas.Add($"Temporada {temporadaAtual} inválida.");
        }

        if (divisoes.Count == 0)
        {
            problemas.Add("É preciso ao menos uma divisão.");
        }

        problemas.AddRange(Repetidos(divisoes.Select(d => d.Id)).Select(id => $"Divisão \"{id}\" definida mais de uma vez."));
        problemas.AddRange(Repetidos(clubes.Select(c => c.Id)).Select(id => $"Clube \"{id}\" definido mais de uma vez."));

        var niveis = divisoes.Select(d => d.Nivel).OrderBy(n => n).ToArray();
        if (!niveis.SequenceEqual(Enumerable.Range(1, niveis.Length)))
        {
            problemas.Add($"Os níveis das divisões devem ser 1, 2, 3... sem buracos nem repetição (encontrados: {string.Join(", ", niveis)}).");
        }

        var idsClubes = clubes.Select(c => c.Id).ToHashSet(StringComparer.Ordinal);
        var divisaoDoClube = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var divisao in divisoes)
        {
            foreach (var clubeId in divisao.ClubeIds)
            {
                if (!idsClubes.Contains(clubeId))
                {
                    problemas.Add($"A divisão \"{divisao.Id}\" cita o clube \"{clubeId}\", que não existe.");
                }

                if (!divisaoDoClube.TryAdd(clubeId, divisao.Id))
                {
                    problemas.Add($"O clube \"{clubeId}\" está em mais de uma divisão (\"{divisaoDoClube[clubeId]}\" e \"{divisao.Id}\").");
                }
            }
        }

        problemas.AddRange(clubes
            .Where(c => !divisaoDoClube.ContainsKey(c.Id))
            .Select(c => $"O clube \"{c.Id}\" não está em nenhuma divisão."));

        var ordenadas = divisoes.OrderBy(d => d.Nivel).ToArray();
        foreach (var (divisao, abaixo) in ordenadas.Zip(ordenadas.Skip(1)))
        {
            if (divisao.VagasRebaixamento != abaixo.VagasAcesso)
            {
                problemas.Add($"\"{divisao.Id}\" rebaixa {divisao.VagasRebaixamento}, mas \"{abaixo.Id}\" promove {abaixo.VagasAcesso}: os números precisam ser iguais.");
            }
        }

        if (ordenadas.Length > 0 && ordenadas[^1].VagasRebaixamento != 0)
        {
            problemas.Add($"\"{ordenadas[^1].Id}\" é a última divisão e não pode rebaixar ninguém.");
        }

        var jogadores = clubes.SelectMany(c => c.Elenco.Select(j => (Clube: c, Jogador: j))).ToArray();
        problemas.AddRange(Repetidos(jogadores.Select(x => x.Jogador.Id.ToString(System.Globalization.CultureInfo.InvariantCulture)))
            .Select(id => $"Jogador com id {id} aparece mais de uma vez."));

        problemas.AddRange(jogadores
            .Where(x => x.Jogador.Contrato is not null && x.Jogador.Contrato.ClubeId != x.Clube.Id)
            .Select(x => $"O jogador {x.Jogador.Id} ({x.Jogador.Nome}) está no elenco de \"{x.Clube.Id}\" mas tem contrato com \"{x.Jogador.Contrato!.ClubeId}\"."));

        return problemas;
    }

    /// <summary>Avança para a próxima temporada (chamado no fim de temporada).</summary>
    public void AvancarTemporada() => TemporadaAtual++;

    private static IEnumerable<string> Repetidos(IEnumerable<string> valores) =>
        valores.GroupBy(v => v, StringComparer.Ordinal).Where(g => g.Count() > 1).Select(g => g.Key).Order(StringComparer.Ordinal);
}
