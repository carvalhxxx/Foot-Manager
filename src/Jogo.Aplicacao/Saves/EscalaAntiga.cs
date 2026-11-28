using Jogo.Simulacao.Geracao;

namespace Jogo.Aplicacao.Saves;

/// <summary>
/// Converte o dinheiro de saves feitos antes da escala real (versão 2 ou menor, D-041).
/// Cada salário vira o da nova curva para a mesma força (a força sai do próprio salário antigo).
/// Caixa, extrato e última renda de cada clube são multiplicados por quanto a folha dele cresceu,
/// para o clube continuar podendo pagar o elenco. O preço do ingresso escolhido acompanha o sugerido.
/// </summary>
internal static class EscalaAntiga
{
    /// <summary>Primeira versão do save com a escala real.</summary>
    public const int VersaoDaEscalaReal = 3;

    public static void Converter(SaveJson save)
    {
        foreach (var clube in save.Clubes ?? [])
        {
            var elenco = clube.Elenco ?? [];
            var folhaAntiga = elenco.Sum(j => j.Salario);
            foreach (var jogador in elenco)
            {
                jogador.Salario = SalarioNovo(jogador.Salario);
            }

            var folhaNova = elenco.Sum(j => j.Salario);
            var fator = folhaAntiga > 0 ? folhaNova / folhaAntiga : 1m;
            clube.Caixa = Math.Round(clube.Caixa * fator);
            if (clube.Extrato is { } extrato)
            {
                foreach (var categoria in extrato.Keys.ToArray())
                {
                    extrato[categoria] = Math.Round(extrato[categoria] * fator);
                }
            }

            if (clube.UltimaRenda is { } renda)
            {
                clube.UltimaRenda = Math.Round(renda * fator);
            }

            if (clube.PrecoIngresso is { } preco)
            {
                var sugeridoAntigo = Math.Max(5m, Math.Round(clube.Reputacao * 0.35m));
                var sugeridoNovo = Math.Max(5m, Math.Round(-30m + (clube.Reputacao * 1.27m)));
                clube.PrecoIngresso = Math.Clamp(Math.Round(preco * sugeridoNovo / sugeridoAntigo), 1m, 1_000m);
            }
        }

        foreach (var livre in save.AgentesLivres ?? [])
        {
            livre.Salario = SalarioNovo(livre.Salario);
        }
    }

    /// <summary>Salário na nova escala para a força que pagava <paramref name="antigo"/> na escala antiga.</summary>
    public static decimal SalarioNovo(decimal antigo)
    {
        if (antigo <= 0)
        {
            return antigo;
        }

        // Escala antiga: 2.000 × 1,12^(força − 40).
        var forca = (int)Math.Round(40 + (Math.Log((double)antigo / 2_000) / Math.Log(1.12)));
        return GeradorDeJogadores.Salario(Math.Clamp(forca, 1, 99));
    }
}
