using Jogo.Dominio.Aleatoriedade;
using Jogo.Dominio.Jogadores;

namespace Jogo.Simulacao.Geracao;

/// <summary>
/// Gera jogadores com atributos coerentes com a posição e com uma força geral alvo.
/// Usado na criação do mundo e, mais tarde, para revelações da base no fim da temporada.
/// </summary>
public sealed class GeradorDeJogadores(IGeradorAleatorio aleatorio)
{
    private static readonly TipoAtributo[] TodosAtributos = Enum.GetValues<TipoAtributo>();

    // Posições secundárias plausíveis para cada posição principal.
    private static readonly Dictionary<Posicao, Posicao[]> Secundarias = new()
    {
        [Posicao.Goleiro] = [],
        [Posicao.Zagueiro] = [Posicao.Volante],
        [Posicao.LateralDireito] = [Posicao.AlaDireito, Posicao.MeiaDireito, Posicao.Zagueiro],
        [Posicao.LateralEsquerdo] = [Posicao.AlaEsquerdo, Posicao.MeiaEsquerdo, Posicao.Zagueiro],
        [Posicao.AlaDireito] = [Posicao.LateralDireito, Posicao.MeiaDireito],
        [Posicao.AlaEsquerdo] = [Posicao.LateralEsquerdo, Posicao.MeiaEsquerdo],
        [Posicao.Volante] = [Posicao.MeiaCentral, Posicao.Zagueiro],
        [Posicao.MeiaCentral] = [Posicao.Volante, Posicao.MeiaOfensivo, Posicao.MeiaDireito, Posicao.MeiaEsquerdo],
        [Posicao.MeiaDireito] = [Posicao.PontaDireita, Posicao.AlaDireito, Posicao.MeiaCentral],
        [Posicao.MeiaEsquerdo] = [Posicao.PontaEsquerda, Posicao.AlaEsquerdo, Posicao.MeiaCentral],
        [Posicao.MeiaOfensivo] = [Posicao.MeiaCentral, Posicao.SegundoAtacante, Posicao.PontaDireita, Posicao.PontaEsquerda],
        [Posicao.PontaDireita] = [Posicao.PontaEsquerda, Posicao.MeiaDireito, Posicao.SegundoAtacante],
        [Posicao.PontaEsquerda] = [Posicao.PontaDireita, Posicao.MeiaEsquerdo, Posicao.SegundoAtacante],
        [Posicao.SegundoAtacante] = [Posicao.Centroavante, Posicao.MeiaOfensivo, Posicao.PontaDireita, Posicao.PontaEsquerda],
        [Posicao.Centroavante] = [Posicao.SegundoAtacante],
    };

    // Peso relativo de cada idade (17 a 36) num elenco profissional.
    private static readonly int[] PesoPorIdade = [2, 3, 4, 5, 6, 7, 8, 8, 8, 8, 8, 7, 6, 6, 5, 4, 3, 2, 2, 1];
    private const int IdadeInicial = 17;

    /// <param name="alvoExato">Se verdadeiro, ignora a variação aleatória por idade (usado para craques).</param>
    public Jogador Gerar(
        int id, string nome, Posicao posicao, int forcaAlvo, string clubeId, int temporada,
        int? idade = null, bool alvoExato = false)
    {
        var idadeFinal = idade ?? SortearIdade();
        var alvo = Math.Clamp(forcaAlvo + (alvoExato ? 0 : AjustePorIdade(idadeFinal)), 15, 95);
        var atributos = GerarAtributos(posicao, alvo);
        var forca = CalculadoraForca.Calcular(atributos, posicao);

        var jogador = new Jogador(id, nome, idadeFinal, posicao, SortearSecundarias(posicao), atributos,
            GerarOcultos(idadeFinal, forca));
        jogador.AssinarContrato(new Contrato(clubeId, Salario(forca), temporada + aleatorio.Proximo(0, 4)));
        return jogador;
    }

    /// <summary>
    /// Salário mensal em reais, crescendo de forma exponencial com a força
    /// (≈ R$ 6 mil com força 50, R$ 60 mil com 70, R$ 330 mil com 85), arredondado a R$ 500.
    /// </summary>
    public static decimal Salario(int forca)
    {
        var bruto = 2_000 * Math.Pow(1.12, forca - 40);
        return Math.Max(1_500m, Math.Round((decimal)bruto / 500m, MidpointRounding.AwayFromZero) * 500m);
    }

