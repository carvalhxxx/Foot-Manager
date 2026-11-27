using Jogo.Dominio.Jogadores;

namespace Jogo.Motor;

/// <summary>Postura do time. Troca risco por chances; pode mudar durante a partida.</summary>
public enum Mentalidade
{
    MuitoDefensiva = -2,
    Defensiva = -1,
    Equilibrada = 0,
    Ofensiva = 1,
    MuitoOfensiva = 2,
}

/// <summary>Intensidade da marcação: forte tira chances do adversário, mas faz mais faltas e cansa mais.</summary>
public enum Marcacao
{
    Leve = -1,
    Normal = 0,
    Forte = 1,
}

/// <summary>Por onde o time concentra os ataques.</summary>
public enum Concentracao
{
    Variado,
    Esquerda,
    Meio,
    Direita,
}

/// <summary>
/// Formação tática: as 10 posições de linha (o goleiro é implícito).
/// Formações fora do padrão (ex.: 5-0-5) são permitidas: o motor é que não deixa elas compensarem.
/// </summary>
public sealed record Formacao
{
    public const int JogadoresDeLinha = 10;

    public Formacao(string nome, IReadOnlyList<Posicao> posicoes)
    {
        ArgumentNullException.ThrowIfNull(posicoes);
        if (string.IsNullOrWhiteSpace(nome))
        {
            throw new ArgumentException("A formação precisa de nome.", nameof(nome));
        }

        if (posicoes.Count != JogadoresDeLinha)
        {
            throw new ArgumentException($"A formação precisa de {JogadoresDeLinha} posições de linha.", nameof(posicoes));
        }

        if (posicoes.Contains(Posicao.Goleiro))
        {
            throw new ArgumentException("O goleiro é implícito: não coloque GOL nas posições de linha.", nameof(posicoes));
        }

        Nome = nome;
        Posicoes = [.. posicoes];
    }

    public string Nome { get; }

    /// <summary>Posições de linha, da defesa para o ataque.</summary>
    public IReadOnlyList<Posicao> Posicoes { get; }

    /// <summary>As 11 posições, com o goleiro na primeira.</summary>
    public IReadOnlyList<Posicao> PosicoesComGoleiro => [Posicao.Goleiro, .. Posicoes];

    public bool Equals(Formacao? outra) => outra is not null && Nome == outra.Nome && Posicoes.SequenceEqual(outra.Posicoes);

    public override int GetHashCode() => HashCode.Combine(Nome, Posicoes.Count);
}

/// <summary>Formações disponíveis para o jogador e para a IA.</summary>
public static class FormacoesPadrao
{
    public static Formacao QuatroQuatroDois { get; } = new("4-4-2",
    [
        Posicao.LateralDireito, Posicao.Zagueiro, Posicao.Zagueiro, Posicao.LateralEsquerdo,
        Posicao.MeiaDireito, Posicao.MeiaCentral, Posicao.MeiaCentral, Posicao.MeiaEsquerdo,
        Posicao.Centroavante, Posicao.Centroavante,
    ]);

    public static Formacao QuatroTresTres { get; } = new("4-3-3",
    [
        Posicao.LateralDireito, Posicao.Zagueiro, Posicao.Zagueiro, Posicao.LateralEsquerdo,
        Posicao.Volante, Posicao.MeiaCentral, Posicao.MeiaCentral,
        Posicao.PontaDireita, Posicao.Centroavante, Posicao.PontaEsquerda,
    ]);

    public static Formacao TresCincoDois { get; } = new("3-5-2",
    [
        Posicao.Zagueiro, Posicao.Zagueiro, Posicao.Zagueiro,
        Posicao.AlaDireito, Posicao.Volante, Posicao.MeiaCentral, Posicao.MeiaOfensivo, Posicao.AlaEsquerdo,
        Posicao.SegundoAtacante, Posicao.Centroavante,
    ]);

    public static Formacao QuatroDoisTresUm { get; } = new("4-2-3-1",
    [
        Posicao.LateralDireito, Posicao.Zagueiro, Posicao.Zagueiro, Posicao.LateralEsquerdo,
        Posicao.Volante, Posicao.Volante,
        Posicao.PontaDireita, Posicao.MeiaOfensivo, Posicao.PontaEsquerda,
        Posicao.Centroavante,
    ]);

    public static Formacao CincoTresDois { get; } = new("5-3-2",
    [
        Posicao.AlaDireito, Posicao.Zagueiro, Posicao.Zagueiro, Posicao.Zagueiro, Posicao.AlaEsquerdo,
        Posicao.Volante, Posicao.MeiaCentral, Posicao.MeiaCentral,
        Posicao.SegundoAtacante, Posicao.Centroavante,
    ]);

    public static Formacao QuatroCincoUm { get; } = new("4-5-1",
    [
        Posicao.LateralDireito, Posicao.Zagueiro, Posicao.Zagueiro, Posicao.LateralEsquerdo,
        Posicao.MeiaDireito, Posicao.Volante, Posicao.MeiaCentral, Posicao.MeiaCentral, Posicao.MeiaEsquerdo,
        Posicao.Centroavante,
    ]);

    public static Formacao TresQuatroTres { get; } = new("3-4-3",
    [
        Posicao.Zagueiro, Posicao.Zagueiro, Posicao.Zagueiro,
        Posicao.MeiaDireito, Posicao.MeiaCentral, Posicao.MeiaCentral, Posicao.MeiaEsquerdo,
        Posicao.PontaDireita, Posicao.Centroavante, Posicao.PontaEsquerda,
    ]);

    public static IReadOnlyList<Formacao> Todas { get; } =
    [
        QuatroQuatroDois, QuatroTresTres, TresCincoDois, QuatroDoisTresUm, CincoTresDois, QuatroCincoUm, TresQuatroTres,
    ];
}
