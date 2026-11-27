using Jogo.Dominio;
using Jogo.Dominio.Aleatoriedade;
using Jogo.Dominio.Clubes;
using Jogo.Dominio.Competicoes;
using Jogo.Dominio.Jogadores;
using Jogo.Dominio.Reais;

namespace Jogo.Simulacao.Geracao;

/// <summary>
/// Cria o mundo a partir de clubes e jogadores reais. A força de cada jogador sai do valor de mercado
/// (escala logarítmica, ajustada pela idade); sem valor, do nível do próprio elenco. Posições só por grupo
/// são distribuídas para o elenco ter laterais, volantes e pontas. Elencos incompletos são completados
/// com jogadores gerados até o elenco-base.
/// </summary>
public static class GeradorDeMundoReal
{
    public const int ElencoMaximo = 30;

    /// <summary>Vagas de acesso diretas nas divisões de baixo; as outras saem do playoff (D-035).</summary>
    public const int VagasDiretasNoAcesso = 2;

    /// <summary>Força para um valor de mercado: 100 mil € ≈ 50, 1 mi € ≈ 61, 10 mi € ≈ 73, 60 mi € ≈ 80.</summary>
    public static int ForcaPeloValor(decimal valorEmEuros, int idade)
    {
        var baseForca = 50 + (11.5 * Math.Log10((double)Math.Max(valorEmEuros, 20_000m) / 100_000));
        // Jovens valem pelo potencial; veteranos valem menos do que jogam.
        var ajuste = idade switch
        {
            <= 19 => -6,
            <= 21 => -3,
            >= 35 => 4,
            >= 32 => 2,
            _ => 0,
        };
        return (int)Math.Round(Math.Clamp(baseForca + ajuste, 40, 88));
    }

    public static Mundo Gerar(ulong seed, int temporada, IReadOnlyList<ClubeReal> clubes)
    {
        ArgumentNullException.ThrowIfNull(clubes);
        var lista = new List<Clube>();
        var proximoId = 1;
        foreach (var modelo in clubes)
        {
            var aleatorio = new GeradorAleatorio(seed ^ GeradorDeMundo.HashEstavel(modelo.Id));
            lista.Add(GerarClube(modelo, aleatorio, temporada, ref proximoId));
        }

        var ultima = clubes.Max(c => c.Divisao);
        var divisoes = clubes.GroupBy(c => c.Divisao).OrderBy(g => g.Key).Select(g =>
        {
            var letra = (char)('a' + g.Key - 1);
            return new Divisao($"serie-{letra}", $"Série {char.ToUpperInvariant(letra)}", g.Key, g.Select(c => c.Id),
                vagasAcesso: g.Key == 1 ? 0 : GeradorDeMundo.VagasAcessoERebaixamento,
                vagasRebaixamento: g.Key == ultima ? 0 : GeradorDeMundo.VagasAcessoERebaixamento,
                // Série B 2026 (D-035): 2 sobem direto; 3º a 6º disputam as outras 2 vagas em playoff de ida e volta.
                vagasDiretas: g.Key == 1 ? null : VagasDiretasNoAcesso);
        }).ToArray();

        return new Mundo(temporada, divisoes, lista);
    }

    private static Clube GerarClube(ClubeReal modelo, GeradorAleatorio aleatorio, int temporada, ref int proximoId)
    {
        var clube = new Clube(modelo.Id, modelo.Nome, modelo.Sigla, modelo.Cidade, modelo.Uf,
            new Cores(modelo.CorPrimaria, modelo.CorSecundaria), new Estadio(modelo.Estadio, modelo.Capacidade),
            modelo.Torcida, modelo.Reputacao, Caixa(modelo));

        // Nível do clube: média dos 15 mais valiosos (quando há valores), senão pela reputação.
        var comValor = modelo.Elenco.Where(j => j.ValorEmEuros is not null)
            .Select(j => ForcaPeloValor(j.ValorEmEuros!.Value, j.Idade ?? 26)).OrderByDescending(f => f).ToArray();
        var nivelPelaReputacao = 48 + (int)Math.Round(modelo.Reputacao * 0.25);
        var nivel = comValor.Length >= 8 ? (int)Math.Round(comValor.Take(15).Average()) : nivelPelaReputacao;

        // Quem não tem valor de mercado costuma ser reserva: fica no quartil de baixo do elenco conhecido.
        var nivelSemValor = comValor.Length >= 8 ? comValor[(int)(comValor.Length * 0.75)] - 1 : nivel - 3;

        var jogadores = new GeradorDeJogadores(aleatorio);
        var candidatos = Distribuir(modelo.Elenco)
            .Select(x =>
            {
                var idade = x.Jogador.Idade ?? aleatorio.Proximo(21, 32);
                var forca = x.Jogador.ValorEmEuros is { } valor
                    ? ForcaPeloValor(valor, idade)
                    : nivelSemValor + aleatorio.Proximo(-3, 4);
                return (x.Jogador.Nome, x.Posicao, Idade: idade, Forca: forca);
            })
            .ToList();

        // Elenco final: o melhor por posição do elenco-base primeiro, depois os mais fortes até 30.
        var escolhidos = new List<(string Nome, Posicao Posicao, int Idade, int Forca)>();
        var restantes = candidatos.OrderByDescending(c => c.Forca).ThenBy(c => c.Nome, StringComparer.Ordinal).ToList();
        var faltando = new List<Posicao>();
        foreach (var posicao in GeradorDeMundo.ElencoBase)
        {
            var indice = restantes.FindIndex(c => Compativel(c.Posicao, posicao));
            if (indice >= 0)
            {
                escolhidos.Add(restantes[indice]);
                restantes.RemoveAt(indice);
            }
            else
            {
                faltando.Add(posicao);
            }
        }

        escolhidos.AddRange(restantes.Take(Math.Max(0, ElencoMaximo - escolhidos.Count - faltando.Count)));

        var usados = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (nome, posicao, idade, forca) in escolhidos)
        {
            // Homônimos no mesmo clube ganham um número para continuar distinguíveis.
            var nomeFinal = usados.Add(nome) ? nome : $"{nome} II";
            usados.Add(nomeFinal);
            clube.AdicionarAoElenco(jogadores.Gerar(proximoId++, nomeFinal, posicao, forca, modelo.Id, temporada, idade, alvoExato: true));
        }

