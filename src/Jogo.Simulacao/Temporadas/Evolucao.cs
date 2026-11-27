using Jogo.Dominio.Aleatoriedade;
using Jogo.Dominio.Jogadores;

namespace Jogo.Simulacao.Temporadas;

/// <summary>
/// Evolução anual dos jogadores: jovens sobem (até o potencial), auge entre ~24 e ~30, declínio depois.
/// O profissionalismo acelera a evolução e freia o declínio.
/// </summary>
public static class Evolucao
{
    private static readonly TipoAtributo[] TodosAtributos = Enum.GetValues<TipoAtributo>();

    /// <summary>Variação de força geral sorteada para um jogador nesta virada de temporada.</summary>
    public static int SortearVariacao(Jogador jogador, IGeradorAleatorio aleatorio)
    {
        ArgumentNullException.ThrowIfNull(jogador);
        ArgumentNullException.ThrowIfNull(aleatorio);

        var (minimo, maximo) = jogador.Idade switch
        {
            <= 20 => (2, 5),
            <= 23 => (1, 4),
            <= 27 => (0, 2),
            <= 30 => (-1, 1),
            <= 32 => (-2, 0),
            <= 34 => (-3, -1),
            _ => (-4, -2),
        };

        var bruta = aleatorio.Proximo(minimo, maximo + 1);
        var profissionalismo = jogador.Ocultos.Profissionalismo / 20.0;
        var ajustada = bruta > 0
            ? bruta * (0.5 + profissionalismo)
            : bruta * (1.5 - profissionalismo);
        return (int)Math.Round(ajustada, MidpointRounding.AwayFromZero);
    }

    /// <summary>Aplica a variação mexendo nos atributos que pesam na posição. Crescimento nunca passa do potencial.</summary>
    public static void Aplicar(Jogador jogador, int variacao, IGeradorAleatorio aleatorio)
    {
        ArgumentNullException.ThrowIfNull(jogador);
        ArgumentNullException.ThrowIfNull(aleatorio);

        var atual = jogador.ForcaGeral;
        var alvo = variacao > 0
            ? Math.Min(atual + variacao, Math.Max(atual, jogador.Ocultos.Potencial))
            : Math.Max(1, atual + variacao);
        if (alvo == atual)
        {
            return;
        }

        var valores = TodosAtributos.Select(a => jogador.Atributos[a]).ToArray();
        var relevantes = TodosAtributos
            .Where(a => CalculadoraForca.Peso(jogador.PosicaoPrincipal, a) > 0)
            .SelectMany(a => Enumerable.Repeat(a, CalculadoraForca.Peso(jogador.PosicaoPrincipal, a)))
            .ToArray();
        var passo = alvo > atual ? 1 : -1;

        for (var tentativa = 0; tentativa < 200; tentativa++)
        {
            var forca = CalculadoraForca.Calcular(Montar(valores), jogador.PosicaoPrincipal);
            if ((passo > 0 && forca >= alvo) || (passo < 0 && forca <= alvo))
            {
                break;
            }

            // Atributos que mais pesam na posição mudam com mais frequência.
            var indice = (int)relevantes[aleatorio.Proximo(0, relevantes.Length)];
            valores[indice] = Math.Clamp(valores[indice] + passo, Atributos.Minimo, Atributos.Maximo);
        }

        jogador.AtualizarAtributos(Montar(valores));
    }

    private static Atributos Montar(int[] v) => new()
    {
        Finalizacao = v[(int)TipoAtributo.Finalizacao],
        Passe = v[(int)TipoAtributo.Passe],
        Desarme = v[(int)TipoAtributo.Desarme],
        Drible = v[(int)TipoAtributo.Drible],
        Velocidade = v[(int)TipoAtributo.Velocidade],
        Resistencia = v[(int)TipoAtributo.Resistencia],
        Decisao = v[(int)TipoAtributo.Decisao],
        Determinacao = v[(int)TipoAtributo.Determinacao],
        Reflexos = v[(int)TipoAtributo.Reflexos],
        Posicionamento = v[(int)TipoAtributo.Posicionamento],
    };
}
