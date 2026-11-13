namespace Jogo.Dominio.Jogadores;

/// <summary>Vínculo de um jogador com um clube.</summary>
public sealed record Contrato
{
    public Contrato(string clubeId, decimal salarioMensal, int temporadaFinal)
    {
        ClubeId = Validacao.Identificador(clubeId, nameof(clubeId));
        SalarioMensal = salarioMensal > 0
            ? salarioMensal
            : throw new ArgumentOutOfRangeException(nameof(salarioMensal), salarioMensal, "Salário deve ser positivo.");
        TemporadaFinal = Validacao.Faixa(temporadaFinal, 1900, 9999, nameof(temporadaFinal));
    }

    public string ClubeId { get; }

    /// <summary>Salário mensal em reais.</summary>
    public decimal SalarioMensal { get; }

    /// <summary>Última temporada (ano) coberta pelo contrato.</summary>
    public int TemporadaFinal { get; }

    public bool VenceAoFimDe(int temporada) => TemporadaFinal <= temporada;
}
