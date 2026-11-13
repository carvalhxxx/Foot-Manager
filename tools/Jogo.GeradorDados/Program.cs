// Gera o mundo inicial do jogo base e grava como definições JSON.
//
// Uso:
//   dotnet run --project tools/Jogo.GeradorDados -- [--seed 2027] [--temporada 2027] [--saida dados/definicoes]
//
// Mesma seed = mesmos arquivos, byte a byte.

using System.Globalization;
using Jogo.Dados.Definicoes;
using Jogo.Dominio.Jogadores;
using Jogo.Simulacao.Geracao;

var seed = 2027UL;
var temporada = 2027;
var saida = Path.Combine("dados", "definicoes");

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
        default:
            Console.Error.WriteLine($"Argumento desconhecido: {args[i]}");
            Console.Error.WriteLine("Uso: Jogo.GeradorDados [--seed N] [--temporada AAAA] [--saida pasta]");
            return 1;
    }
}

var mundo = GeradorDeMundo.Gerar(seed, temporada, CatalogoBase.Clubes);
var manifesto = new Manifesto(
    "Foot Manager BR · Base",
    "Equipe Foot Manager BR",
    $"Clubes e jogadores fictícios gerados com a seed {seed}.");

EscritorDefinicoes.Salvar(mundo, manifesto, saida);

// Resumo para conferência rápida.
Console.WriteLine($"Mundo gerado (seed {seed}, temporada {temporada}) em {Path.GetFullPath(saida)}");
foreach (var divisao in mundo.Divisoes)
{
    Console.WriteLine();
    Console.WriteLine($"{divisao.Nome}");
    Console.WriteLine($"  {"Clube",-30} {"Elenco",6} {"Top 11",7} {"Média",6} {"Caixa (R$ mi)",14}");
    foreach (var clube in divisao.ClubeIds.Select(mundo.Clube).OrderByDescending(Top11))
    {
        var media = clube.Elenco.Average(j => j.ForcaGeral);
        Console.WriteLine(string.Create(CultureInfo.GetCultureInfo("pt-BR"),
            $"  {clube.Nome,-30} {clube.Elenco.Count,6} {Top11(clube),7:F1} {media,6:F1} {clube.Caixa / 1_000_000m,14:F1}"));
    }
}

return 0;

static double Top11(Jogo.Dominio.Clubes.Clube clube) =>
    clube.Elenco.OrderByDescending(j => j.ForcaGeral).Take(11).Average(j => j.ForcaGeral);
