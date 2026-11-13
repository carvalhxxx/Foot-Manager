using System.Reflection;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using System.Text.Json;
using Jogo.Dados.Definicoes;
using Jogo.Dominio;
using Jogo.Dominio.Clubes;

namespace Jogo.Wasm;

/// <summary>
/// API do núcleo exposta ao JavaScript. Regras:
/// - entra e sai só texto JSON (contrato espelhado em web/src/nucleo/contrato.ts);
/// - nenhuma exceção atravessa a fronteira: erros viram { ok: false, erros: [...] };
/// - o estado do jogo vive aqui dentro, uma partida por aba do navegador.
/// </summary>
[SupportedOSPlatform("browser")]
public static partial class NucleoJs
{
    private static Mundo? _mundo;

    [JSExport]
    internal static string Versao() =>
        typeof(NucleoJs).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "?";

    /// <summary>
    /// Carrega as definições (mapa "caminho relativo" → conteúdo do arquivo) e devolve o resumo do mundo.
    /// </summary>
    [JSExport]
    internal static string CarregarDefinicoes(string arquivosJson)
    {
        try
        {
            var arquivos = JsonSerializer.Deserialize(arquivosJson, ContratoJson.Default.DictionaryStringString)
                ?? throw new ArgumentException("Nenhum arquivo recebido.");

            var (manifesto, mundo) = LeitorDefinicoes.Carregar(new FonteMemoria(arquivos));
            _mundo = mundo;
            return Ok(Resumir(manifesto, mundo));
        }
        catch (DefinicoesInvalidasException ex)
        {
            return Falha([.. ex.Erros.Select(e => new ErroJs(e.Arquivo, e.Local, e.Mensagem))]);
        }
        catch (Exception ex) when (ex is ArgumentException or JsonException)
        {
            return Falha([new ErroJs("", "", ex.Message)]);
        }
    }

    private static ResumoMundoJs Resumir(Manifesto manifesto, Mundo mundo) => new(
        manifesto.Nome,
        mundo.TemporadaAtual,
        mundo.Clubes.Sum(c => c.Elenco.Count),
        [.. mundo.Divisoes.Select(d => new DivisaoJs(
            d.Id,
            d.Nome,
            [.. d.ClubeIds.Select(mundo.Clube).Select(ResumirClube).OrderByDescending(c => c.ForcaTop11)]))]);

    private static ClubeResumoJs ResumirClube(Clube c) => new(
        c.Id, c.Nome, c.Sigla, c.Cidade, c.Uf, c.Cores.Primaria, c.Cores.Secundaria,
        Math.Round(c.Elenco.OrderByDescending(j => j.ForcaGeral).Take(11).Average(j => j.ForcaGeral), 1),
        c.Elenco.Count,
        c.Torcida);

    private static string Ok(ResumoMundoJs dados) =>
        JsonSerializer.Serialize(new RespostaResumoJs(true, dados, null), ContratoJson.Default.RespostaResumoJs);

    private static string Falha(ErroJs[] erros) =>
        JsonSerializer.Serialize(new RespostaResumoJs(false, null, erros), ContratoJson.Default.RespostaResumoJs);
}
