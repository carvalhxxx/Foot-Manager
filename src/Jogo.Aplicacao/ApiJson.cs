using System.Globalization;
using System.Text.Json;

namespace Jogo.Aplicacao;

/// <summary>
/// A mesma API de <see cref="ServicoDeJogo"/>, mas entrando e saindo texto JSON.
/// É o que o Jogo.Wasm expõe ao JavaScript: assim a fronteira é testável aqui, sem navegador.
/// </summary>
public sealed class ApiJson
{
    private readonly ServicoDeJogo _servico = new();

    public ServicoDeJogo Servico => _servico;

    public string CarregarDefinicoes(string arquivosJson) => Proteger(() =>
    {
        var arquivos = JsonSerializer.Deserialize(arquivosJson, ContratoJson.Default.DictionaryStringString)
            ?? throw new JsonException("Nenhum arquivo recebido.");
        return Json(_servico.CarregarDefinicoes(arquivos));
    }, ContratoJson.Default.RespostaResumoMundoDto);

    public string NovaCarreira(string clubeId, string seed) => Proteger(
        () => Json(_servico.NovaCarreira(clubeId, ulong.Parse(seed, CultureInfo.InvariantCulture))),
        ContratoJson.Default.RespostaEstadoDto);

    public string Estado() => Json(_servico.Estado());

    public string Elenco() => Json(_servico.Elenco());

    public string Tatica() => Json(_servico.Tatica());

    public string DefinirTatica(string definicaoJson) => Proteger(() =>
    {
        var definicao = JsonSerializer.Deserialize(definicaoJson, ContratoJson.Default.DefinicaoTaticaDto)
            ?? throw new JsonException("Tática vazia.");
        return Json(_servico.DefinirTatica(definicao));
    }, ContratoJson.Default.RespostaTaticaDto);

    public string EscalacaoAutomatica() => Json(_servico.EscalacaoAutomatica());

    public string AvancarRodada() => Json(_servico.AvancarRodada());

    public string UltimaRodada() => Json(_servico.UltimaRodada());

    public string Classificacao(string divisaoId) => Json(_servico.Classificacao(divisaoId));

    public string EncerrarTemporada() => Json(_servico.EncerrarTemporada());

    // Uma sobrecarga por tipo de resposta: o serializador gerado em compilação precisa do tipo concreto.
    private static string Json(Resposta<ResumoMundoDto> r) => JsonSerializer.Serialize(r, ContratoJson.Default.RespostaResumoMundoDto);
    private static string Json(Resposta<EstadoDto> r) => JsonSerializer.Serialize(r, ContratoJson.Default.RespostaEstadoDto);
    private static string Json(Resposta<IReadOnlyList<JogadorDto>> r) => JsonSerializer.Serialize(r, ContratoJson.Default.RespostaIReadOnlyListJogadorDto);
    private static string Json(Resposta<TaticaDto> r) => JsonSerializer.Serialize(r, ContratoJson.Default.RespostaTaticaDto);
    private static string Json(Resposta<RodadaDto> r) => JsonSerializer.Serialize(r, ContratoJson.Default.RespostaRodadaDto);
    private static string Json(Resposta<ClassificacaoDto> r) => JsonSerializer.Serialize(r, ContratoJson.Default.RespostaClassificacaoDto);
    private static string Json(Resposta<FimDeTemporadaDto> r) => JsonSerializer.Serialize(r, ContratoJson.Default.RespostaFimDeTemporadaDto);

    /// <summary>Converte entrada malformada em resposta de erro, em vez de deixar a exceção chegar ao JavaScript.</summary>
    private static string Proteger<T>(Func<string> acao, System.Text.Json.Serialization.Metadata.JsonTypeInfo<Resposta<T>> tipo)
    {
        try
        {
            return acao();
        }
        catch (Exception ex) when (ex is JsonException or FormatException or OverflowException or ArgumentException)
        {
            return JsonSerializer.Serialize(Resposta<T>.Falha(ex.Message), tipo);
        }
    }
}
