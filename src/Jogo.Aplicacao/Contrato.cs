using System.Text.Json.Serialization;

namespace Jogo.Aplicacao;

// Contrato de dados com a interface web (JSON em camelCase).
// Qualquer mudança aqui precisa ser espelhada em web/src/nucleo/contrato.ts.

public sealed record ErroDto(string Arquivo, string Local, string Mensagem);

/// <summary>Envelope de toda resposta: { ok: true, dados } ou { ok: false, erros }.</summary>
public sealed record Resposta<T>(bool Ok, T? Dados, IReadOnlyList<ErroDto>? Erros)
{
    public static Resposta<T> Sucesso(T dados) => new(true, dados, null);

    public static Resposta<T> Falha(params ErroDto[] erros) => new(false, default, erros);

    public static Resposta<T> Falha(string mensagem) => Falha(new ErroDto("", "", mensagem));
}

public sealed record ClubeResumoDto(
    string Id, string Nome, string Sigla, string Cidade, string Uf,
    string CorPrimaria, string CorSecundaria, double ForcaTop11, int Jogadores, int Torcida);

public sealed record DivisaoResumoDto(string Id, string Nome, IReadOnlyList<ClubeResumoDto> Clubes);

public sealed record ResumoMundoDto(string Nome, int Temporada, int TotalJogadores, IReadOnlyList<DivisaoResumoDto> Divisoes);

// ------------------------------------------------------------------ carreira

public sealed record ProximoJogoDto(int Rodada, ClubeResumoDto Adversario, bool EmCasa);

public sealed record ResultadoRecenteDto(int Rodada, string AdversarioSigla, bool EmCasa, int GolsPro, int GolsContra);

public sealed record EstadoDto(
    int Ano,
    int RodadaAtual,
    int TotalRodadas,
    bool TemporadaEncerrada,
    ClubeResumoDto Clube,
    string DivisaoId,
    string DivisaoNome,
    int Posicao,
    int Pontos,
    ProximoJogoDto? ProximoJogo,
    IReadOnlyList<ResultadoRecenteDto> UltimosResultados,
    IReadOnlyList<string> AjustesNaEscalacao,
    bool RodadaAoVivo);

public sealed record AtributosDto(
    int Finalizacao, int Passe, int Desarme, int Drible, int Velocidade,
    int Resistencia, int Decisao, int Determinacao, int Reflexos, int Posicionamento);

public sealed record JogadorDto(
    int Id,
    string Nome,
    int Idade,
    string Posicao,
    IReadOnlyList<string> PosicoesSecundarias,
    int Forca,
    bool Estrela,
    int Condicao,
    int Moral,
    bool Disponivel,
    int RodadasLesionado,
    int RodadasSuspenso,
    int Amarelos,
    decimal Salario,
    int ContratoAte,
    int Jogos,
    int Gols,
    AtributosDto Atributos,
    decimal ValorDeMercado,
    bool PodeRenovar,
    decimal SalarioPedido,
    decimal CustoDaDispensa);

public sealed record FormacaoDto(string Nome, IReadOnlyList<string> Posicoes);

public sealed record TitularDto(string Posicao, int JogadorId, int ForcaNaPosicao, bool Improvisado);

public sealed record TaticaDto(
    string Formacao,
    string Mentalidade,
    IReadOnlyList<TitularDto> Titulares,
    IReadOnlyList<int> Reservas,
    IReadOnlyList<FormacaoDto> Formacoes,
    IReadOnlyList<string> Mentalidades);

/// <summary>O que a interface envia para mudar a tática.</summary>
public sealed record DefinicaoTaticaDto(string Formacao, string Mentalidade, IReadOnlyList<int> Titulares, IReadOnlyList<int> Reservas);

// ------------------------------------------------------------------ partidas

public sealed record EstatisticasDto(int Finalizacoes, int NoAlvo, int Escanteios, int Faltas, int Amarelos, int Vermelhos);

public sealed record EventoDto(int Minuto, int Acrescimo, string Tipo, string? Equipe, string? Jogador, string? OutroJogador, string? Origem, string? Detalhe);

public sealed record PartidaDto(
    int Rodada,
    ClubeResumoDto Mandante,
    ClubeResumoDto Visitante,
    int GolsMandante,
    int GolsVisitante,
    int PosseMandante,
    EstatisticasDto EstatisticasMandante,
    EstatisticasDto EstatisticasVisitante,
    IReadOnlyList<EventoDto> Eventos);

public sealed record PlacarDto(string MandanteId, string MandanteSigla, string MandanteNome, int GolsMandante,
    string VisitanteId, string VisitanteSigla, string VisitanteNome, int GolsVisitante);

public sealed record RodadaDivisaoDto(string DivisaoId, string DivisaoNome, IReadOnlyList<PlacarDto> Jogos);

public sealed record RodadaDto(int Rodada, PartidaDto? MinhaPartida, IReadOnlyList<RodadaDivisaoDto> Divisoes);

// ------------------------------------------------------------------ ao vivo

public sealed record JogadorEmCampoDto(int Id, string Nome, string Posicao, int Forca, int Condicao, int Amarelos, int Gols);

public sealed record ReservaDto(int Id, string Nome, string Posicao, int Forca, int Condicao);

