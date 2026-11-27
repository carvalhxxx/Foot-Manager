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
        double bonusEntrosamento = 0,
        Marcacao marcacao = Marcacao.Normal,
        Concentracao concentracao = Concentracao.Variado,
        int? capitaoId = null,
        int? cobradorId = null)
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

        if (capitaoId is { } c && !titulares.Any(j => j.Id == c))
        {
            throw new ArgumentException("O capitão precisa ser titular.", nameof(capitaoId));
        }

        if (cobradorId is { } f && !titulares.Any(j => j.Id == f))
        {
            throw new ArgumentException("O cobrador de faltas precisa ser titular.", nameof(cobradorId));
        }

        ClubeId = clubeId;
        Marcacao = marcacao;
        Concentracao = concentracao;
        CapitaoId = capitaoId;
        CobradorId = cobradorId;
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
    public Marcacao Marcacao { get; }
    public Concentracao Concentracao { get; }

    /// <summary>Capitão escolhido (nulo = o titular de maior Determinação).</summary>
    public int? CapitaoId { get; }

    /// <summary>Cobrador de faltas e escanteios (nulo = sem cobrador fixo).</summary>
    public int? CobradorId { get; }

    /// <summary>Cópia com outro bônus de entrosamento.</summary>
    public TimeEscalado ComEntrosamento(double bonus) =>
        new(ClubeId, Formacao, Titulares, Reservas, Mentalidade, bonus, Marcacao, Concentracao, CapitaoId, CobradorId);

    /// <summary>Capitão efetivo: o escolhido ou o titular de maior Determinação.</summary>
    public int CapitaoEfetivo => CapitaoId ?? CapitaoAutomatico(Titulares)!.Id;

    public static Jogador? CapitaoAutomatico(IEnumerable<Jogador> titulares) =>
        titulares.OrderByDescending(j => j.Atributos.Determinacao).ThenBy(j => j.Id).FirstOrDefault();

    /// <summary>Quem assume as cobranças quando o cobrador escolhido sai de campo.</summary>
    /// <param name="deLinha">Jogadores de linha (sem o goleiro).</param>
    public static Jogador? CobradorAutomatico(IEnumerable<Jogador> deLinha) =>
        deLinha.OrderByDescending(j => j.Atributos.Finalizacao + j.Atributos.Passe).ThenBy(j => j.Id).FirstOrDefault();
}
