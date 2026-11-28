namespace Jogo.Dominio.Clubes;

/// <summary>Categorias do extrato do clube. A ordem é a de exibição.</summary>
public enum CategoriaFinanceira
{
    Bilheteria,
    CotaDeTv,
    Patrocinio,
    Premiacao,
    VendaDeJogadores,
    Salarios,

    /// <summary>Encargos, comissão técnica, CT, viagens e estrutura (D-041).</summary>
    CustosOperacionais,
    CompraDeJogadores,
    Rescisoes,
}
