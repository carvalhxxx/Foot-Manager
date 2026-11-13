using System.Text.Json.Serialization;

namespace Jogo.Wasm;

// Contrato de dados com a interface web. Qualquer mudança aqui precisa ser
// espelhada em web/src/nucleo/contrato.ts.

internal sealed record ErroJs(string Arquivo, string Local, string Mensagem);

internal sealed record ClubeResumoJs(
    string Id,
    string Nome,
    string Sigla,
    string Cidade,
    string Uf,
    string CorPrimaria,
    string CorSecundaria,
    double ForcaTop11,
    int Jogadores,
    int Torcida);

internal sealed record DivisaoJs(string Id, string Nome, ClubeResumoJs[] Clubes);

internal sealed record ResumoMundoJs(string Nome, int Temporada, int TotalJogadores, DivisaoJs[] Divisoes);

internal sealed record RespostaResumoJs(bool Ok, ResumoMundoJs? Dados, ErroJs[]? Erros);

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(RespostaResumoJs))]
internal sealed partial class ContratoJson : JsonSerializerContext;
