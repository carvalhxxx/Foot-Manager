using Jogo.Dominio.Jogadores;

namespace Jogo.Motor;

/// <summary>
/// Decisões automáticas durante o jogo, usadas pela IA dos outros clubes e no modo rápido.
/// Regras simples e previsíveis (sem sorteio): repõe lesionados, troca cansados no 2º tempo
/// e ajusta a mentalidade conforme o placar no fim do jogo.
/// </summary>
public static class TreinadorAutomatico
{
    private const double CondicaoParaTrocar = 62;
    private const int MinutoInicioTrocas = 60;

    public static IReadOnlyList<Decisao> Decidir(Partida partida, TimeEmJogo time)
    {
        ArgumentNullException.ThrowIfNull(partida);
        ArgumentNullException.ThrowIfNull(time);

        var decisoes = new List<Decisao>();
        var restantes = partida.Configuracao.MaximoSubstituicoes - time.SubstituicoesFeitas;
        var reservas = time.Reservas.ToList();

        // 1. Lesionado sai na hora.
        foreach (var (jogadorId, posicao) in time.VagasAbertas)
        {
            if (restantes == 0)
            {
                break;
            }

            if (MelhorReserva(reservas, posicao) is { } reserva)
            {
                decisoes.Add(new Substituicao(jogadorId, reserva.Id));
                reservas.Remove(reserva);
                restantes--;
            }
        }

        // 2. No 2º tempo, troca o mais cansado (uma troca por minuto) se o reserva render mais.
        if (restantes > 0 && partida.MinutosCorridos >= MinutoInicioTrocas)
        {
            var cansado = time.EmCampo
                .Where(j => j.Condicao < CondicaoParaTrocar)
                .OrderBy(j => j.Condicao)
                .FirstOrDefault();

            if (cansado is not null && MelhorReserva(reservas, cansado.Posicao) is { } reserva
                && ValorEsperado(reserva, cansado.Posicao, reserva.Condicao) > ValorEsperado(cansado.Jogador, cansado.Posicao, cansado.Condicao))
            {
                decisoes.Add(new Substituicao(cansado.Jogador.Id, reserva.Id));
            }
        }

        // 3. Mentalidade conforme o placar.
        var saldo = time.Estatisticas.Gols - partida.Adversario(time.Equipe).Estatisticas.Gols;
        var desejada = (partida.MinutosCorridos, saldo) switch
        {
            ( >= 80, <= -2) => Mentalidade.MuitoOfensiva,
            ( >= 70, < 0) => Mentalidade.Ofensiva,
            ( >= 80, 1) => Mentalidade.Defensiva,
            _ => time.Mentalidade,
        };

        if (desejada != time.Mentalidade)
        {
            decisoes.Add(new TrocaDeMentalidade(desejada));
        }

        return decisoes;
    }

    /// <summary>Melhor reserva para a posição (goleiro só entra no gol, e só goleiro entra no gol).</summary>
    public static Jogador? MelhorReserva(IEnumerable<Jogador> reservas, Posicao posicao) =>
        reservas
            .Where(r => (r.PosicaoPrincipal == Posicao.Goleiro) == (posicao == Posicao.Goleiro))
            .OrderByDescending(r => ValorEsperado(r, posicao, r.Condicao))
            .ThenBy(r => r.Id)
            .FirstOrDefault();

    private static double ValorEsperado(Jogador jogador, Posicao posicao, double condicao) =>
        jogador.ForcaNaPosicao(posicao) * Setores.FatorImproviso(jogador, posicao) * (0.75 + (0.25 * condicao / 100));
}
