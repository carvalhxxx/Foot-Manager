using System.Text.Json.Serialization;

namespace Jogo.Dados.Definicoes;

// Espelho exato dos arquivos JSON (docs/FORMATO_DADOS.md).
// Todos os campos são anuláveis de propósito: a ausência é detectada na conversão,
// que gera uma mensagem em português apontando o arquivo e o campo.

internal sealed class ManifestoJson
{
    public int? VersaoFormato { get; set; }
    public string? Nome { get; set; }
    public string? Autor { get; set; }
    public string? Descricao { get; set; }
    public int? TemporadaInicial { get; set; }
}

internal sealed class DivisoesJson
{
    public int? VersaoFormato { get; set; }
    public List<DivisaoJson>? Divisoes { get; set; }
}

internal sealed class DivisaoJson
{
    public string? Id { get; set; }
    public string? Nome { get; set; }
    public int? Nivel { get; set; }
    public int? VagasAcesso { get; set; }
    public int? VagasRebaixamento { get; set; }
    public List<string>? Clubes { get; set; }
}

internal sealed class ClubeJson
{
    public int? VersaoFormato { get; set; }
    public string? Id { get; set; }
    public string? Nome { get; set; }
    public string? Sigla { get; set; }
    public string? Cidade { get; set; }
    public string? Uf { get; set; }
    public CoresJson? Cores { get; set; }
    public EstadioJson? Estadio { get; set; }
    public int? Torcida { get; set; }
    public int? Reputacao { get; set; }
    public decimal? Caixa { get; set; }
    public List<JogadorJson>? Elenco { get; set; }
}

internal sealed class CoresJson
{
    public string? Primaria { get; set; }
    public string? Secundaria { get; set; }
}

internal sealed class EstadioJson
{
    public string? Nome { get; set; }
    public int? Capacidade { get; set; }
}

internal sealed class JogadorJson
{
    public int? Id { get; set; }
    public string? Nome { get; set; }
    public int? Idade { get; set; }
    public string? Posicao { get; set; }
    public List<string>? PosicoesSecundarias { get; set; }
    public AtributosJson? Atributos { get; set; }
    public OcultosJson? Ocultos { get; set; }
    public decimal? Salario { get; set; }
    public int? ContratoAte { get; set; }
}

internal sealed class AtributosJson
{
    public int? Finalizacao { get; set; }
    public int? Passe { get; set; }
    public int? Desarme { get; set; }
    public int? Drible { get; set; }
    public int? Velocidade { get; set; }
    public int? Resistencia { get; set; }
    public int? Decisao { get; set; }
    public int? Determinacao { get; set; }
    public int? Reflexos { get; set; }
    public int? Posicionamento { get; set; }
}

internal sealed class OcultosJson
{
    public int? Potencial { get; set; }
    public int? PropensaoLesao { get; set; }
    public int? Profissionalismo { get; set; }
}

/// <summary>
/// Serialização gerada em tempo de compilação (sem reflexão: rápido e compatível com AOT/mobile).
/// As opções (camelCase, indentação, acentos legíveis) ficam em <see cref="FormatoDefinicoes"/>.
/// </summary>
[JsonSerializable(typeof(ManifestoJson))]
[JsonSerializable(typeof(DivisoesJson))]
[JsonSerializable(typeof(ClubeJson))]
internal sealed partial class ContextoJson : JsonSerializerContext;
