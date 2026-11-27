namespace Jogo.Dominio.Competicoes;

/// <summary>
/// Uma divisão de pontos corridos (ex.: Série A). Nível 1 é a mais alta.
/// Os clubes sobem para o nível − 1 e descem para o nível + 1 (D-013: 4 sobem e 4 descem).
/// </summary>
public sealed class Divisao
{
    public Divisao(
        string id,
        string nome,
        int nivel,
        IEnumerable<string> clubeIds,
        int vagasAcesso,
        int vagasRebaixamento,
        int? vagasDiretas = null)
    {
        ArgumentNullException.ThrowIfNull(clubeIds);

        Id = Validacao.Identificador(id, nameof(id));
        Nome = Validacao.Texto(nome, nameof(nome));
        Nivel = Validacao.Faixa(nivel, 1, 10, nameof(nivel));

        var ids = clubeIds.Select(c => Validacao.Identificador(c, nameof(clubeIds))).ToArray();
        if (ids.Length < 2 || ids.Length % 2 != 0)
        {
            throw new ArgumentException("Uma divisão precisa de um número par de clubes (mínimo 2).", nameof(clubeIds));
        }

        if (ids.Distinct(StringComparer.Ordinal).Count() != ids.Length)
        {
            throw new ArgumentException("Clube repetido na divisão.", nameof(clubeIds));
        }

        ClubeIds = ids;
        VagasAcesso = Validacao.Faixa(vagasAcesso, 0, ids.Length / 2, nameof(vagasAcesso));
        VagasRebaixamento = Validacao.Faixa(vagasRebaixamento, 0, ids.Length / 2, nameof(vagasRebaixamento));

        VagasDiretas = Validacao.Faixa(vagasDiretas ?? VagasAcesso, 0, VagasAcesso, nameof(vagasDiretas));
        if (VagasDiretas + (2 * VagasNoPlayoff) > ids.Length - VagasRebaixamento)
        {
            throw new ArgumentException("O playoff de acesso precisa de mais clubes do que a divisão tem fora da zona de rebaixamento.", nameof(vagasDiretas));
        }

        if (nivel == 1 && vagasAcesso != 0)
        {
            throw new ArgumentException("A primeira divisão não tem acesso.", nameof(vagasAcesso));
        }
    }

    public string Id { get; }
    public string Nome { get; }
    public int Nivel { get; }

    /// <summary>Clubes participantes, na ordem das definições.</summary>
    public IReadOnlyList<string> ClubeIds { get; }

    /// <summary>Quantos sobem para a divisão de cima.</summary>
    public int VagasAcesso { get; }

    /// <summary>Quantos sobem direto pela tabela; o resto das vagas de acesso sai do playoff.</summary>
    public int VagasDiretas { get; }

    /// <summary>Vagas decididas no playoff (cada uma é um confronto de ida e volta).</summary>
    public int VagasNoPlayoff => VagasAcesso - VagasDiretas;

    public bool TemPlayoff => VagasNoPlayoff > 0;

    /// <summary>
    /// Confrontos do playoff por posição na tabela: o melhor colocado contra o pior da faixa
    /// (ex.: 2 vagas diretas e 2 no playoff → 3º x 6º e 4º x 5º).
    /// </summary>
    public IReadOnlyList<(int MelhorPosicao, int PiorPosicao)> ConfrontosDoPlayoff =>
        [.. Enumerable.Range(0, VagasNoPlayoff).Select(i => (VagasDiretas + 1 + i, VagasDiretas + (2 * VagasNoPlayoff) - i))];

    /// <summary>Quantos descem para a divisão de baixo.</summary>
    public int VagasRebaixamento { get; }

    /// <summary>Rodadas num turno e returno (ida e volta contra cada adversário).</summary>
    public int TotalRodadas => (ClubeIds.Count - 1) * 2;
}