/// <summary>Posição deixada por um lesionado, à espera de substituição.</summary>
public sealed record VagaDto(int JogadorId, string Nome, string Posicao);

public sealed record MinhaPartidaAoVivoDto(
    ClubeResumoDto Mandante,
    ClubeResumoDto Visitante,
    bool SouMandante,
    int GolsMandante,
    int GolsVisitante,
    int Tempo,
    int Minuto,
    int Acrescimo,
    bool Encerrada,
    int PosseMandante,
    EstatisticasDto EstatisticasMandante,
    EstatisticasDto EstatisticasVisitante,
    string Formacao,
    string Mentalidade,
    int SubstituicoesRestantes,
    IReadOnlyList<JogadorEmCampoDto> EmCampo,
    IReadOnlyList<ReservaDto> Reservas,
    IReadOnlyList<VagaDto> VagasAbertas,
    IReadOnlyList<EventoDto> Eventos);

public sealed record PlacarAoVivoDto(
    string MandanteId, string MandanteSigla, int GolsMandante,
    string VisitanteId, string VisitanteSigla, int GolsVisitante,
    int Minuto, int Acrescimo, bool Encerrado);

public sealed record RodadaDivisaoAoVivoDto(string DivisaoId, string DivisaoNome, IReadOnlyList<PlacarAoVivoDto> Jogos);

/// <summary>
/// Foto da rodada ao vivo. <see cref="Pausa"/> diz por que a simulação parou sozinha
/// ("intervalo", "lesao", "expulsao" ou "fim"); nula se parou só porque os minutos pedidos acabaram.
/// </summary>
public sealed record AoVivoDto(
    int Rodada,
    bool RodadaEncerrada,
    string? Pausa,
    MinhaPartidaAoVivoDto? MinhaPartida,
    IReadOnlyList<RodadaDivisaoAoVivoDto> Placares);

/// <summary>Decisão do jogador durante o jogo: "substituicao", "mentalidade" ou "formacao".</summary>
public sealed record DecisaoDto(string Tipo, int? SaiId = null, int? EntraId = null, string? Mentalidade = null, string? Formacao = null);

// ------------------------------------------------------------------ finanças

public sealed record LancamentoDto(string Categoria, decimal Valor);

public sealed record FinancasDto(
    decimal Caixa,
    decimal ResultadoDaTemporada,
    IReadOnlyList<LancamentoDto> Extrato,
    decimal FolhaMensal,
    decimal CotaTvAnual,
    decimal PatrocinioAnual,
    decimal PrecoIngresso,
    decimal PrecoSugerido,
    bool PrecoPersonalizado,
    int Capacidade,
    int? UltimoPublico,
    decimal? UltimaRenda);

// ------------------------------------------------------------------ saves

/// <summary>Save pronto para guardar: o conteúdo (texto opaco) e um resumo para a lista de saves.</summary>
public sealed record SaveDto(
    string Conteudo, string ClubeId, string ClubeNome, string CorPrimaria, string CorSecundaria,
    int Ano, int Rodada, string Divisao);

// ------------------------------------------------------------------ tabelas

public sealed record LinhaTabelaDto(
    int Posicao, string ClubeId, string Nome, string Sigla, string CorPrimaria, string CorSecundaria,
    int Jogos, int Vitorias, int Empates, int Derrotas, int GolsPro, int GolsContra, int Saldo, int Pontos);

public sealed record ArtilheiroDto(string Jogador, string ClubeSigla, int Gols, int Jogos);

public sealed record ClassificacaoDto(
    string DivisaoId, string Nome, int VagasAcesso, int VagasRebaixamento,
    IReadOnlyList<LinhaTabelaDto> Linhas, IReadOnlyList<ArtilheiroDto> Artilharia);

public sealed record ResumoDivisaoDto(string Nome, string Campeao, IReadOnlyList<string> Promovidos, IReadOnlyList<string> Rebaixados, ArtilheiroDto? Artilheiro);

public sealed record FimDeTemporadaDto(
    int Ano, IReadOnlyList<ResumoDivisaoDto> Divisoes, int MinhaPosicao, string MinhaDivisao,
    string? Movimento, int Aposentados, int Revelacoes, int ProximoAno, decimal MeuResultadoFinanceiro);

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(DefinicaoTaticaDto))]
[JsonSerializable(typeof(DecisaoDto))]
[JsonSerializable(typeof(Resposta<AoVivoDto>))]
[JsonSerializable(typeof(Resposta<SaveDto>))]
[JsonSerializable(typeof(Resposta<FinancasDto>))]
[JsonSerializable(typeof(Resposta<ResumoMundoDto>))]
[JsonSerializable(typeof(Resposta<EstadoDto>))]
[JsonSerializable(typeof(Resposta<IReadOnlyList<JogadorDto>>))]
[JsonSerializable(typeof(Resposta<TaticaDto>))]
[JsonSerializable(typeof(Resposta<RodadaDto>))]
[JsonSerializable(typeof(Resposta<ClassificacaoDto>))]
[JsonSerializable(typeof(Resposta<FimDeTemporadaDto>))]
internal sealed partial class ContratoJson : JsonSerializerContext;
