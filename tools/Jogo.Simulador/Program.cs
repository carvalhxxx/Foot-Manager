// Simula temporadas completas a partir do mundo base, sem interface.
//
// Uso:
//   dotnet run --project tools/Jogo.Simulador -- [--temporadas 1] [--seed 1]
//
// Com 1 temporada: mostra as tabelas finais das Séries A e B, artilheiros, acesso e rebaixamento.
// Com várias: um resumo por ano (campeões, quem subiu e desceu) e as tabelas da última.

using System.Globalization;
using Jogo.Dados.Definicoes;
using Jogo.Dominio;
using Jogo.Simulacao.Temporadas;

var temporadas = 1;
var seed = 1UL;

for (var i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--temporadas" when i + 1 < args.Length:
            temporadas = int.Parse(args[++i], CultureInfo.InvariantCulture);
            break;
        case "--seed" when i + 1 < args.Length:
            seed = ulong.Parse(args[++i], CultureInfo.InvariantCulture);
            break;
        default:
            Console.Error.WriteLine($"Argumento desconhecido: {args[i]}");
            Console.Error.WriteLine("Uso: Jogo.Simulador [--temporadas N] [--seed N]");
            return 2;
    }
}

var (_, mundo) = LeitorDefinicoes.Carregar(PastaDefinicoes());
ResultadoTemporada? ultimo = null;
Temporada? ultimaTemporada = null;

for (var t = 0; t < temporadas; t++)
{
    var temporada = new Temporada(mundo, seed + (ulong)t);
    temporada.JogarAteOFim();
    ultimaTemporada = temporada;
    ultimo = FimDeTemporada.Encerrar(temporada, mundo, seed + (ulong)t);

    if (temporadas > 1)
    {
        var a = ultimo.Divisoes[0];
        var b = ultimo.Divisoes[1];
        Console.WriteLine($"{ultimo.Ano}: campeão {Nome(a.CampeaoId)} ({a.TabelaFinal[0].Pontos} pts) · " +
                          $"Série B: {Nome(b.CampeaoId)} · sobem {string.Join(", ", b.Promovidos.Select(Sigla))} · " +
                          $"descem {string.Join(", ", a.Rebaixados.Select(Sigla))} · " +
                          $"{ultimo.Aposentados.Count} aposentados, {ultimo.Revelacoes.Count} revelações");
    }
}

if (ultimo is null || ultimaTemporada is null)
{
    return 0;
}

foreach (var divisao in ultimo.Divisoes)
{
    Console.WriteLine();
    Console.WriteLine($"{divisao.Nome} {ultimo.Ano}");
    Console.WriteLine($"  {"#",2}  {"Clube",-30} {"P",3} {"J",3} {"V",3} {"E",3} {"D",3} {"GP",3} {"GC",3} {"SG",4}");
    foreach (var l in divisao.TabelaFinal)
    {
        var marca = divisao.Promovidos.Contains(l.ClubeId) ? "↑" : divisao.Rebaixados.Contains(l.ClubeId) ? "↓" : " ";
        Console.WriteLine($"{marca} {l.Posicao,2}  {Nome(l.ClubeId),-30} {l.Pontos,3} {l.Jogos,3} {l.Vitorias,3} {l.Empates,3} {l.Derrotas,3} {l.GolsPro,3} {l.GolsContra,3} {l.Saldo,4:+0;-0;0}");
    }

    Console.WriteLine();
    Console.WriteLine("  Artilheiros");
    foreach (var a in ultimaTemporada.Artilharia(divisao.DivisaoId, 5))
    {
        var jogador = mundo.Clubes.SelectMany(c => c.Elenco).FirstOrDefault(j => j.Id == a.JogadorId);
        Console.WriteLine($"    {a.Gols,2} gols  {jogador?.Nome ?? $"#{a.JogadorId} (aposentado)",-24} {Nome(a.ClubeId)}");
    }
}

return 0;

string Nome(string clubeId) => mundo.Clube(clubeId).Nome;
string Sigla(string clubeId) => mundo.Clube(clubeId).Sigla;

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
