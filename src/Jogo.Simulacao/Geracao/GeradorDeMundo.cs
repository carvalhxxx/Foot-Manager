using Jogo.Dominio;
using Jogo.Dominio.Aleatoriedade;
using Jogo.Dominio.Clubes;
using Jogo.Dominio.Competicoes;
using Jogo.Dominio.Jogadores;

namespace Jogo.Simulacao.Geracao;

/// <summary>
/// Cria o mundo inicial (Séries A e B) a partir dos moldes de clube e de uma seed.
/// Cada clube usa o seu próprio gerador aleatório, derivado da seed e do id do clube:
/// assim, mexer em um clube do catálogo não muda os elencos dos outros.
/// </summary>
public static class GeradorDeMundo
{
    public const int VagasAcessoERebaixamento = 4;

    /// <summary>Quanto um craque fica acima do nível técnico do clube.</summary>
    public const int BonusDeCraque = 15;

    /// <summary>Elenco base de 25 jogadores (3 goleiros, 8 defensores, 8 meio-campistas, 6 atacantes).</summary>
    internal static readonly Posicao[] ElencoBase =
    [
        Posicao.Goleiro, Posicao.Goleiro, Posicao.Goleiro,
        Posicao.Zagueiro, Posicao.Zagueiro, Posicao.Zagueiro, Posicao.Zagueiro,
        Posicao.LateralDireito, Posicao.LateralDireito, Posicao.LateralEsquerdo, Posicao.LateralEsquerdo,
        Posicao.Volante, Posicao.Volante, Posicao.MeiaCentral, Posicao.MeiaCentral, Posicao.MeiaCentral,
        Posicao.MeiaOfensivo, Posicao.MeiaDireito, Posicao.MeiaEsquerdo,
        Posicao.PontaDireita, Posicao.PontaEsquerda, Posicao.SegundoAtacante,
        Posicao.Centroavante, Posicao.Centroavante, Posicao.Centroavante,
    ];

    /// <summary>Posições sorteadas para completar o elenco até 30.</summary>
    private static readonly Posicao[] Extras =
    [
        Posicao.Zagueiro, Posicao.Volante, Posicao.MeiaCentral, Posicao.MeiaOfensivo, Posicao.PontaDireita,
        Posicao.PontaEsquerda, Posicao.Centroavante, Posicao.LateralDireito, Posicao.LateralEsquerdo,
    ];

    public static Mundo Gerar(ulong seed, int temporada, IReadOnlyList<ModeloClube> modelos)
    {
        ArgumentNullException.ThrowIfNull(modelos);

        var clubes = new List<Clube>();
        var proximoId = 1;

        foreach (var modelo in modelos)
        {
            var aleatorio = new GeradorAleatorio(seed ^ HashEstavel(modelo.Id));
            var clube = GerarClube(modelo, aleatorio, temporada, ref proximoId);
            clubes.Add(clube);
        }

        var divisoes = modelos
            .GroupBy(m => m.Divisao)
            .OrderBy(g => g.Key)
            .Select(g => CriarDivisao(g.Key, g.Select(m => m.Id), ultimaDivisao: g.Key == modelos.Max(m => m.Divisao)))
            .ToArray();

        return new Mundo(temporada, divisoes, clubes);
    }

    private static Divisao CriarDivisao(int nivel, IEnumerable<string> clubes, bool ultimaDivisao)
    {
        var letra = (char)('a' + nivel - 1);
        return new Divisao(
            $"serie-{letra}",
            $"Série {char.ToUpperInvariant(letra)}",
            nivel,
            clubes,
            vagasAcesso: nivel == 1 ? 0 : VagasAcessoERebaixamento,
            vagasRebaixamento: ultimaDivisao ? 0 : VagasAcessoERebaixamento);
    }

