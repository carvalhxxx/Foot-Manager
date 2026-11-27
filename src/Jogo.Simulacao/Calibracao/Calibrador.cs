using Jogo.Dominio;
using Jogo.Dominio.Aleatoriedade;
using Jogo.Dominio.Clubes;
using Jogo.Dominio.Jogadores;
using Jogo.Motor;
using Jogo.Simulacao.Ia;
using Jogo.Simulacao.Temporadas;

namespace Jogo.Simulacao.Calibracao;

/// <summary>Uma faixa aceitável para uma métrica. Faixa nula = métrica só informativa.</summary>
public sealed record Meta(string Nome, double Valor, double? Minimo, double? Maximo, string Formato = "F2")
{
    public bool Atingida => (Minimo is null || Valor >= Minimo) && (Maximo is null || Valor <= Maximo);
}

/// <summary>Resultado de uma rodada de calibração.</summary>
public sealed record RelatorioCalibracao(IReadOnlyList<Meta> Liga, IReadOnlyList<Meta> Formacoes)
{
    public bool Aprovado => Liga.Concat(Formacoes).All(m => m.Atingida);
}

/// <summary>Faixas-alvo da calibração (docs/DECISOES.md, D-020).</summary>
public sealed record MetasCalibracao
{
    public static MetasCalibracao Padrao { get; } = new();

    public (double Min, double Max) GolsPorJogo { get; init; } = (2.3, 2.7);
    public (double Min, double Max) VitoriasMandante { get; init; } = (0.42, 0.48);
    public (double Min, double Max) Empates { get; init; } = (0.24, 0.29);

    /// <summary>Vitórias do favorito (6+ pontos de força a mais no top 11): alta, mas sem ser certa.</summary>
    public (double Min, double Max) FavoritoVence { get; init; } = (0.55, 0.80);

    /// <summary>Fatia dos pontos de uma formação padrão nos confrontos contra o 4-4-2 (50% = equilíbrio).</summary>
    public (double Min, double Max) FormacaoPadrao { get; init; } = (0.40, 0.60);

    /// <summary>Fatia máxima dos pontos de uma formação absurda (5-0-5, 3-0-7) contra o 4-4-2.</summary>
    public double FormacaoExtremaMaximo { get; init; } = 0.40;

    public int DiferencaFavorito { get; init; } = 6;

    // Temporada (Série A, pontos corridos) ------------------------------------

    /// <summary>Quantas vezes o clube mais forte (top 11 no início) é campeão: alta, mas não absoluta.</summary>
    public (double Min, double Max) MaisForteCampeao { get; init; } = (0.25, 0.75);

    /// <summary>Fração mínima de títulos ficando com um dos 4 mais fortes.</summary>
    public double CampeaoEntreOsQuatroMaisFortes { get; init; } = 0.70;

    public (double Min, double Max) PontosDoCampeao { get; init; } = (68, 88);
    public (double Min, double Max) PontosDoLanterna { get; init; } = (18, 40);

    // Finanças (uma temporada sem mercado) --------------------------------------

    /// <summary>Resultado médio (receitas − despesas, em milhões) dos clubes de reputação ≥ 80.</summary>
    public (double Min, double Max) ResultadoDosGrandes { get; init; } = (0, 15);

    /// <summary>Resultado médio (em milhões) dos clubes de reputação &lt; 46: apertado, perto do zero.</summary>
    public (double Min, double Max) ResultadoDosPequenos { get; init; } = (-2, 3);

    /// <summary>Fração máxima de clubes com caixa negativo no fim da temporada.</summary>
    public double ClubesNoVermelhoMaximo { get; init; } = 0.10;

    /// <summary>Fatia dos pontos de cada opção de marcação/concentração contra o padrão (D-032).</summary>
    public (double Min, double Max) EstiloPadrao { get; init; } = (0.45, 0.55);

    // Carreiras de vários anos (mercado ligado) ----------------------------------

    /// <summary>Em 5 anos, quantos campeões diferentes da Série A (nem dinastia, nem loteria).</summary>
    public (double Min, double Max) CampeoesDiferentes { get; init; } = (2, 4.5);