    /// <summary>Variação triangular em [−2a, 2a], concentrada perto de zero.</summary>
    public int Variacao(int amplitude) =>
        aleatorio.Proximo(-amplitude, amplitude + 1) + aleatorio.Proximo(-amplitude, amplitude + 1);

    private int SortearIdade()
    {
        var sorteio = aleatorio.Proximo(0, PesoPorIdade.Sum());
        for (var i = 0; i < PesoPorIdade.Length; i++)
        {
            sorteio -= PesoPorIdade[i];
            if (sorteio < 0)
            {
                return IdadeInicial + i;
            }
        }

        return IdadeInicial + PesoPorIdade.Length - 1;
    }

    private int AjustePorIdade(int idade) => Variacao(3) + idade switch
    {
        <= 20 => -7,
        <= 22 => -3,
        >= 33 => -3,
        _ => 0,
    };

    private Atributos GerarAtributos(Posicao posicao, int forcaAlvo)
    {
        // Média de atributo (1-20) equivalente à força alvo (1-99).
        var media = 1 + ((forcaAlvo - 1) * 19.0 / 98);
        var valores = new int[TodosAtributos.Length];

        foreach (var atributo in TodosAtributos)
        {
            var peso = CalculadoraForca.Peso(posicao, atributo);
            var ajuste = peso switch
            {
                >= 20 => 1.5,
                >= 10 => 0.5,
                > 0 => -0.5,
                // Goleiro é especialista: o que não usa no gol fica bem abaixo.
                _ when posicao == Posicao.Goleiro => -7.0,
                _ => -3.0,
            };
            valores[(int)atributo] = Math.Clamp((int)Math.Round(media + ajuste) + aleatorio.Proximo(-2, 3), 1, 20);
        }

        // Atributos que não fazem sentido para a função ficam bem baixos.
        if (posicao == Posicao.Goleiro)
        {
            valores[(int)TipoAtributo.Finalizacao] = aleatorio.Proximo(1, 6);
            valores[(int)TipoAtributo.Drible] = aleatorio.Proximo(1, 6);
        }
        else
        {
            valores[(int)TipoAtributo.Reflexos] = aleatorio.Proximo(1, 5);
            valores[(int)TipoAtributo.Posicionamento] = aleatorio.Proximo(1, 5);
        }

        // Ajuste fino: aproxima a força do alvo mexendo nos atributos que pesam na posição.
        var relevantes = TodosAtributos.Where(a => CalculadoraForca.Peso(posicao, a) > 0).ToArray();
        for (var passo = 0; passo < 60; passo++)
        {
            var forca = CalculadoraForca.Calcular(Montar(valores), posicao);
            if (Math.Abs(forca - forcaAlvo) <= 1)
            {
                break;
            }

            var indice = (int)relevantes[aleatorio.Proximo(0, relevantes.Length)];
            valores[indice] = Math.Clamp(valores[indice] + (forca < forcaAlvo ? 1 : -1), 1, 20);
        }

        return Montar(valores);
    }

    private AtributosOcultos GerarOcultos(int idade, int forca)
    {
        var crescimento = idade switch
        {
            <= 23 => ((24 - idade) * aleatorio.Proximo(1, 4)) + aleatorio.Proximo(0, 5),
            <= 27 => aleatorio.Proximo(0, 4),
            _ => 0,
        };

        return new AtributosOcultos
        {
            Potencial = Math.Clamp(forca + crescimento, forca, 99),
            PropensaoLesao = aleatorio.Proximo(1, 9) + aleatorio.Proximo(0, 8),
            Profissionalismo = aleatorio.Proximo(1, 11) + aleatorio.Proximo(0, 10),
        };
    }

    private Posicao[] SortearSecundarias(Posicao posicao)
    {
        var opcoes = Secundarias[posicao].ToList();
        var quantidade = aleatorio.Proximo(0, 100) switch
        {
            < 50 => 0,
            < 85 => 1,
            _ => 2,
        };

        var escolhidas = new List<Posicao>();
        while (escolhidas.Count < quantidade && opcoes.Count > 0)
        {
            var indice = aleatorio.Proximo(0, opcoes.Count);
            escolhidas.Add(opcoes[indice]);
            opcoes.RemoveAt(indice);
        }

        return [.. escolhidas];
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
