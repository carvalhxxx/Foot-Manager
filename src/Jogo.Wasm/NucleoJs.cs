using System.Reflection;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using Jogo.Aplicacao;

namespace Jogo.Wasm;

/// <summary>
/// Funções do núcleo expostas ao JavaScript. Só repassam para <see cref="ApiJson"/>, que é testada fora do navegador.
/// O estado do jogo vive aqui dentro: uma carreira por aba.
/// </summary>
[SupportedOSPlatform("browser")]
public static partial class NucleoJs
{
    private static readonly ApiJson Api = new();

    [JSExport]
    internal static string Versao() =>
        typeof(NucleoJs).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "?";

    [JSExport]
    internal static string CarregarDefinicoes(string arquivosJson) => Api.CarregarDefinicoes(arquivosJson);

    [JSExport]
    internal static string NovaCarreira(string clubeId, string seed) => Api.NovaCarreira(clubeId, seed);

    [JSExport]
    internal static string Estado() => Api.Estado();

    [JSExport]
    internal static string Elenco() => Api.Elenco();

    [JSExport]
    internal static string Tatica() => Api.Tatica();

    [JSExport]
    internal static string DefinirTatica(string definicaoJson) => Api.DefinirTatica(definicaoJson);

    [JSExport]
    internal static string EscalacaoAutomatica() => Api.EscalacaoAutomatica();

    [JSExport]
    internal static string AvancarRodada() => Api.AvancarRodada();

    [JSExport]
    internal static string UltimaRodada() => Api.UltimaRodada();

    [JSExport]
    internal static string Classificacao(string divisaoId) => Api.Classificacao(divisaoId);

    [JSExport]
    internal static string EncerrarTemporada() => Api.EncerrarTemporada();

    [JSExport]
    internal static string IniciarAoVivo() => Api.IniciarAoVivo();

    [JSExport]
    internal static string AoVivo() => Api.AoVivo();

    [JSExport]
    internal static string AvancarAoVivo(int minutos) => Api.AvancarAoVivo(minutos);

    [JSExport]
    internal static string DecidirAoVivo(string decisaoJson) => Api.DecidirAoVivo(decisaoJson);

    [JSExport]
    internal static string ConcluirAoVivo() => Api.ConcluirAoVivo();

    [JSExport]
    internal static string Financas() => Api.Financas();

    [JSExport]
    internal static string DefinirPrecoIngresso(string preco) => Api.DefinirPrecoIngresso(preco);

    [JSExport]
    internal static string RenovarContrato(int jogadorId, int anos) => Api.RenovarContrato(jogadorId, anos);

    [JSExport]
    internal static string Dispensar(int jogadorId) => Api.Dispensar(jogadorId);

    [JSExport]
    internal static string ExportarSave() => Api.ExportarSave();

    [JSExport]
    internal static string CarregarSave(string conteudo) => Api.CarregarSave(conteudo);
}
