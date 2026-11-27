using Jogo.Dominio;
using Jogo.Dominio.Clubes;
using Jogo.Dominio.Jogadores;

namespace Jogo.Simulacao.Mercado;

/// <summary>Renovação, dispensa e fim de contrato (do jogador e da IA).</summary>
public static class Contratos
{
    public const int AnosMinimos = 1;
    public const int AnosMaximos = 4;

    /// <summary>Multa de rescisão, em salários mensais.</summary>
    public const int SalariosDeRescisao = 3;

    /// <summary>Abaixo disso o clube não pode dispensar ninguém.</summary>
    public const int ElencoMinimoParaDispensar = 18;

    /// <summary>Máximo de agentes livres guardados (os mais fortes ficam).</summary>
    public const int MaximoDeAgentesLivres = 150;

    /// <summary>Só dá para renovar no último ano de contrato.</summary>
    public static bool PodeRenovar(Jogador jogador, int temporadaAtual) =>
        jogador.Contrato is { } c && c.VenceAoFimDe(temporadaAtual);

    /// <summary>Renova pelo salário que o jogador pede, por 1 a 4 temporadas além da atual.</summary>
    public static void Renovar(Clube clube, Jogador jogador, int temporadaAtual, int anos)
    {
        ArgumentNullException.ThrowIfNull(clube);
        ArgumentNullException.ThrowIfNull(jogador);
        if (!clube.Elenco.Contains(jogador))
        {
            throw new InvalidOperationException($"{jogador.Nome} não é do {clube.Nome}.");
        }

        if (anos is < AnosMinimos or > AnosMaximos)
        {
            throw new ArgumentOutOfRangeException(nameof(anos), anos, $"O contrato deve ter de {AnosMinimos} a {AnosMaximos} anos.");
        }

        if (!PodeRenovar(jogador, temporadaAtual))
        {
            throw new InvalidOperationException($"{jogador.Nome} ainda não está no último ano de contrato.");
        }

        jogador.AssinarContrato(new Contrato(clube.Id, Valores.SalarioPedido(jogador), temporadaAtual + anos));
        jogador.AjustarMoral(5);
    }

    /// <summary>Custo de dispensar: <see cref="SalariosDeRescisao"/> salários.</summary>
    public static decimal CustoDaDispensa(Jogador jogador) => (jogador.Contrato?.SalarioMensal ?? 0) * SalariosDeRescisao;

    /// <summary>Paga a rescisão, tira o jogador do elenco e o deixa como agente livre.</summary>
    public static void Dispensar(Mundo mundo, Clube clube, Jogador jogador)
    {
        ArgumentNullException.ThrowIfNull(mundo);
        ArgumentNullException.ThrowIfNull(clube);
        ArgumentNullException.ThrowIfNull(jogador);
        if (!clube.Elenco.Contains(jogador))
        {
            throw new InvalidOperationException($"{jogador.Nome} não é do {clube.Nome}.");
        }

        if (clube.Elenco.Count <= ElencoMinimoParaDispensar)
        {
            throw new InvalidOperationException($"O elenco precisa ter pelo menos {ElencoMinimoParaDispensar} jogadores.");
        }

        clube.Lancar(CategoriaFinanceira.Rescisoes, -CustoDaDispensa(jogador));
        Liberar(mundo, clube, jogador);
    }

    /// <summary>
    /// Fim de temporada: contratos que vencem. O clube do jogador perde quem ele não renovou;
    /// a IA renova quem é útil (jovem ou entre os 18 melhores) e libera os veteranos fracos.
    /// </summary>
    public static void ResolverVencimentos(Mundo mundo, int temporada, string? clubeDoJogador)
    {
        ArgumentNullException.ThrowIfNull(mundo);
        foreach (var clube in mundo.Clubes)
        {
            var vencendo = clube.Elenco.Where(j => j.Contrato?.VenceAoFimDe(temporada) == true).ToArray();
            if (vencendo.Length == 0)
            {
                continue;
            }

            if (clube.Id == clubeDoJogador)
            {
                foreach (var jogador in vencendo)
                {
                    Liberar(mundo, clube, jogador);
                }

                continue;
            }

            var melhores = clube.Elenco
                .OrderByDescending(j => j.ForcaGeral).ThenBy(j => j.Id)
                .Take(18).Select(j => j.Id).ToHashSet();
            foreach (var jogador in vencendo)
            {
                if (jogador.Idade <= 30 || (melhores.Contains(jogador.Id) && jogador.Idade <= 33))
                {
                    var anos = jogador.Idade switch { <= 25 => 3, <= 30 => 2, _ => 1 };
                    jogador.AssinarContrato(new Contrato(clube.Id, Valores.SalarioPedido(jogador), temporada + anos));
                }
                else
                {
                    Liberar(mundo, clube, jogador);
                }
            }
        }
    }

    /// <summary>Mantém só os agentes livres mais fortes (o resto some do jogo).</summary>
    public static void LimitarAgentesLivres(Mundo mundo)
    {
        ArgumentNullException.ThrowIfNull(mundo);
        foreach (var jogador in mundo.AgentesLivres
                     .OrderByDescending(j => j.ForcaGeral).ThenBy(j => j.Id)
                     .Skip(MaximoDeAgentesLivres).ToArray())
        {
            mundo.RemoverAgenteLivre(jogador.Id);
        }
    }

    private static void Liberar(Mundo mundo, Clube clube, Jogador jogador)
    {
        clube.RemoverDoElenco(jogador.Id);
        jogador.EncerrarContrato();
        mundo.AdicionarAgenteLivre(jogador);
    }
}
