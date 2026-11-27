using Jogo.Dominio.Jogadores;

namespace Jogo.Motor;

/// <summary>O que o treinador entrega antes do jogo: formação, titulares, reservas e mentalidade.</summary>
public sealed class TimeEscalado
{
    public const int MaximoReservas = 9;
    public const double BonusEntrosamentoMaximo = 0.1;

    /// <param name="titulares">11 jogadores alinhados com <see cref="Formacao.PosicoesComGoleiro"/> (o primeiro vai no gol).</param>
    public TimeEscalado(
        string clubeId,
        Formacao formacao,
        IReadOnlyList<Jogador> titulares,
        IReadOnlyList<Jogador> reservas,
        Mentalidade mentalidade = Mentalidade.Equilibrada,
        double bonusEntrosamento = 0)
    {
        ArgumentNullException.ThrowIfNull(formacao);
        ArgumentNullException.ThrowIfNull(titulares);
        ArgumentNullException.ThrowIfNull(reservas);

        if (titulares.Count != Formacao.JogadoresDeLinha + 1)
        {
            throw new ArgumentException("A escalação precisa de 11 titulares.", nameof(titulares));
        }

        if (reservas.Count > MaximoReservas)
        {
            throw new ArgumentException($"No máximo {MaximoReservas} reservas.", nameof(reservas));
        }

        var ids = titulares.Concat(reservas).Select(j => j.Id).ToArray();
        if (ids.Distinct().Count() != ids.Length)
        {
            throw new ArgumentException("Jogador repetido na escalação.", nameof(titulares));
        }

        if (bonusEntrosamento is < 0 or > BonusEntrosamentoMaximo || double.IsNaN(bonusEntrosamento))
        {
            throw new ArgumentOutOfRangeException(nameof(bonusEntrosamento), bonusEntrosamento, "Bônus de entrosamento fora da faixa.");
        }

        ClubeId = clubeId;
        BonusEntrosamento = bonusEntrosamento;
        Formacao = formacao;
        Titulares = [.. titulares];
        Reservas = [.. reservas];
        Mentalidade = mentalidade;
    }

    public string ClubeId { get; }
    public Formacao Formacao { get; }
    public IReadOnlyList<Jogador> Titulares { get; }
    public IReadOnlyList<Jogador> Reservas { get; }
    public Mentalidade Mentalidade { get; }

    /// <summary>Bônus de força em todos os setores por jogarem juntos há várias rodadas (0 a 0,1).</summary>
    public double BonusEntrosamento { get; }

    /// <summary>Cópia com outro bônus de entrosamento.</summary>
    public TimeEscalado ComEntrosamento(double bonus) => new(ClubeId, Formacao, Titulares, Reservas, Mentalidade, bonus);
}
