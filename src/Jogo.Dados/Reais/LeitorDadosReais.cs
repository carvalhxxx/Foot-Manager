using System.Globalization;
using Jogo.Dominio.Jogadores;
using Jogo.Dominio.Reais;

namespace Jogo.Dados.Reais;

/// <summary>
/// Lê os dados reais pesquisados (clubes.txt e elencos-*.txt, texto separado por "|")
/// usados pelo gerador do mundo base (D-033).
/// </summary>
public static class LeitorDadosReais
{
    public static List<ClubeReal> Carregar(string pasta)
    {
        var elencos = Directory.GetFiles(pasta, "elencos-*.txt").Order(StringComparer.Ordinal)
            .SelectMany(File.ReadAllLines)
            .Select(l => l.Trim())
            .Where(l => l.Length > 0 && !l.StartsWith('#'))
            .Select(l => l.Split('|'))
            .GroupBy(c => c[0], StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Select(LerJogador).ToList(), StringComparer.Ordinal);

        var clubes = File.ReadAllLines(Path.Combine(pasta, "clubes.txt"))
            .Select(l => l.Trim())
            .Where(l => l.Length > 0 && !l.StartsWith('#'))
            .Select(l => l.Split('|'))
            .Select(c => new ClubeReal(c[0], c[1], c[2], c[3], c[4], c[5], c[6], c[7],
                int.Parse(c[8], CultureInfo.InvariantCulture), int.Parse(c[9], CultureInfo.InvariantCulture),
                int.Parse(c[10], CultureInfo.InvariantCulture), int.Parse(c[11], CultureInfo.InvariantCulture),
                elencos.GetValueOrDefault(c[0]) ?? []))
            .ToList();

        var semClube = elencos.Keys.Where(k => clubes.All(c => c.Id != k)).ToArray();
        if (semClube.Length > 0)
        {
            throw new InvalidOperationException("Elencos de clubes que não estão em clubes.txt: " + string.Join(", ", semClube));
        }

        return clubes;
    }

    private static JogadorReal LerJogador(string[] c)
    {
        var (posicao, grupo) = c[2] switch
        {
            "GOL" => ((Posicao?)Posicao.Goleiro, GrupoDePosicao.Goleiro),
            "DEF" => (null, GrupoDePosicao.Defensor),
            "MEI" => (null, GrupoDePosicao.Meia),
            "ATA" => (Posicao.Centroavante, GrupoDePosicao.Atacante),
            var sigla when PosicaoExtensoes.TentarDaSigla(sigla, out var p) => (p, GrupoDoSetor(p)),
            var sigla => throw new FormatException($"Posição \"{sigla}\" desconhecida ({string.Join('|', c)})."),
        };
        return new JogadorReal(c[1], posicao, grupo,
            c.Length > 3 && c[3].Length > 0 ? int.Parse(c[3], CultureInfo.InvariantCulture) : null,
            c.Length > 4 && c[4].Length > 0 ? decimal.Parse(c[4], CultureInfo.InvariantCulture) : null);
    }

    private static GrupoDePosicao GrupoDoSetor(Posicao p) => p.Setor() switch
    {
        Setor.Gol => GrupoDePosicao.Goleiro,
        Setor.Defesa => GrupoDePosicao.Defensor,
        Setor.Meio => GrupoDePosicao.Meia,
        _ => GrupoDePosicao.Atacante,
    };
}
