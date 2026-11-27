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

    public string IniciarAoVivo() => Json(_servico.IniciarAoVivo());

    public string AoVivo() => Json(_servico.AoVivo());

    public string AvancarAoVivo(int minutos) => Json(_servico.AvancarAoVivo(minutos));

    public string DecidirAoVivo(string decisaoJson) => Proteger(() =>
    {
        var decisao = JsonSerializer.Deserialize(decisaoJson, ContratoJson.Default.DecisaoDto)
            ?? throw new JsonException("Decisão vazia.");
        return Json(_servico.DecidirAoVivo(decisao));
    }, ContratoJson.Default.RespostaAoVivoDto);

    public string ConcluirAoVivo() => Json(_servico.ConcluirAoVivo());

    public string Financas() => Json(_servico.Financas());

    /// <summary>Preço em texto ("" volta ao sugerido): evita perder precisão de decimal no caminho JS → C#.</summary>
    public string DefinirPrecoIngresso(string preco) => Proteger(
        () => Json(_servico.DefinirPrecoIngresso(string.IsNullOrWhiteSpace(preco) ? null : decimal.Parse(preco, CultureInfo.InvariantCulture))),
        ContratoJson.Default.RespostaFinancasDto);

    public string ExportarSave() => Json(_servico.ExportarSave());

    public string CarregarSave(string conteudo) => Json(_servico.CarregarSave(conteudo));

    // Uma sobrecarga por tipo de resposta: o serializador gerado em compilação precisa do tipo concreto.
    private static string Json(Resposta<ResumoMundoDto> r) => JsonSerializer.Serialize(r, ContratoJson.Default.RespostaResumoMundoDto);
    private static string Json(Resposta<EstadoDto> r) => JsonSerializer.Serialize(r, ContratoJson.Default.RespostaEstadoDto);
    private static string Json(Resposta<IReadOnlyList<JogadorDto>> r) => JsonSerializer.Serialize(r, ContratoJson.Default.RespostaIReadOnlyListJogadorDto);
    private static string Json(Resposta<TaticaDto> r) => JsonSerializer.Serialize(r, ContratoJson.Default.RespostaTaticaDto);
    private static string Json(Resposta<RodadaDto> r) => JsonSerializer.Serialize(r, ContratoJson.Default.RespostaRodadaDto);
    private static string Json(Resposta<ClassificacaoDto> r) => JsonSerializer.Serialize(r, ContratoJson.Default.RespostaClassificacaoDto);
    private static string Json(Resposta<FimDeTemporadaDto> r) => JsonSerializer.Serialize(r, ContratoJson.Default.RespostaFimDeTemporadaDto);
    private static string Json(Resposta<AoVivoDto> r) => JsonSerializer.Serialize(r, ContratoJson.Default.RespostaAoVivoDto);
    private static string Json(Resposta<FinancasDto> r) => JsonSerializer.Serialize(r, ContratoJson.Default.RespostaFinancasDto);
    private static string Json(Resposta<SaveDto> r) => JsonSerializer.Serialize(r, ContratoJson.Default.RespostaSaveDto);

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
