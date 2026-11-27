// CLI de calibração do motor de partida.
//
// Uso:
//   dotnet run --project tools/Jogo.Calibracao -- [--partidas 10000] [--por-formacao 1500] [--seed 1]
//
// Carrega o mundo base (dados/definicoes), simula as partidas e confere as metas.
// Sai com código 1 se alguma meta não for atingida: o CI usa isso para barrar regressões.

using System.Diagnostics;
using System.Globalization;
using Jogo.Dados.Definicoes;
using Jogo.Simulacao.Calibracao;

var partidas = 10_000;
var porFormacao = 1_500;
var seed = 1UL;

for (var i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--partidas" when i + 1 < args.Length:
            partidas = int.Parse(args[++i], CultureInfo.InvariantCulture);
            break;
        case "--por-formacao" when i + 1 < args.Length:
            porFormacao = int.Parse(args[++i], CultureInfo.InvariantCulture);
            break;
        case "--seed" when i + 1 < args.Length:
            seed = ulong.Parse(args[++i], CultureInfo.InvariantCulture);
            break;
        default:
            Console.Error.WriteLine($"Argumento desconhecido: {args[i]}");
            Console.Error.WriteLine("Uso: Jogo.Calibracao [--partidas N] [--por-formacao N] [--seed N]");
            return 2;
    }
}

var (_, mundo) = LeitorDefinicoes.Carregar(PastaDefinicoes());
var cronometro = Stopwatch.StartNew();
var relatorio = Calibrador.Executar(mundo, partidas, porFormacao, seed);
cronometro.Stop();

var pt = CultureInfo.GetCultureInfo("pt-BR");
Console.WriteLine($"Calibração: {partidas:N0} partidas de liga + {porFormacao:N0} por formação (seed {seed}) em {cronometro.Elapsed.TotalSeconds:F1}s");
Imprimir("Liga", relatorio.Liga);
Imprimir("Formações (fatia dos pontos contra o 4-4-2; 50% = equilíbrio)", relatorio.Formacoes);
Console.WriteLine();
Console.WriteLine(relatorio.Aprovado ? "✔ Todas as metas atingidas." : "✖ Há metas fora da faixa.");
return relatorio.Aprovado ? 0 : 1;

void Imprimir(string titulo, IReadOnlyList<Meta> metas)
{
    Console.WriteLine();
    Console.WriteLine(titulo);
    foreach (var meta in metas)
    {
        var faixa = (meta.Minimo, meta.Maximo) switch
        {
            (null, null) => "",
            ({ } min, null) => $"≥ {min.ToString(meta.Formato, pt)}",
            (null, { } max) => $"≤ {max.ToString(meta.Formato, pt)}",
            ({ } min, { } max) => $"{min.ToString(meta.Formato, pt)} a {max.ToString(meta.Formato, pt)}",
        };
        var marca = meta.Minimo is null && meta.Maximo is null ? " " : meta.Atingida ? "✔" : "✖";
        Console.WriteLine($"  {marca} {meta.Nome,-34} {meta.Valor.ToString(meta.Formato, pt),8}   {faixa}");
    }
}

static string PastaDefinicoes()
{
    var pasta = new DirectoryInfo(AppContext.BaseDirectory);
    while (pasta is not null && !File.Exists(Path.Combine(pasta.FullName, "FootManagerBR.slnx")))
    {
        pasta = pasta.Parent;
    }

    return pasta is null
        ? throw new DirectoryNotFoundException("Raiz do repositório não encontrada.")
        : Path.Combine(pasta.FullName, "dados", "definicoes");
}