    private static Clube GerarClube(ModeloClube modelo, GeradorAleatorio aleatorio, int temporada, ref int proximoId)
    {
        var perfil = PerfilDoPorte(modelo.Porte, modelo.Divisao);
        var nomes = new GeradorDeNomes(aleatorio);
        var jogadores = new GeradorDeJogadores(aleatorio);

        var clube = new Clube(
            modelo.Id, modelo.Nome, modelo.Sigla, modelo.Cidade, modelo.Uf,
            new Cores(modelo.CorPrimaria, modelo.CorSecundaria),
            new Estadio($"Estádio {nomes.NomeDePessoa()}", Arredondar(Entre(aleatorio, perfil.Capacidade), 500)),
            Entre(aleatorio, perfil.Torcida),
            Entre(aleatorio, perfil.Reputacao),
            Arredondar(Entre(aleatorio, perfil.CaixaEmMilhares), 100) * 1_000m);

        // Cada clube tem um nível técnico próprio dentro da faixa do porte.
        var forcaDoClube = perfil.Forca + aleatorio.Proximo(-3, 4);

        var posicoes = ElencoBase.ToList();
        var extras = aleatorio.Proximo(0, 6);
        for (var i = 0; i < extras; i++)
        {
            posicoes.Add(Extras[aleatorio.Proximo(0, Extras.Length)]);
        }

        // Craques: jogadores bem acima do nível do elenco, no auge da carreira (candidatos a "estrela").
        var quantidadeDeCraques = (modelo.Porte, modelo.Divisao) switch
        {
            (Porte.Grande, _) => 2,
            (Porte.Medio, 1) => aleatorio.Proximo(0, 2),
            _ => 0,
        };
        var craques = new HashSet<int>();
        while (craques.Count < quantidadeDeCraques)
        {
            var indice = aleatorio.Proximo(0, posicoes.Count);
            if (posicoes[indice] != Posicao.Goleiro)
            {
                craques.Add(indice);
            }
        }

        var nomesUsados = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < posicoes.Count; i++)
        {
            var nome = nomes.Gerar(nomesUsados);
            var jogador = craques.Contains(i)
                ? jogadores.Gerar(proximoId++, nome, posicoes[i], forcaDoClube + BonusDeCraque, modelo.Id, temporada,
                    idade: aleatorio.Proximo(24, 32), alvoExato: true)
                : jogadores.Gerar(proximoId++, nome, posicoes[i], forcaDoClube, modelo.Id, temporada);
            clube.AdicionarAoElenco(jogador);
        }

        return clube;
    }

    private sealed record Perfil(int Forca, (int Min, int Max) Torcida, (int Min, int Max) Reputacao,
        (int Min, int Max) CaixaEmMilhares, (int Min, int Max) Capacidade);

    private static Perfil PerfilDoPorte(Porte porte, int divisao) => (porte, divisao) switch
    {
        (Porte.Grande, _) => new(71, (85, 95), (80, 92), (200_000, 400_000), (45_000, 75_000)),
        (Porte.Medio, 1) => new(66, (50, 72), (55, 72), (60_000, 150_000), (22_000, 45_000)),
        (Porte.Medio, _) => new(61, (40, 60), (42, 58), (30_000, 75_000), (15_000, 35_000)),
        _ => new(56, (18, 40), (28, 45), (10_000, 40_000), (8_000, 20_000)),
    };

    private static int Entre(GeradorAleatorio aleatorio, (int Min, int Max) faixa) =>
        aleatorio.Proximo(faixa.Min, faixa.Max + 1);

    private static int Arredondar(int valor, int multiplo) => (valor + (multiplo / 2)) / multiplo * multiplo;

    /// <summary>
    /// Hash FNV-1a de 64 bits. Diferente de <see cref="string.GetHashCode()"/>, dá o mesmo valor
    /// em toda execução e em toda máquina.
    /// </summary>
    internal static ulong HashEstavel(string texto)
    {
        var hash = 14695981039346656037UL;
        foreach (var c in texto)
        {
            hash ^= c;
            hash *= 1099511628211UL;
        }

        return hash;
    }
}