    public double TitulosDoMaiorCampeaoMaximo { get; init; } = 3.5;
    public (double Min, double Max) TransferenciasPorAno { get; init; } = (15, 80);

    /// <summary>Fatia do dinheiro (positivo) que fica com os 4 clubes mais ricos.</summary>
    public double ConcentracaoDeCaixaMaxima { get; init; } = 0.6;

    public int ElencoMinimo { get; init; } = 18;
    public int ElencoMaximo { get; init; } = 35;
}

/// <summary>
/// Simula milhares de partidas e mede se o motor se comporta como futebol de verdade.
/// Puro e determinístico: mesma seed e mesmo mundo = mesmo relatório.
/// </summary>
public static class Calibrador
{
    public static Formacao CincoZeroCinco { get; } = new("5-0-5",
    [
        Posicao.LateralDireito, Posicao.Zagueiro, Posicao.Zagueiro, Posicao.Zagueiro, Posicao.LateralEsquerdo,
        Posicao.PontaDireita, Posicao.SegundoAtacante, Posicao.Centroavante, Posicao.Centroavante, Posicao.PontaEsquerda,
    ]);

    public static Formacao TresZeroSete { get; } = new("3-0-7",
    [
        Posicao.Zagueiro, Posicao.Zagueiro, Posicao.Zagueiro,
        Posicao.PontaDireita, Posicao.SegundoAtacante, Posicao.SegundoAtacante, Posicao.Centroavante,
        Posicao.Centroavante, Posicao.Centroavante, Posicao.PontaEsquerda,
    ]);

    public static RelatorioCalibracao Executar(
        Mundo mundo,
        int partidasLiga,
        int partidasPorFormacao,
        ulong seed,
        MetasCalibracao? metas = null,
        ConfiguracaoMotor? configuracao = null)
    {
        ArgumentNullException.ThrowIfNull(mundo);
        metas ??= MetasCalibracao.Padrao;
        configuracao ??= ConfiguracaoMotor.Padrao;

        return new RelatorioCalibracao(
            Liga(mundo, partidasLiga, seed, metas, configuracao),
            [.. Formacoes(mundo, partidasPorFormacao, seed ^ 0x5EED, metas, configuracao),
             .. Estilos(mundo, partidasPorFormacao, seed ^ 0xE57110, metas, configuracao)]);
    }

