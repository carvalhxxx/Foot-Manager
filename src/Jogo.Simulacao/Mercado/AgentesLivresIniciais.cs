using Jogo.Dominio;
using Jogo.Dominio.Aleatoriedade;
using Jogo.Simulacao.Geracao;

namespace Jogo.Simulacao.Mercado;

/// <summary>
/// Agentes livres do começo da carreira: sem eles, o mercado de agentes livres ficaria vazio no primeiro ano.
/// São jogadores de nível de Série B, de todas as posições, a maioria já experiente.
/// </summary>
public static class AgentesLivresIniciais
{
    public const int Quantidade = 40;

    public static void Gerar(Mundo mundo, ulong seed)
    {
        ArgumentNullException.ThrowIfNull(mundo);
        var aleatorio = new GeradorAleatorio(seed ^ GeradorDeMundo.HashEstavel("agentes-livres-iniciais"));
        var gerador = new GeradorDeJogadores(aleatorio);
        var nomes = new GeradorDeNomes(aleatorio);
        var usados = mundo.Clubes.SelectMany(c => c.Elenco).Select(j => j.Nome).ToHashSet(StringComparer.Ordinal);
        var proximoId = mundo.MaiorIdDeJogador + 1;
        // Um clube qualquer só para o gerador assinar; o contrato é desfeito em seguida.
        var clubeId = mundo.Clubes[0].Id;

        for (var i = 0; i < Quantidade; i++)
        {
            var posicao = GeradorDeMundo.ElencoBase[i % GeradorDeMundo.ElencoBase.Length];
            var jogador = gerador.Gerar(proximoId++, nomes.Gerar(usados), posicao, aleatorio.Proximo(48, 68), clubeId,
                mundo.TemporadaAtual, idade: aleatorio.Proximo(21, 34));
            jogador.EncerrarContrato();
            mundo.AdicionarAgenteLivre(jogador);
        }
    }
}
