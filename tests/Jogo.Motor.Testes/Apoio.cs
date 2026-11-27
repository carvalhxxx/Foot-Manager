using Jogo.Dominio.Aleatoriedade;
using Jogo.Dominio.Clubes;
using Jogo.Dominio.Jogadores;
using Jogo.Simulacao.Geracao;
using Jogo.Simulacao.Ia;

namespace Jogo.Motor.Testes;

internal static class Apoio
{
    /// <summary>
    /// Clube com elenco gerado. Mesma <paramref name="seed"/> = mesmos atributos (ids diferentes por
    /// <paramref name="primeiroId"/>), o que permite montar dois times idênticos.
    /// </summary>
    public static Clube Clube(string id, int forca, ulong seed, int primeiroId)
    {
        var clube = new Clube(id, id, "ABC", "Recife", "PE", new Cores("#000000", "#FFFFFF"),
            new Estadio("Estádio", 30_000), 50, 50, 0m);
        var gerador = new GeradorDeJogadores(new GeradorAleatorio(seed));
        Posicao[] posicoes =
        [
            Posicao.Goleiro, Posicao.Goleiro, Posicao.Zagueiro, Posicao.Zagueiro, Posicao.Zagueiro, Posicao.Zagueiro,
            Posicao.LateralDireito, Posicao.LateralDireito, Posicao.LateralEsquerdo, Posicao.LateralEsquerdo,
            Posicao.Volante, Posicao.Volante, Posicao.MeiaCentral, Posicao.MeiaCentral, Posicao.MeiaCentral,
            Posicao.MeiaDireito, Posicao.MeiaEsquerdo, Posicao.MeiaOfensivo,
            Posicao.PontaDireita, Posicao.PontaEsquerda, Posicao.Centroavante, Posicao.Centroavante,
        ];

        for (var i = 0; i < posicoes.Length; i++)
        {
            clube.AdicionarAoElenco(gerador.Gerar(primeiroId + i, $"J{primeiroId + i}", posicoes[i], forca, id, 2027, idade: 26, alvoExato: true));
        }

        return clube;
    }

    public static (TimeEscalado Mandante, TimeEscalado Visitante) DoisTimes(int forcaMandante = 70, int forcaVisitante = 70)
    {
        var mandante = Escalador.EscalarNaFormacao(Clube("casa", forcaMandante, 11, 1), FormacoesPadrao.QuatroQuatroDois);
        var visitante = Escalador.EscalarNaFormacao(Clube("fora", forcaVisitante, 22, 101), FormacoesPadrao.QuatroQuatroDois);
        return (mandante, visitante);
    }

    public static Partida Iniciar(ulong seed = 1, ConfiguracaoMotor? cfg = null, bool automatico = true)
    {
        var (m, v) = DoisTimes();
        return MotorDePartida.Iniciar(m, v, new GeradorAleatorio(seed), configuracao: cfg,
            mandanteAutomatico: automatico, visitanteAutomatico: automatico);
    }
}
