using System.Text.Json.Serialization;

namespace Jogo.Aplicacao.Saves;

// Formato do save (versão 1). Guarda tudo que não dá para recalcular:
// o mundo inteiro (com o estado de carreira de cada jogador), as rodadas disputadas e a tática.
// O calendário NÃO é salvo: é recalculado pela seed e sai idêntico.
// Dos jogos guardamos só o necessário para tabela, artilharia e estatísticas do elenco;
// os lances completos ficam apenas para o jogo do clube do jogador na última rodada.

internal sealed class SaveJson
{
    public int VersaoFormato { get; set; }
    public string? VersaoJogo { get; set; }
    public string? ClubeId { get; set; }

    /// <summary>Seed como texto: inteiro de 64 bits não cabe com segurança em número JavaScript.</summary>
    public string? Seed { get; set; }

    public int Ano { get; set; }
    public List<DivisaoSave>? Divisoes { get; set; }
    public List<ClubeSave>? Clubes { get; set; }
    public TemporadaSave? Temporada { get; set; }
    public TaticaSave? Tatica { get; set; }
}

internal sealed class DivisaoSave
{
    public string? Id { get; set; }
    public string? Nome { get; set; }
    public int Nivel { get; set; }
    public List<string>? Clubes { get; set; }
    public int VagasAcesso { get; set; }
    public int VagasRebaixamento { get; set; }
}

internal sealed class ClubeSave
{
    public string? Id { get; set; }
    public string? Nome { get; set; }
    public string? Sigla { get; set; }
    public string? Cidade { get; set; }
    public string? Uf { get; set; }
    public string? CorPrimaria { get; set; }
    public string? CorSecundaria { get; set; }
    public string? Estadio { get; set; }
    public int Capacidade { get; set; }
    public int Torcida { get; set; }
    public int Reputacao { get; set; }
    public decimal Caixa { get; set; }
    public List<JogadorSave>? Elenco { get; set; }
}

internal sealed class JogadorSave
{
    public int Id { get; set; }
    public string? Nome { get; set; }
    public int Idade { get; set; }
    public string? Posicao { get; set; }
    public List<string>? Secundarias { get; set; }

    /// <summary>Os 10 atributos na ordem de <c>TipoAtributo</c>.</summary>
    public int[]? Atributos { get; set; }

    /// <summary>Potencial, propensão a lesão e profissionalismo.</summary>
    public int[]? Ocultos { get; set; }

    public decimal Salario { get; set; }
    public int ContratoAte { get; set; }
    public int Condicao { get; set; }
    public int Moral { get; set; }
    public int Lesao { get; set; }
    public int Suspensao { get; set; }
    public int Amarelos { get; set; }
    public int Sequencia { get; set; }
}

internal sealed class TemporadaSave
{
    public int RodadaAtual { get; set; }
    public List<JogoSave>? Jogos { get; set; }
}

/// <summary>Um jogo disputado, em forma compacta (são 760 por temporada).</summary>
internal sealed class JogoSave
{
    [JsonPropertyName("d")] public string? Divisao { get; set; }
    [JsonPropertyName("r")] public int Rodada { get; set; }
    [JsonPropertyName("m")] public string? Mandante { get; set; }
    [JsonPropertyName("v")] public string? Visitante { get; set; }
    [JsonPropertyName("gm")] public int GolsMandante { get; set; }
    [JsonPropertyName("gv")] public int GolsVisitante { get; set; }
    [JsonPropertyName("am")] public int AmarelosMandante { get; set; }
    [JsonPropertyName("av")] public int AmarelosVisitante { get; set; }
    [JsonPropertyName("vm")] public int VermelhosMandante { get; set; }
    [JsonPropertyName("vv")] public int VermelhosVisitante { get; set; }

    /// <summary>Ids de quem entrou em campo pelo mandante.</summary>
    [JsonPropertyName("pm")] public int[]? ParticipantesMandante { get; set; }

    [JsonPropertyName("pv")] public int[]? ParticipantesVisitante { get; set; }

    /// <summary>Id de quem marcou, uma vez por gol.</summary>
    [JsonPropertyName("sm")] public int[]? AutoresMandante { get; set; }

    [JsonPropertyName("sv")] public int[]? AutoresVisitante { get; set; }

    /// <summary>Lances e estatísticas completas (só o jogo do jogador na última rodada).</summary>
    [JsonPropertyName("x")] public DetalheSave? Detalhe { get; set; }
}

internal sealed class DetalheSave
{
    /// <summary>Gols, posse, finalizações, no alvo, escanteios, faltas, amarelos, vermelhos.</summary>
    public int[]? EstatisticasMandante { get; set; }

    public int[]? EstatisticasVisitante { get; set; }
    public List<EventoSave>? Eventos { get; set; }
}

internal sealed class EventoSave
{
    [JsonPropertyName("m")] public int Minuto { get; set; }
    [JsonPropertyName("a")] public int Acrescimo { get; set; }
    [JsonPropertyName("t")] public string? Tipo { get; set; }
    [JsonPropertyName("e")] public string? Equipe { get; set; }
    [JsonPropertyName("j")] public int? Jogador { get; set; }
    [JsonPropertyName("o")] public int? Outro { get; set; }
    [JsonPropertyName("or")] public string? Origem { get; set; }
    [JsonPropertyName("dt")] public string? Detalhe { get; set; }
}

internal sealed class TaticaSave
{
    public string? Formacao { get; set; }
    public string? Mentalidade { get; set; }
    public List<int>? Titulares { get; set; }
    public List<int>? Reservas { get; set; }
}

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(SaveJson))]
internal sealed partial class ContextoSave : JsonSerializerContext;
