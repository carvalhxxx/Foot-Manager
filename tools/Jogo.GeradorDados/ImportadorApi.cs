// Importa elencos atuais da API-Football para uso LOCAL (D-038): nada disto vai para o repositório.
// Cache em dados/local/api (cada resposta é guardada; só chama a API de novo com --renovar).

using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Jogo.Dados.Reais;
using Jogo.Dominio.Reais;

internal sealed class ImportadorApi(string chave, string pastaCache, bool renovar) : IDisposable
{
    private const string Base = "https://v3.football.api-sports.io/";

    /// <summary>Pausa entre chamadas reais (o plano grátis limita por minuto e bloqueia quem abusa).</summary>
    private static readonly TimeSpan Pausa = TimeSpan.FromSeconds(7);

    // Nome como a API costuma escrever, quando diferente do nosso (a busca é por país Brasil).
    private static readonly Dictionary<string, string[]> Apelidos = new(StringComparer.Ordinal)
    {
        ["athletico-pr"] = ["Athletico Paranaense", "Atletico Paranaense", "Athletico-PR"],
        ["atletico-mg"] = ["Atletico-MG", "Atletico Mineiro"],
        ["bragantino"] = ["RB Bragantino", "Red Bull Bragantino", "Bragantino"],
        ["sao-paulo"] = ["Sao Paulo"],
        ["vasco"] = ["Vasco DA Gama", "Vasco da Gama"],
        ["fortaleza"] = ["Fortaleza EC", "Fortaleza"],
        ["atletico-go"] = ["Atletico Goianiense", "Atletico-GO"],
        ["sport"] = ["Sport Recife", "Sport"],
        ["operario-pr"] = ["Operario-PR", "Operario Ferroviario", "Operario"],
        ["sao-bernardo"] = ["Sao Bernardo", "São Bernardo FC"],
        ["botafogo-sp"] = ["Botafogo SP", "Botafogo-SP"],
        ["athletic"] = ["Athletic Club", "Athletic"],
        ["america-mg"] = ["America Mineiro", "America-MG"],
        ["novorizontino"] = ["Novorizontino", "Gremio Novorizontino"],
    };

    private readonly HttpClient _http = CriarCliente(chave);
    private int _chamadasReais;
    private int? _restantesHoje;

    public void Dispose() => _http.Dispose();

    /// <summary>Elenco da API de cada clube (vazio quando não deu para buscar), com um relatório no console.</summary>
    public async Task<Dictionary<string, List<JogadorApi>>> ElencosAsync(IReadOnlyList<ClubeReal> clubes)
    {
        var ids = await IdsDosTimesAsync(clubes);
        var elencos = new Dictionary<string, List<JogadorApi>>(StringComparer.Ordinal);
        foreach (var clube in clubes)
        {
            if (!ids.TryGetValue(clube.Id, out var idApi))
            {
                elencos[clube.Id] = [];
                continue;
            }

            var json = await ObterAsync($"players/squads?team={idApi}", $"elenco-{clube.Id}.json", podeRenovar: true);
            elencos[clube.Id] = json is null ? [] : LerElenco(json);
        }

        return elencos;
    }

    private async Task<Dictionary<string, int>> IdsDosTimesAsync(IReadOnlyList<ClubeReal> clubes)
    {
        // Ajustes manuais (opcional): dados/local/api/times-manuais.txt com "clube|idApi" por linha.
        var manuais = new Dictionary<string, int>(StringComparer.Ordinal);
        var arquivoManual = Path.Combine(pastaCache, "times-manuais.txt");
        if (File.Exists(arquivoManual))
        {
            foreach (var partes in File.ReadAllLines(arquivoManual).Select(l => l.Split('|')).Where(p => p.Length == 2))
            {
                manuais[partes[0].Trim()] = int.Parse(partes[1].Trim(), CultureInfo.InvariantCulture);
            }
        }

        // Uma chamada só traz todos os clubes do Brasil (a API não aceita "search" junto com "country").
        var ids = new Dictionary<string, int>(StringComparer.Ordinal);
        var json = await ObterAsync("teams?country=Brazil", "times-brasil.json", podeRenovar: false);
        var candidatos = json is null
            ? []
            : json.RootElement.GetProperty("response").EnumerateArray()
                .Select(t => (Id: t.GetProperty("team").GetProperty("id").GetInt32(),
                    Nome: t.GetProperty("team").GetProperty("name").GetString() ?? "",
                    Nacional: t.GetProperty("team").TryGetProperty("national", out var n) && n.ValueKind == JsonValueKind.True))
                .Where(t => !t.Nacional)
                .ToList();

        foreach (var clube in clubes)
        {
            if (manuais.TryGetValue(clube.Id, out var manual))
            {
                ids[clube.Id] = manual;
                continue;
            }

            var nomes = (Apelidos.GetValueOrDefault(clube.Id) ?? []).Append(clube.Nome).ToArray();
            var achados = candidatos.Where(c => nomes.Any(n => Igual(n, c.Nome))).ToList();
            if (achados.Count == 1)
            {
                ids[clube.Id] = achados[0].Id;
            }
            else
            {
                var parecidos = candidatos.Where(c => nomes.Any(n => SemAcento(c.Nome).Contains(SemAcento(n), StringComparison.OrdinalIgnoreCase))).Take(6);
                Console.WriteLine($"  ? {clube.Nome}: {(achados.Count == 0 ? "nenhum nome exato" : "mais de um nome igual")}. Parecidos: "
                    + string.Join(", ", parecidos.Select(c => $"{c.Nome}={c.Id}")) + ". Use dados/local/api/times-manuais.txt.");
            }
        }

        File.WriteAllLines(Path.Combine(pastaCache, "times-encontrados.txt"),
            ids.OrderBy(i => i.Key, StringComparer.Ordinal).Select(i => $"{i.Key}|{i.Value}"));
        return ids;
    }

