using Jogo.Dominio.Clubes;
using Jogo.Motor;

namespace Jogo.Simulacao.Temporadas;

/// <summary>Um jogo da rodada sendo disputado.</summary>
public sealed record JogoEmAndamento(
    CompeticaoDaTemporada Competicao,
    JogoAgendado Jogo,
    Clube Mandante,
    Clube Visitante,
    TimeEscalado EscalacaoMandante,
    TimeEscalado EscalacaoVisitante,
    Partida Partida);

/// <summary>
/// Uma rodada jogada minuto a minuto: todos os jogos andam juntos, como num "placar da rodada" ao vivo.
/// Cada jogo tem o seu próprio gerador aleatório, então o resultado de um não depende do ritmo dos outros.
/// </summary>
public sealed class RodadaEmAndamento
{
    internal RodadaEmAndamento(int rodada, IReadOnlyList<JogoEmAndamento> jogos)
    {
        Rodada = rodada;
        Jogos = jogos;
    }

    public int Rodada { get; }
    public IReadOnlyList<JogoEmAndamento> Jogos { get; }
    public bool Encerrada => Jogos.All(j => j.Partida.Encerrada);

    /// <summary>Jogo de um clube nesta rodada (nulo se ele não joga).</summary>
    public JogoEmAndamento? JogoDo(string clubeId) =>
        Jogos.FirstOrDefault(j => j.Mandante.Id == clubeId || j.Visitante.Id == clubeId);

    /// <summary>Um minuto em todos os jogos que ainda não terminaram.</summary>
    public void AvancarMinuto()
    {
        foreach (var jogo in Jogos)
        {
            MotorDePartida.AvancarMinuto(jogo.Partida);
        }
    }

    public void AvancarAteOFim()
    {
        while (!Encerrada)
        {
            AvancarMinuto();
        }
    }
}
