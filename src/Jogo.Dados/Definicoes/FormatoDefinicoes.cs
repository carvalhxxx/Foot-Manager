using System.Text.Encodings.Web;
using System.Text.Json;

namespace Jogo.Dados.Definicoes;

/// <summary>Constantes e opções compartilhadas pelo leitor e pelo escritor de definições.</summary>
public static class FormatoDefinicoes
{
    /// <summary>Versão do formato gravada pelo jogo. Arquivos com versão maior são recusados com mensagem clara.</summary>
    public const int VersaoAtual = 1;

    public const string ArquivoManifesto = "manifesto.json";
    public const string ArquivoDivisoes = "divisoes.json";
    public const string PastaClubes = "clubes";

    internal static ContextoJson Contexto { get; } = new(new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
        NewLine = "\n",
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        // Mantém acentos legíveis no arquivo ("Grêmio", não "Grêmio").
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    });
}
