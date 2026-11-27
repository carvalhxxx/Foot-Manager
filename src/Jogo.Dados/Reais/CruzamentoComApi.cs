using System.Globalization;
using System.Text;
using Jogo.Dominio.Reais;

namespace Jogo.Dados.Reais;

/// <summary>Um jogador como vem do elenco da API-Football (posição só por grupo).</summary>
public sealed record JogadorApi(int Id, string Nome, int? Idade, GrupoDePosicao Grupo);

/// <summary>Resultado do cruzamento de um clube: o elenco final e o que mudou, para conferência.</summary>
public sealed record CruzamentoDoClube(
    IReadOnlyList<JogadorReal> Elenco,
    IReadOnlyList<string> Mantidos,
    IReadOnlyList<string> Novos,
    IReadOnlyList<string> Sairam);

/// <summary>
/// Junta o elenco atual da API (quem está no clube e a idade) com os nossos dados pesquisados
/// (valor de mercado e posição exata), D-038. Regras:
/// <list type="bullet">
/// <item>A API manda em quem está no elenco: quem só existe nos nossos dados saiu do clube.</item>
/// <item>Jogador encontrado nos dois lados fica com o nome mais completo, a idade da API e o valor e a
/// posição nossos (a posição só se for do mesmo grupo da API).</item>
/// <item>Nomes da API costumam vir abreviados ("A. Rossi"): casa por inicial + sobrenome.</item>
/// <item>Se a API não trouxe ninguém (falha), o elenco pesquisado fica como está.</item>
/// </list>
/// </summary>
public static class CruzamentoComApi
{
    public const int IdadeMinima = 17;

    public static CruzamentoDoClube Cruzar(IReadOnlyList<JogadorReal> nossos, IReadOnlyList<JogadorApi> daApi)
    {
        ArgumentNullException.ThrowIfNull(nossos);
        ArgumentNullException.ThrowIfNull(daApi);
        if (daApi.Count == 0)
        {
            return new CruzamentoDoClube(nossos, [.. nossos.Select(j => j.Nome)], [], []);
        }

        var livres = nossos.ToList();
        var daApiValidos = daApi.Where(j => j.Idade is null || j.Idade >= IdadeMinima).ToList();

        // 1ª passada: nomes iguais. 2ª: parecidos (só com candidato único). Assim um apelido
        // ("Higor") não "rouba" quem tem o nome exato mais adiante ("Higor Meritão").
        var pares = new Dictionary<JogadorApi, JogadorReal>();
        foreach (var api in daApiValidos)
        {
            if (livres.FirstOrDefault(n => Igual(n.Nome, api.Nome)) is { } igual)
            {
                pares[api] = igual;
                livres.Remove(igual);
            }
        }

        foreach (var api in daApiValidos.Where(a => !pares.ContainsKey(a)))
        {
            var parecidos = livres.Where(n => Parecido(n.Nome, api.Nome)).Take(2).ToArray();
            if (parecidos.Length == 1)
            {
                pares[api] = parecidos[0];
                livres.Remove(parecidos[0]);
            }
        }

        var elenco = new List<JogadorReal>();
        var mantidos = new List<string>();
        var novos = new List<string>();
        foreach (var api in daApiValidos)
        {
            if (!pares.TryGetValue(api, out var nosso))
            {
                elenco.Add(new JogadorReal(api.Nome, api.Grupo == GrupoDePosicao.Goleiro ? Dominio.Jogadores.Posicao.Goleiro : null,
                    api.Grupo, api.Idade, null));
                novos.Add(api.Nome);
                continue;
            }

            var mesmoGrupo = nosso.Grupo == api.Grupo;
            var nome = Normalizar(nosso.Nome).Length >= Normalizar(api.Nome).Length ? nosso.Nome : api.Nome;
            elenco.Add(new JogadorReal(
                nome,
                mesmoGrupo ? nosso.Posicao : (api.Grupo == GrupoDePosicao.Goleiro ? Dominio.Jogadores.Posicao.Goleiro : null),
                api.Grupo,
                api.Idade ?? nosso.Idade,
                nosso.ValorEmEuros));
            mantidos.Add(nome);
        }

        return new CruzamentoDoClube(elenco, mantidos, novos, [.. livres.Select(j => j.Nome)]);
    }

    /// <summary>Mesmo nome ignorando acentos, caixa e pontuação.</summary>
    public static bool Igual(string a, string b) => Normalizar(a) == Normalizar(b);

    /// <summary>
    /// "A. Rossi" ~ "Agustín Rossi"; "Arrascaeta" ~ "Giorgian De Arrascaeta"; "Andrew" ~ "Andrew Ventura".
    /// Com dois nomes ou mais: último nome igual e mesma inicial. Com um nome só: é o primeiro ou o último do outro.
    /// </summary>
    public static bool Parecido(string nosso, string api)
    {
        var n = Partes(nosso);
        var a = Partes(api);
        if (n.Length == 0 || a.Length == 0)
        {
            return false;
        }

        if (n.Length == 1 || a.Length == 1)
        {
            // Nome único (apelido): é o primeiro ou o último nome do outro ("Andrew" ~ "Andrew Ventura").
            var unico = n.Length == 1 ? n[0] : a[0];
            var outro = n.Length == 1 ? a : n;
            return unico.Length >= 4 && (outro[0] == unico || outro[^1] == unico);
        }

        return n[^1] == a[^1] && n[0][0] == a[0][0];
    }

    private static string[] Partes(string nome) =>
        Normalizar(nome).Split(' ', StringSplitOptions.RemoveEmptyEntries).Where(p => p is not ("de" or "da" or "do" or "dos" or "das")).ToArray();

    private static string Normalizar(string texto)
    {
        var decomposto = texto.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposto.Length);
        foreach (var c in decomposto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            sb.Append(char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : ' ');
        }

        return string.Join(' ', sb.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}