    /// <summary>
    /// Simula temporadas completas, cada uma a partir de um mundo novo (sem acesso/rebaixamento entre elas),
    /// e mede a Série A: quem é campeão e com quantos pontos.
    /// </summary>
    public static IReadOnlyList<Meta> Temporadas(Func<Mundo> criarMundo, int temporadas, ulong seed, MetasCalibracao? metas = null)
    {
        ArgumentNullException.ThrowIfNull(criarMundo);
        metas ??= MetasCalibracao.Padrao;

        int maisForteCampeao = 0, entreQuatro = 0, clubesNoVermelho = 0, clubes = 0;
        double pontosCampeao = 0, pontosLanterna = 0, golsPorJogo = 0;
        var resultadosGrandes = new List<double>();
        var resultadosPequenos = new List<double>();

        for (var i = 0; i < temporadas; i++)
        {
            var mundo = criarMundo();
            var serieA = mundo.Divisoes[0];
            var porForca = serieA.ClubeIds.OrderByDescending(id => Top11(mundo.Clube(id))).ThenBy(id => id, StringComparer.Ordinal).ToArray();

            var temporada = new Temporada(mundo, seed + (ulong)i);
            temporada.JogarAteOFim();
            var tabela = temporada.Tabela(serieA.Id);

            maisForteCampeao += tabela[0].ClubeId == porForca[0] ? 1 : 0;
            entreQuatro += porForca.Take(4).Contains(tabela[0].ClubeId) ? 1 : 0;
            pontosCampeao += tabela[0].Pontos;
            pontosLanterna += tabela[^1].Pontos;
            golsPorJogo += (double)tabela.Sum(l => l.GolsPro) / tabela.Sum(l => l.Jogos) * 2;

            // Resultado da temporada inclui a premiação pela posição final (paga no fim de temporada).
            var premios = mundo.Divisoes.SelectMany(d =>
                {
                    var t = temporada.Tabela(d.Id);
                    return t.Select(l => (l.ClubeId, Premio: Financas.Financas.Premio(l.Posicao, t.Count, d, temporada.RegrasFinanceiras)));
                })
                .ToDictionary(x => x.ClubeId, x => x.Premio, StringComparer.Ordinal);

            foreach (var clube in mundo.Clubes)
            {
                var resultado = (double)(clube.Extrato.Values.Sum() + premios[clube.Id]) / 1_000_000;
                if (clube.Reputacao >= 80)
                {
                    resultadosGrandes.Add(resultado);
                }
                else if (clube.Reputacao < 46)
                {
                    resultadosPequenos.Add(resultado);
                }

                clubesNoVermelho += clube.Caixa < 0 ? 1 : 0;
                clubes++;
            }
        }

        double Media(double total) => total / temporadas;

        return
        [
            new("Mais forte é campeão", Media(maisForteCampeao), metas.MaisForteCampeao.Min, metas.MaisForteCampeao.Max, "P0"),
            new("Campeão entre os 4 mais fortes", Media(entreQuatro), metas.CampeaoEntreOsQuatroMaisFortes, null, "P0"),
            new("Pontos do campeão", Media(pontosCampeao), metas.PontosDoCampeao.Min, metas.PontosDoCampeao.Max, "F1"),
            new("Pontos do lanterna", Media(pontosLanterna), metas.PontosDoLanterna.Min, metas.PontosDoLanterna.Max, "F1"),
            new("Gols por jogo na temporada", Media(golsPorJogo), null, null, "F2"),
            new("Resultado dos grandes (R$ mi)", resultadosGrandes.DefaultIfEmpty(0).Average(), metas.ResultadoDosGrandes.Min, metas.ResultadoDosGrandes.Max, "F1"),
            new("Resultado dos pequenos (R$ mi)", resultadosPequenos.DefaultIfEmpty(0).Average(), metas.ResultadoDosPequenos.Min, metas.ResultadoDosPequenos.Max, "F1"),
            new("Clubes no vermelho", clubes == 0 ? 0 : (double)clubesNoVermelho / clubes, null, metas.ClubesNoVermelhoMaximo, "P0"),
        ];
    }

