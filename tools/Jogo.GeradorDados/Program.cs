// Gera o mundo inicial do jogo base (clubes e jogadores reais, D-033) e grava como definições JSON.
//
// Uso:
//   dotnet run --project tools/Jogo.GeradorDados -- [--seed 2027] [--temporada 2026] [--saida dados/definicoes]
//   dotnet run --project tools/Jogo.GeradorDados -- --api [--renovar]
//
// --api: SÓ LOCAL (D-038). Busca os elencos atuais na API-Football (chave na variável de ambiente
// API_FOOTBALL_KEY), cruza com os dados pesquisados e grava em dados/local/definicoes (ignorada pelo Git).
// O cache das respostas fica em dados/local/api; --renovar busca os elencos de novo.
//
// Entradas: tools/Jogo.GeradorDados/reais/clubes.txt e elencos-*.txt (dados públicos pesquisados).
// Os atributos são estimados pelo gerador; mesma seed = mesmos arquivos, byte a byte.

using System.Globalization;
using Jogo.Dados.Definicoes;
using Jogo.Dados.Reais;
using Jogo.Simulacao.Geracao;

var seed = 2027UL;
var temporada = 2026;
var saida = Path.Combine("dados", "definicoes");
var usarApi = false;
var renovar = false;

for (var i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--seed" when i + 1 < args.Length:
            seed = ulong.Parse(args[++i], CultureInfo.InvariantCulture);
            break;
        case "--temporada" when i + 1 < args.Length:
            temporada = int.Parse(args[++i], CultureInfo.InvariantCulture);
            break;
        case "--saida" when i + 1 < args.Length:
            saida = args[++i];
            break;
        case "--api":
            usarApi = true;
            break;
        case "--renovar":
            renovar = true;
            break;
        default:
            Console.Error.WriteLine($"Argumento desconhecido: {args[i]}");
            Console.Error.WriteLine("Uso: Jogo.GeradorDados [--seed N] [--temporada AAAA] [--saida pasta] [--api [--renovar]]");
            return 1;
    }
}

var pastaReais = Path.Combine(PastaDoProjeto(), "tools", "Jogo.GeradorDados", "reais");
var clubes = LeitorDadosReais.Carregar(pastaReais);
var manifesto = new Manifesto(
    "Foot Manager BR · Brasileirão 2026",
    "Equipe Foot Manager BR",
    "Clubes e elencos reais das Séries A e B de 2026 (fontes públicas, novembro de 2026). Atributos estimados pelo jogo.");

if (usarApi)
{
    var chave = Environment.GetEnvironmentVariable("API_FOOTBALL_KEY");
    if (string.IsNullOrWhiteSpace(chave))
    {
        Console.Error.WriteLine("Defina a variável de ambiente API_FOOTBALL_KEY com a sua chave da API-Football.");
        return 1;
    }

    var pastaLocal = Path.Combine(PastaDoProjeto(), "dados", "local");
    var pastaCache = Path.Combine(pastaLocal, "api");
    Directory.CreateDirectory(pastaCache);
    if (!args.Contains("--saida"))
    {
        saida = Path.Combine(pastaLocal, "definicoes");
    }

    Console.WriteLine("Buscando elencos na API-Football (uso local)...");
    using var importador = new ImportadorApi(chave, pastaCache, renovar);
    var daApi = await importador.ElencosAsync(clubes);

    var relatorio = new List<string>();
    clubes = [.. clubes.Select(c =>
    {
        var r = CruzamentoComApi.Cruzar(c.Elenco, daApi[c.Id]);
        relatorio.Add(daApi[c.Id].Count == 0
            ? $"{c.Nome}: sem elenco da API (mantido o pesquisado, {c.Elenco.Count})"
            : $"{c.Nome}: {r.Elenco.Count} no elenco · {r.Mantidos.Count} já conhecidos · novos: {string.Join(", ", r.Novos)} · saíram: {string.Join(", ", r.Sairam)}");
        return c with { Elenco = r.Elenco };
    })];

    File.WriteAllLines(Path.Combine(pastaCache, "relatorio.txt"), relatorio);
    Console.WriteLine($"Relatório do cruzamento em {Path.Combine(pastaCache, "relatorio.txt")}");
    manifesto = manifesto with
    {
        Nome = "Foot Manager BR · Brasileirão 2026 (local)",
        Descricao = "Elencos atuais da API-Football cruzados com os dados pesquisados. Só para uso local; não publicar.",
    };
}

var mundo = GeradorDeMundoReal.Gerar(seed, temporada, clubes);

EscritorDefinicoes.Salvar(mundo, manifesto, saida);

Console.WriteLine($"Mundo gerado (seed {seed}, temporada {temporada}) em {Path.GetFullPath(saida)}");
foreach (var divisao in mundo.Divisoes)
{
    Console.WriteLine();
    Console.WriteLine(divisao.Nome);
    Console.WriteLine($"  {"Clube",-24} {"Reais",5} {"Elenco",6} {"Top 11",7} {"Caixa (R$ mi)",14}  Melhor");
    foreach (var clube in divisao.ClubeIds.Select(mundo.Clube).OrderByDescending(Top11))
    {
        var reais = clubes.First(c => c.Id == clube.Id).Elenco.Select(j => j.Nome).ToHashSet(StringComparer.Ordinal);
        var melhor = clube.Elenco.MaxBy(j => j.ForcaGeral)!;
        Console.WriteLine(string.Create(CultureInfo.GetCultureInfo("pt-BR"),
            $"  {clube.Nome,-24} {clube.Elenco.Count(j => reais.Contains(j.Nome)),5} {clube.Elenco.Count,6} {Top11(clube),7:F1} {clube.Caixa / 1_000_000m,14:F1}  {melhor.Nome} ({melhor.ForcaGeral})"));
    }
}

return 0;

static double Top11(Jogo.Dominio.Clubes.Clube clube) =>
    clube.Elenco.OrderByDescending(j => j.ForcaGeral).Take(11).Average(j => j.ForcaGeral);

static string PastaDoProjeto()
{
    var pasta = new DirectoryInfo(AppContext.BaseDirectory);
    while (pasta is not null && !File.Exists(Path.Combine(pasta.FullName, "FootManagerBR.slnx")))
    {
        pasta = pasta.Parent;
    }

    return pasta?.FullName ?? Directory.GetCurrentDirectory();
}