    private static List<JogadorApi> LerElenco(JsonDocument json)
    {
        var resposta = json.RootElement.GetProperty("response");
        if (resposta.GetArrayLength() == 0)
        {
            return [];
        }

        return [.. resposta[0].GetProperty("players").EnumerateArray().Select(p => new JogadorApi(
            p.GetProperty("id").GetInt32(),
            p.GetProperty("name").GetString() ?? "?",
            p.TryGetProperty("age", out var idade) && idade.ValueKind == JsonValueKind.Number ? idade.GetInt32() : null,
            (p.GetProperty("position").GetString() ?? "") switch
            {
                "Goalkeeper" => GrupoDePosicao.Goleiro,
                "Defender" => GrupoDePosicao.Defensor,
                "Midfielder" => GrupoDePosicao.Meia,
                _ => GrupoDePosicao.Atacante,
            }))];
    }

    /// <summary>Resposta do cache ou da API (guardando no cache). Nulo se a API recusar ou a cota acabar.</summary>
    private async Task<JsonDocument?> ObterAsync(string caminho, string arquivoCache, bool podeRenovar)
    {
        var cache = Path.Combine(pastaCache, arquivoCache);
        if (File.Exists(cache) && !(renovar && podeRenovar))
        {
            return JsonDocument.Parse(await File.ReadAllTextAsync(cache));
        }

        if (_restantesHoje is <= 1)
        {
            Console.WriteLine($"  ! Cota do dia acabando: {caminho} fica para amanhã (rode de novo; o que já veio está no cache).");
            return null;
        }

        if (_chamadasReais > 0)
        {
            await Task.Delay(Pausa);
        }

        _chamadasReais++;
        using var resposta = await _http.GetAsync(caminho);
        if (resposta.Headers.TryGetValues("x-ratelimit-requests-remaining", out var restantes)
            && int.TryParse(restantes.FirstOrDefault(), out var r))
        {
            _restantesHoje = r;
        }

        var texto = await resposta.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(texto);
        var erros = json.RootElement.GetProperty("errors");
        if (!resposta.IsSuccessStatusCode || (erros.ValueKind == JsonValueKind.Object && erros.EnumerateObject().Any())
            || (erros.ValueKind == JsonValueKind.Array && erros.GetArrayLength() > 0))
        {
            Console.WriteLine($"  ! {caminho}: {(int)resposta.StatusCode} {erros.GetRawText()}");
            return null;
        }

        await File.WriteAllTextAsync(cache, texto);
        Console.WriteLine($"  · {caminho} (restam {_restantesHoje?.ToString(CultureInfo.InvariantCulture) ?? "?"} hoje)");
        return json;
    }

    private static HttpClient CriarCliente(string chave)
    {
        var http = new HttpClient { BaseAddress = new Uri(Base), Timeout = TimeSpan.FromSeconds(30) };
        http.DefaultRequestHeaders.Add("x-apisports-key", chave);
        http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return http;
    }

    private static bool Igual(string a, string b) => SemAcento(a).Equals(SemAcento(b), StringComparison.OrdinalIgnoreCase);

    private static string SemAcento(string texto) =>
        new([.. texto.Normalize(NormalizationForm.FormD).Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)]);
}