    /// <summary>
    /// Carreiras de vários anos seguidos, com acesso, rebaixamento, contratos e mercado da IA.
    /// O "jogador" é o clube mais fraco da última divisão, com escalação automática (quase não interfere).
    /// </summary>
    public static IReadOnlyList<Meta> Carreiras(Func<Mundo> criarMundo, int carreiras, int anos, ulong seed, MetasCalibracao? metas = null)
    {
        ArgumentNullException.ThrowIfNull(criarMundo);
        metas ??= MetasCalibracao.Padrao;

        double campeoesDistintos = 0, maiorTitulos = 0, transferencias = 0, vermelho = 0, clubes = 0, livres = 0;
        double concentracao = 0;
        int menorElenco = int.MaxValue, maiorElenco = 0;

        for (var i = 0; i < carreiras; i++)
        {
            var mundo = criarMundo();
            var ultimo = mundo.Divisoes[^1].ClubeIds.OrderBy(id => Top11(mundo.Clube(id))).ThenBy(id => id, StringComparer.Ordinal).First();
            var carreira = new Carreira(mundo, ultimo, seed + (ulong)i);
            var campeoes = new List<string>();

            for (var ano = 0; ano < anos; ano++)
            {
                while (!carreira.Temporada.Encerrada)
                {
                    carreira.AvancarRodada();
                    transferencias += carreira.UltimasTransferencias.Count;
                }

                campeoes.Add(carreira.Temporada.Tabela(mundo.Divisoes[0].Id)[0].ClubeId);
                carreira.EncerrarTemporada();
            }

            campeoesDistintos += campeoes.Distinct(StringComparer.Ordinal).Count();
            maiorTitulos += campeoes.GroupBy(c => c, StringComparer.Ordinal).Max(g => g.Count());
            var caixas = mundo.Clubes.Select(c => (double)c.Caixa).OrderByDescending(c => c).ToArray();
            concentracao += caixas.Take(4).Sum() / Math.Max(1, caixas.Where(c => c > 0).Sum());
            vermelho += caixas.Count(c => c < 0);
            clubes += caixas.Length;
            livres += mundo.AgentesLivres.Count;
            menorElenco = Math.Min(menorElenco, mundo.Clubes.Min(c => c.Elenco.Count));
            maiorElenco = Math.Max(maiorElenco, mundo.Clubes.Max(c => c.Elenco.Count));
        }

        double Media(double total) => total / carreiras;

        return
        [
            new($"Campeões diferentes em {anos} anos", Media(campeoesDistintos), metas.CampeoesDiferentes.Min, metas.CampeoesDiferentes.Max, "F1"),
            new("Títulos do maior campeão", Media(maiorTitulos), null, metas.TitulosDoMaiorCampeaoMaximo, "F1"),
            new("Transferências da IA por ano", Media(transferencias) / anos, metas.TransferenciasPorAno.Min, metas.TransferenciasPorAno.Max, "F0"),
            new("Caixa dos 4 mais ricos (fatia)", Media(concentracao), null, metas.ConcentracaoDeCaixaMaxima, "P0"),
            new("Clubes no vermelho no fim", clubes == 0 ? 0 : vermelho / clubes, null, metas.ClubesNoVermelhoMaximo * 2, "P0"),
            new("Agentes livres no fim", Media(livres), null, null, "F0"),
            new("Menor elenco", menorElenco, metas.ElencoMinimo, null, "F0"),
            new("Maior elenco", maiorElenco, null, metas.ElencoMaximo, "F0"),
        ];
    }

    private static List<Meta> Liga(Mundo mundo, int partidas, ulong seed, MetasCalibracao metas, ConfiguracaoMotor cfg)
    {
        var sorteio = new GeradorAleatorio(seed);
        var escalacoes = mundo.Clubes.ToDictionary(c => c.Id, c => Escalador.Escalar(c), StringComparer.Ordinal);
        var top11 = mundo.Clubes.ToDictionary(c => c.Id, Top11, StringComparer.Ordinal);

        int vitoriasMandante = 0, empates = 0, gols = 0, finalizacoes = 0, noAlvo = 0, amarelos = 0, vermelhos = 0, lesoes = 0;
        int jogosComFavorito = 0, vitoriasFavorito = 0, empatesFavorito = 0;

        for (var i = 0; i < partidas; i++)
        {
            var divisao = mundo.Divisoes[sorteio.Proximo(0, mundo.Divisoes.Count)];
            var a = divisao.ClubeIds[sorteio.Proximo(0, divisao.ClubeIds.Count)];
            string b;
            do
            {
                b = divisao.ClubeIds[sorteio.Proximo(0, divisao.ClubeIds.Count)];
            }
            while (b == a);

            var r = MotorDePartida.SimularRapido(escalacoes[a], escalacoes[b], new GeradorAleatorio(seed + (ulong)i + 1), configuracao: cfg);

            gols += r.GolsMandante + r.GolsVisitante;
            vitoriasMandante += r.GolsMandante > r.GolsVisitante ? 1 : 0;
            empates += r.GolsMandante == r.GolsVisitante ? 1 : 0;
            finalizacoes += r.EstatisticasMandante.Finalizacoes + r.EstatisticasVisitante.Finalizacoes;
            noAlvo += r.EstatisticasMandante.NoAlvo + r.EstatisticasVisitante.NoAlvo;
            amarelos += r.EstatisticasMandante.Amarelos + r.EstatisticasVisitante.Amarelos;
            vermelhos += r.EstatisticasMandante.Vermelhos + r.EstatisticasVisitante.Vermelhos;
            lesoes += r.Participacoes.Count(p => p.RodadasDeLesao > 0);

            var diferenca = top11[a] - top11[b];
            if (Math.Abs(diferenca) >= metas.DiferencaFavorito)
            {
                jogosComFavorito++;
                var saldoFavorito = diferenca > 0 ? r.GolsMandante - r.GolsVisitante : r.GolsVisitante - r.GolsMandante;
                vitoriasFavorito += saldoFavorito > 0 ? 1 : 0;
                empatesFavorito += saldoFavorito == 0 ? 1 : 0;
            }
        }

        double Por(int total) => (double)total / partidas;

        return
        [
            new("Gols por jogo", Por(gols), metas.GolsPorJogo.Min, metas.GolsPorJogo.Max),
            new("Vitórias do mandante", Por(vitoriasMandante), metas.VitoriasMandante.Min, metas.VitoriasMandante.Max, "P1"),
            new("Empates", Por(empates), metas.Empates.Min, metas.Empates.Max, "P1"),
            new("Vitórias do visitante", Por(partidas - vitoriasMandante - empates), null, null, "P1"),
            new($"Favorito (+{metas.DiferencaFavorito} de força) vence", jogosComFavorito == 0 ? 0 : (double)vitoriasFavorito / jogosComFavorito,
                metas.FavoritoVence.Min, metas.FavoritoVence.Max, "P1"),
            new("Favorito empata", jogosComFavorito == 0 ? 0 : (double)empatesFavorito / jogosComFavorito, null, null, "P1"),
            new("Finalizações por jogo", Por(finalizacoes), null, null, "F1"),
            new("No alvo por jogo", Por(noAlvo), null, null, "F1"),
            new("Amarelos por jogo", Por(amarelos), null, null, "F1"),
            new("Vermelhos por jogo", Por(vermelhos), null, null, "F2"),
            new("Lesões por jogo", Por(lesoes), null, null, "F2"),
        ];
    }

