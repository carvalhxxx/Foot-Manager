using Jogo.Dominio.Jogadores;

namespace Jogo.Motor;

/// <summary>O que o treinador entrega antes do jogo: formação, titulares, reservas e mentalidade.</summary>
public sealed class TimeEscalado
{
    public const int MaximoReservas = 9;

    /// <param name="titulares">11 jogadores alinhados com <see cref="Formacao.PosicoesComGoleiro"/> (o primeiro vai no gol).</param>
    public TimeEscalado(
        string clubeId,
        Formacao formacao,
        IReadOnlyList<Jogador> titulares,
        IReadOnlyList<Jogador> reservas,
        Mentalidade mentalidade = Mentalidade.Equilibrada)
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

        ClubeId = clubeId;
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
}
