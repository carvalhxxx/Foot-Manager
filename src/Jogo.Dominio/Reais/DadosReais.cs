using Jogo.Dominio.Jogadores;

namespace Jogo.Dominio.Reais;

/// <summary>Grupo de posição quando a fonte não diz a posição exata.</summary>
public enum GrupoDePosicao
{
    Goleiro,
    Defensor,
    Meia,
    Atacante,
}

/// <summary>
/// Um jogador real: nome, idade e valor de mercado vêm de fontes públicas; a posição pode ser exata
/// ou só o grupo. Os atributos são estimados por nós (D-033).
/// </summary>
public sealed record JogadorReal(string Nome, Posicao? Posicao, GrupoDePosicao Grupo, int? Idade, decimal? ValorEmEuros);

/// <summary>Um clube real com o elenco conhecido (que pode estar incompleto ou vazio).</summary>
public sealed record ClubeReal(
    string Id, string Nome, string Sigla, string Cidade, string Uf, string CorPrimaria, string CorSecundaria,
    string Estadio, int Capacidade, int Divisao, int Torcida, int Reputacao, IReadOnlyList<JogadorReal> Elenco);