        // Vagas sem jogador conhecido: completadas com jogadores gerados (fictícios) no nível do clube.
        var nomes = new GeradorDeNomes(aleatorio);
        foreach (var posicao in faltando)
        {
            clube.AdicionarAoElenco(jogadores.Gerar(proximoId++, nomes.Gerar(usados), posicao, nivel - 4, modelo.Id, temporada));
        }

        return clube;
    }

    /// <summary>A vaga do elenco-base aceita a posição exata ou uma vizinha da mesma função.</summary>
    private static bool Compativel(Posicao jogador, Posicao vaga) =>
        jogador == vaga || (vaga, jogador) switch
        {
            (Posicao.LateralDireito, Posicao.AlaDireito) => true,
            (Posicao.LateralEsquerdo, Posicao.AlaEsquerdo) => true,
            (Posicao.MeiaDireito, Posicao.PontaDireita) => true,
            (Posicao.MeiaEsquerdo, Posicao.PontaEsquerda) => true,
            (Posicao.SegundoAtacante, Posicao.MeiaOfensivo or Posicao.Centroavante) => true,
            _ => false,
        };

    // Como distribuir quem só tem o grupo, em ordem (repetindo o ciclo se houver mais jogadores).
    private static readonly Posicao[] CicloDefensores =
        [Posicao.Zagueiro, Posicao.LateralDireito, Posicao.Zagueiro, Posicao.LateralEsquerdo, Posicao.Zagueiro, Posicao.LateralDireito, Posicao.LateralEsquerdo, Posicao.Zagueiro];

    private static readonly Posicao[] CicloMeias =
        [Posicao.Volante, Posicao.MeiaCentral, Posicao.MeiaOfensivo, Posicao.Volante, Posicao.MeiaCentral, Posicao.MeiaDireito, Posicao.MeiaEsquerdo, Posicao.MeiaCentral];

    private static readonly Posicao[] CicloAtacantes =
        [Posicao.Centroavante, Posicao.PontaDireita, Posicao.PontaEsquerda, Posicao.Centroavante, Posicao.SegundoAtacante, Posicao.PontaDireita, Posicao.PontaEsquerda];

    /// <summary>
    /// Dá uma posição exata a quem só tem o grupo, completando o que o elenco ainda não tem
    /// (os jogadores com posição conhecida contam primeiro).
    /// </summary>
    private static List<(JogadorReal Jogador, Posicao Posicao)> Distribuir(IReadOnlyList<JogadorReal> elenco)
    {
        var resultado = new List<(JogadorReal, Posicao)>();
        var contagem = elenco.Where(j => j.Posicao is not null).GroupBy(j => j.Posicao!.Value).ToDictionary(g => g.Key, g => g.Count());
        int Tem(Posicao p) => contagem.GetValueOrDefault(p);

        foreach (var jogador in elenco)
        {
            if (jogador.Posicao is { } exata)
            {
                resultado.Add((jogador, exata));
                continue;
            }

            Posicao[] ciclo = jogador.Grupo switch
            {
                GrupoDePosicao.Goleiro => [Posicao.Goleiro],
                GrupoDePosicao.Defensor => CicloDefensores,
                GrupoDePosicao.Meia => CicloMeias,
                _ => CicloAtacantes,
            };

            // A posição do ciclo de que o elenco mais precisa (proporção atual ÷ proporção no ciclo).
            var escolhida = ciclo.Distinct()
                .OrderBy(p => Tem(p) / (double)ciclo.Count(c => c == p))
                .ThenBy(p => Array.IndexOf(ciclo, p))
                .First();
            contagem[escolhida] = Tem(escolhida) + 1;
            resultado.Add((jogador, escolhida));
        }

        return resultado;
    }

    /// <summary>Caixa inicial pela reputação: de R$ 2 mi (pequenos) a cerca de R$ 80 mi (os maiores).</summary>
    private static decimal Caixa(ClubeReal modelo)
    {
        var milhoes = 2 + (78 * Math.Pow(Math.Max(0, modelo.Reputacao - 40) / 55.0, 2.2));
        return Math.Round((decimal)milhoes * 10m) * 100_000m;
    }
}
