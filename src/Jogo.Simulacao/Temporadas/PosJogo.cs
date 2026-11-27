using Jogo.Dominio.Clubes;
using Jogo.Dominio.Jogadores;
using Jogo.Motor;

namespace Jogo.Simulacao.Temporadas;

/// <summary>Aplica ao elenco o que aconteceu numa partida: físico, lesões, cartões, moral e entrosamento.</summary>
public static class PosJogo
{
    public static void Aplicar(Clube clube, TimeEscalado escalacao, ResultadoPartida resultado, Equipe equipe, RegrasTemporada regras)
    {
        ArgumentNullException.ThrowIfNull(clube);
        ArgumentNullException.ThrowIfNull(escalacao);
        ArgumentNullException.ThrowIfNull(resultado);
        ArgumentNullException.ThrowIfNull(regras);

        var participacoes = resultado.Participacoes.Where(p => p.Equipe == equipe).ToDictionary(p => p.JogadorId);
        var titulares = escalacao.Titulares.Select(j => j.Id).ToHashSet();
        var (pro, contra) = equipe == Equipe.Mandante
            ? (resultado.GolsMandante, resultado.GolsVisitante)
            : (resultado.GolsVisitante, resultado.GolsMandante);
        var moralDoResultado = pro > contra ? regras.MoralVitoria : pro == contra ? regras.MoralEmpate : regras.MoralDerrota;

        foreach (var jogador in clube.Elenco)
        {
            var jogou = participacoes.TryGetValue(jogador.Id, out var p);
            if (jogou)
            {
                jogador.AjustarCondicao(p!.CondicaoFinal - jogador.Condicao);

                if (p.RodadasDeLesao > 0)
                {
                    jogador.Lesionar(p.RodadasDeLesao);
                }

                if (p.Expulso)
                {
                    jogador.Suspender(regras.SuspensaoPorVermelho);
                }
                else if (p.Amarelos > 0)
                {
                    jogador.AdicionarAmarelos(p.Amarelos);
                    if (jogador.AmarelosAcumulados >= regras.LimiteAmarelos)
                    {
                        jogador.Suspender(1);
                        jogador.DescontarAmarelos(regras.LimiteAmarelos);
                    }
                }
            }

            var regressao = (int)Math.Round((50 - jogador.Moral) * regras.RegressaoMoral);
            jogador.AjustarMoral(moralDoResultado + (jogou ? regras.MoralJogou : regras.MoralNaoJogou) + regressao);
            jogador.RegistrarRodada(titulares.Contains(jogador.Id));
        }
    }

    /// <summary>
    /// Bônus de entrosamento: média, entre os titulares, de quantas rodadas seguidas cada um vem jogando
    /// (até o máximo das regras), aplicada sobre o bônus máximo.
    /// </summary>
    public static double BonusEntrosamento(IEnumerable<Jogador> titulares, RegrasTemporada regras)
    {
        ArgumentNullException.ThrowIfNull(titulares);
        ArgumentNullException.ThrowIfNull(regras);

        var media = titulares
            .Select(j => Math.Min(j.SequenciaComoTitular, regras.RodadasParaEntrosamentoMaximo) / (double)regras.RodadasParaEntrosamentoMaximo)
            .DefaultIfEmpty(0)
            .Average();
        return regras.EntrosamentoMaximo * media;
    }
}
