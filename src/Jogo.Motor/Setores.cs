using Jogo.Dominio.Jogadores;

namespace Jogo.Motor;

/// <summary>Força de um time em cada setor, já com todos os modificadores.</summary>
public readonly record struct ForcaSetores(double Gol, double Defesa, double Meio, double Ataque);

/// <summary>Regras de quanto cada posição contribui para cada setor e de rendimento fora da posição.</summary>
public static class Setores
{
    // Contribuição de cada posição para (Defesa, Meio, Ataque). O goleiro conta só no Gol.
    private static readonly Dictionary<Posicao, (double Defesa, double Meio, double Ataque)> Contribuicoes = new()
    {
        [Posicao.Zagueiro] = (1.0, 0.0, 0.0),
        [Posicao.LateralDireito] = (0.7, 0.3, 0.0),
        [Posicao.LateralEsquerdo] = (0.7, 0.3, 0.0),
        [Posicao.AlaDireito] = (0.4, 0.6, 0.0),
        [Posicao.AlaEsquerdo] = (0.4, 0.6, 0.0),
        [Posicao.Volante] = (0.3, 0.7, 0.0),
        [Posicao.MeiaCentral] = (0.0, 1.0, 0.0),
        [Posicao.MeiaDireito] = (0.0, 0.8, 0.2),
        [Posicao.MeiaEsquerdo] = (0.0, 0.8, 0.2),
        [Posicao.MeiaOfensivo] = (0.0, 0.5, 0.5),
        [Posicao.PontaDireita] = (0.0, 0.2, 0.8),
        [Posicao.PontaEsquerda] = (0.0, 0.2, 0.8),
        [Posicao.SegundoAtacante] = (0.0, 0.2, 0.8),
        [Posicao.Centroavante] = (0.0, 0.0, 1.0),
    };

    // Ocupação de referência = a do 4-4-2: nessa formação o fator de ocupação é exatamente 1.
    private const double OcupacaoReferenciaDefesa = 3.4;
    private const double OcupacaoReferenciaMeio = 4.2;
    private const double OcupacaoReferenciaAtaque = 2.4;

    public static (double Defesa, double Meio, double Ataque) Contribuicao(Posicao posicao) =>
        posicao == Posicao.Goleiro ? (0, 0, 0) : Contribuicoes[posicao];

    /// <summary>
    /// Rendimento de um jogador numa posição: 100% na principal e nas secundárias, um pouco menos no lado
    /// oposto da mesma função, menos no mesmo setor e bem menos longe dele.
    /// (A força já cai pelos pesos dos atributos; isto é a "falta de costume".)
    /// </summary>
    public static double FatorImproviso(Jogador jogador, Posicao posicao)
    {
        ArgumentNullException.ThrowIfNull(jogador);

        if (jogador.JogaNaPosicao(posicao))
        {
            return 1.0;
        }

        var principal = jogador.PosicaoPrincipal;
        if (principal.Funcao() == posicao.Funcao())
        {
            return 0.93;
        }

        if (principal == Posicao.Goleiro || posicao == Posicao.Goleiro)
        {
            return 0.5;
        }

        return principal.Setor() == posicao.Setor() ? 0.85 : 0.7;
    }

    /// <summary>
    /// Calcula a força dos 4 setores a partir de (posição, valor individual) de quem está em campo.
    /// Força do setor = qualidade média × (ocupação / ocupação do 4-4-2) ^ expoente.
    /// </summary>
    public static ForcaSetores Calcular(IEnumerable<(Posicao Posicao, double Valor)> jogadores, double expoenteOcupacao)
    {
        ArgumentNullException.ThrowIfNull(jogadores);

        double gol = 0, somaDef = 0, somaMeio = 0, somaAtq = 0, ocDef = 0, ocMeio = 0, ocAtq = 0;
        foreach (var (posicao, valor) in jogadores)
        {
            if (posicao == Posicao.Goleiro)
            {
                gol = Math.Max(gol, valor);
                continue;
            }

            var (d, m, a) = Contribuicoes[posicao];
            somaDef += d * valor;
            somaMeio += m * valor;
            somaAtq += a * valor;
            ocDef += d;
            ocMeio += m;
            ocAtq += a;
        }

        return new ForcaSetores(
            gol,
            Setor(somaDef, ocDef, OcupacaoReferenciaDefesa, expoenteOcupacao),
            Setor(somaMeio, ocMeio, OcupacaoReferenciaMeio, expoenteOcupacao),
            Setor(somaAtq, ocAtq, OcupacaoReferenciaAtaque, expoenteOcupacao));
    }

    private static double Setor(double soma, double ocupacao, double referencia, double expoente) =>
        ocupacao <= 0 ? 0 : soma / ocupacao * Math.Pow(ocupacao / referencia, expoente);
}