    private static List<Meta> Formacoes(Mundo mundo, int partidas, ulong seed, MetasCalibracao metas, ConfiguracaoMotor cfg)
    {
        var resultado = new List<Meta>();
        var serieA = mundo.Divisoes[0].ClubeIds.Select(mundo.Clube).ToArray();
        var referencia = FormacoesPadrao.QuatroQuatroDois;

        var testadas = FormacoesPadrao.Todas.Where(f => f != referencia)
            .Select(f => (Formacao: f, Extrema: false))
            .Concat([(CincoZeroCinco, true), (TresZeroSete, true)]);

        foreach (var (formacao, extrema) in testadas)
        {
            var sorteio = new GeradorAleatorio(seed ^ Geracao.GeradorDeMundo.HashEstavel(formacao.Nome));
            var cacheTeste = new Dictionary<string, TimeEscalado>(StringComparer.Ordinal);
            var cacheReferencia = new Dictionary<string, TimeEscalado>(StringComparer.Ordinal);
            int pontosTeste = 0, pontosReferencia = 0;

            for (var i = 0; i < partidas; i++)
            {
                var a = serieA[sorteio.Proximo(0, serieA.Length)];
                Clube b;
                do
                {
                    b = serieA[sorteio.Proximo(0, serieA.Length)];
                }
                while (b.Id == a.Id);

                var teste = Escalacao(cacheTeste, a, formacao);
                var adversario = Escalacao(cacheReferencia, b, referencia);
                var testeEmCasa = i % 2 == 0;

                var r = testeEmCasa
                    ? MotorDePartida.SimularRapido(teste, adversario, new GeradorAleatorio(seed + (ulong)i + 7), configuracao: cfg)
                    : MotorDePartida.SimularRapido(adversario, teste, new GeradorAleatorio(seed + (ulong)i + 7), configuracao: cfg);

                var saldo = testeEmCasa ? r.GolsMandante - r.GolsVisitante : r.GolsVisitante - r.GolsMandante;
                pontosTeste += saldo > 0 ? 3 : saldo == 0 ? 1 : 0;
                pontosReferencia += saldo < 0 ? 3 : saldo == 0 ? 1 : 0;
            }

            // Fatia dos pontos do confronto: 50% = equilíbrio, independente da taxa de empates.
            var aproveitamento = (double)pontosTeste / Math.Max(1, pontosTeste + pontosReferencia);
            resultado.Add(extrema
                ? new Meta($"{formacao.Nome} x 4-4-2", aproveitamento, null, metas.FormacaoExtremaMaximo, "P1")
                : new Meta($"{formacao.Nome} x 4-4-2", aproveitamento, metas.FormacaoPadrao.Min, metas.FormacaoPadrao.Max, "P1"));
        }

        return resultado;
    }

