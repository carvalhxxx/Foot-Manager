using Jogo.Dominio.Jogadores;
using Jogo.Simulacao.Geracao;

namespace Jogo.Simulacao.Mercado;

/// <summary>Quanto um jogador vale e quanto ele pede de salário.</summary>
public static class Valores
{
    /// <summary>Menor valor de mercado (jogadores fracos ou veteranos).</summary>
    public const decimal ValorMinimo = 50_000m;

    /// <summary>
    /// Valor de mercado: cresce exponencialmente com a força (≈ R$ 1,7 mi com força 55, R$ 33 mi com 70,
    /// R$ 300 mi com 85), sobe para jovens com potencial, cai com a idade e quando o contrato está no fim.
    /// </summary>
    public static decimal ValorDeMercado(Jogador jogador, int temporadaAtual)
    {
        ArgumentNullException.ThrowIfNull(jogador);

        var forca = jogador.ForcaGeral;
        // Inverso da força tirada do valor real (GeradorDeMundoReal): € 100 mil = força 50, ×10 a cada 11,5 pontos;
        // em reais (€ 1 ≈ R$ 6).
        var baseValor = 600_000.0 * Math.Pow(10, (forca - 50) / 11.5);
        var margem = Math.Max(0, jogador.Ocultos.Potencial - forca);
        var idade = jogador.Idade switch
        {
            <= 21 => 1.4 + (0.03 * margem),
            <= 27 => 1.0 + (0.015 * margem),
            <= 30 => 0.85,
            <= 32 => 0.6,
            _ => 0.35,
        };
        var anosDeContrato = (jogador.Contrato?.TemporadaFinal ?? temporadaAtual) - temporadaAtual;
        var contrato = anosDeContrato switch
        {
            <= 0 => 0.5,
            1 => 0.8,
            _ => 1.0,
        };

        var valor = (decimal)(baseValor * idade * contrato);
        return Math.Max(ValorMinimo, Math.Round(valor / 50_000m) * 50_000m);
    }

    /// <summary>
    /// Salário que o jogador pede para assinar (renovação ou contratação): o de mercado para a força dele,
    /// um pouco mais se estiver insatisfeito, e nunca muito abaixo do que já ganha.
    /// </summary>
    public static decimal SalarioPedido(Jogador jogador)
    {
        ArgumentNullException.ThrowIfNull(jogador);

        var mercado = GeradorDeJogadores.Salario(jogador.ForcaGeral);
        var humor = 1.1m - ((jogador.Moral - 50) / 500m);
        var pedido = mercado * humor;
        var atual = jogador.Contrato?.SalarioMensal ?? 0;
        return Math.Max(GeradorDeJogadores.SalarioMinimo, Math.Round(Math.Max(pedido, atual * 0.9m) / 500m) * 500m);
    }
}