    /// <summary>
    /// Marcação e concentração (D-032): cada opção, num 4-4-2, contra o mesmo 4-4-2 com marcação normal e
    /// ataque variado. Nenhuma opção deve ganhar ou perder sempre.
    /// </summary>
    private static List<Meta> Estilos(Mundo mundo, int partidas, ulong seed, MetasCalibracao metas, ConfiguracaoMotor cfg)
    {
        var resultado = new List<Meta>();
        var serieA = mundo.Divisoes[0].ClubeIds.Select(mundo.Clube).ToArray();
        var formacao = FormacoesPadrao.QuatroQuatroDois;
        (string Nome, Marcacao Marcacao, Concentracao Concentracao)[] testadas =
        [
            ("Marcação forte x normal", Marcacao.Forte, Concentracao.Variado),
            ("Marcação leve x normal", Marcacao.Leve, Concentracao.Variado),
            ("Ataque pela esquerda x variado", Marcacao.Normal, Concentracao.Esquerda),
            ("Ataque pelo meio x variado", Marcacao.Normal, Concentracao.Meio),
            ("Ataque pela direita x variado", Marcacao.Normal, Concentracao.Direita),
        ];

        foreach (var (nome, marcacao, concentracao) in testadas)
        {
            var sorteio = new GeradorAleatorio(seed ^ Geracao.GeradorDeMundo.HashEstavel(nome));
            var cache = new Dictionary<string, TimeEscalado>(StringComparer.Ordinal);
            int pontosTeste = 0, pontosReferencia = 0;

            for (var i = 0; i < partidas; i++)
            {
                var a = serieA[sorteio.Proximo(0, serieA.Length)];
                Clube b;
                do
                {
                    b = serieA[sorteio.Proximo(0, serieA.Length)];
                }
                while (b.Id == a.Id);

                var normal = Escalacao(cache, a, formacao);
                var teste = new TimeEscalado(normal.ClubeId, normal.Formacao, normal.Titulares, normal.Reservas, normal.Mentalidade,
                    normal.BonusEntrosamento, marcacao, concentracao);
                var adversario = Escalacao(cache, b, formacao);
                var testeEmCasa = i % 2 == 0;

                var r = testeEmCasa
                    ? MotorDePartida.SimularRapido(teste, adversario, new GeradorAleatorio(seed + (ulong)i + 11), configuracao: cfg)
                    : MotorDePartida.SimularRapido(adversario, teste, new GeradorAleatorio(seed + (ulong)i + 11), configuracao: cfg);

                var saldo = testeEmCasa ? r.GolsMandante - r.GolsVisitante : r.GolsVisitante - r.GolsMandante;
                pontosTeste += saldo > 0 ? 3 : saldo == 0 ? 1 : 0;
                pontosReferencia += saldo < 0 ? 3 : saldo == 0 ? 1 : 0;
            }

            var aproveitamento = (double)pontosTeste / Math.Max(1, pontosTeste + pontosReferencia);
            resultado.Add(new Meta(nome, aproveitamento, metas.EstiloPadrao.Min, metas.EstiloPadrao.Max, "P1"));
        }

        return resultado;
    }

    private static TimeEscalado Escalacao(Dictionary<string, TimeEscalado> cache, Clube clube, Formacao formacao)
    {
        if (!cache.TryGetValue(clube.Id, out var escalacao))
        {
            escalacao = Escalador.EscalarNaFormacao(clube, formacao);
            cache[clube.Id] = escalacao;
        }

        return escalacao;
    }

    private static double Top11(Clube clube) =>
        clube.Elenco.OrderByDescending(j => j.ForcaGeral).Take(11).Average(j => j.ForcaGeral);
}
