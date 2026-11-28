using Jogo.Dominio.Clubes;
using Jogo.Dominio.Competicoes;

namespace Jogo.Simulacao.Financas;

/// <summary>
/// Números da economia. Os valores padrão saem da calibração (tools/Jogo.Calibracao):
/// mudar qualquer um exige rodar a calibração de novo.
/// </summary>
public sealed record RegrasFinanceiras
{
    public static RegrasFinanceiras Padrao { get; } = new();

    // Bilheteria --------------------------------------------------------------

    /// <summary>Torcedores dispostos a ir ao jogo por ponto de torcida (1-100), com ingresso no preço sugerido.</summary>
    public double PublicoPorPontoDeTorcida { get; init; } = 300;

    /// <summary>Preço sugerido: base + reputação × fator (R$): cerca de R$ 20 nos pequenos e R$ 90 nos maiores (D-041).</summary>
    public decimal PrecoBase { get; init; } = -30m;
    public decimal PrecoPorReputacao { get; init; } = 1.27m;

    /// <summary>Quanto o público cai quando o ingresso sobe (público ∝ (sugerido/preço)^elasticidade).</summary>
    public double Elasticidade { get; init; } = 1.3;

    /// <summary>Quanto a fase (últimos 5 jogos) mexe no público: 0 = nada; 0,3 = ±30%.</summary>
    public double EfeitoDaFase { get; init; } = 0.3;

    /// <summary>Quanto um adversário de torcida grande aumenta o público (0,3 = até +30%).</summary>
    public double EfeitoDoAdversario { get; init; } = 0.3;

    // Receitas fixas (por temporada, pagas em parcelas a cada rodada) --------

    /// <summary>
    /// Cota de TV anual = reputação ^ expoente × fator (escala real, D-041): na Série A, cerca de
    /// R$ 70 mi nos pequenos e R$ 330 mi no maior.
    /// </summary>
    public double ExpoenteTv { get; init; } = 3.0;

    public decimal FatorTv { get; init; } = 385m;

    /// <summary>Fração da cota de TV da Série A paga na Série B (nível 2) e na Série C (nível 3 ou abaixo).</summary>
    public decimal FatorTvSerieB { get; init; } = 0.25m;

    public decimal FatorTvSerieC { get; init; } = 0.06m;

    /// <summary>
    /// Patrocínio e receitas comerciais (inclui sócio-torcedor) = reputação ^ expoente × fator: de poucos milhões
    /// nos pequenos a mais de R$ 500 mi no maior. O expoente alto faz a receita dos grandes crescer
    /// no mesmo ritmo da folha deles (o salário cresce exponencialmente com a força).
    /// </summary>
    public double ExpoentePatrocinio { get; init; } = 5.0;

    public decimal FatorPatrocinio { get; init; } = 0.071m;

    /// <summary>
    /// Custos operacionais (encargos, comissão técnica, CT, viagens, estrutura) como fração da TV e do patrocínio:
    /// na vida real a folha é só parte do custo do futebol.
    /// </summary>
    public decimal FracaoCustosOperacionais { get; init; } = 0.55m;

    // Premiação por posição no fim da temporada ---------------------------------

    public decimal PremioCampeao { get; init; } = 48_000_000m;

    /// <summary>Divisões abaixo da primeira recebem esta fração da premiação.</summary>
    public decimal FatorPremioDivisaoInferior { get; init; } = 0.07m;
}

/// <summary>Regras da economia do clube: bilheteria, receitas fixas, salários e premiação.</summary>
public static class Financas
{
    public static decimal PrecoSugerido(Clube clube, RegrasFinanceiras regras) =>
        Math.Max(5m, Math.Round(regras.PrecoBase + (clube.Reputacao * regras.PrecoPorReputacao)));

    public static decimal PrecoDoIngresso(Clube clube, RegrasFinanceiras regras) =>
        clube.PrecoIngresso ?? PrecoSugerido(clube, regras);

    /// <summary>
    /// Público de um jogo em casa: torcida do mandante × fase × atração do adversário × efeito do preço,
    /// limitado pela capacidade do estádio.
    /// </summary>
    /// <param name="aproveitamentoRecente">Fração dos pontos nos últimos jogos (0 a 1); 0,5 se ainda não jogou.</param>
    public static int Publico(Clube mandante, Clube visitante, decimal preco, double aproveitamentoRecente, RegrasFinanceiras regras)
    {
        ArgumentNullException.ThrowIfNull(mandante);
        ArgumentNullException.ThrowIfNull(visitante);
        ArgumentNullException.ThrowIfNull(regras);

        var fase = 1 + (regras.EfeitoDaFase * ((aproveitamentoRecente * 2) - 1));
        var adversario = 1 + (regras.EfeitoDoAdversario * visitante.Torcida / 100.0);
        var precoRelativo = (double)(PrecoSugerido(mandante, regras) / Math.Max(1m, preco));
        var demanda = mandante.Torcida * regras.PublicoPorPontoDeTorcida * fase * adversario * Math.Pow(precoRelativo, regras.Elasticidade);
        return (int)Math.Clamp(Math.Round(demanda), 0, mandante.Estadio.Capacidade);
    }

    /// <summary>Cota de TV anual: depende da reputação e da divisão.</summary>
    public static decimal CotaTvAnual(Clube clube, Divisao divisao, RegrasFinanceiras regras)
    {
        var cota = (decimal)Math.Pow(clube.Reputacao, regras.ExpoenteTv) * regras.FatorTv;
        var fator = divisao.Nivel switch
        {
            1 => 1m,
            2 => regras.FatorTvSerieB,
            _ => regras.FatorTvSerieC,
        };
        return Math.Round(cota * fator);
    }

    public static decimal PatrocinioAnual(Clube clube, RegrasFinanceiras regras) =>
        Math.Round((decimal)Math.Pow(clube.Reputacao, regras.ExpoentePatrocinio) * regras.FatorPatrocinio);

    /// <summary>Custos operacionais anuais: fração da TV e do patrocínio do clube.</summary>
    public static decimal CustosOperacionaisAnuais(Clube clube, Divisao divisao, RegrasFinanceiras regras) =>
        Math.Round((CotaTvAnual(clube, divisao, regras) + PatrocinioAnual(clube, regras)) * regras.FracaoCustosOperacionais);

    /// <summary>Soma dos salários mensais do elenco.</summary>
    public static decimal FolhaMensal(Clube clube) => clube.Elenco.Sum(j => j.Contrato?.SalarioMensal ?? 0);

    /// <summary>
    /// Movimento de uma rodada para um clube: parcelas de TV, patrocínio, custos operacionais e salários
    /// (folha mensal × 12 meses ÷ rodadas da temporada).
    /// </summary>
    public static void ProcessarRodada(Clube clube, Divisao divisao, int rodadasNaTemporada, RegrasFinanceiras regras)
    {
        ArgumentNullException.ThrowIfNull(clube);
        ArgumentNullException.ThrowIfNull(divisao);
        clube.Lancar(CategoriaFinanceira.CotaDeTv, Math.Round(CotaTvAnual(clube, divisao, regras) / rodadasNaTemporada));
        clube.Lancar(CategoriaFinanceira.Patrocinio, Math.Round(PatrocinioAnual(clube, regras) / rodadasNaTemporada));
        clube.Lancar(CategoriaFinanceira.CustosOperacionais, -Math.Round(CustosOperacionaisAnuais(clube, divisao, regras) / rodadasNaTemporada));
        clube.Lancar(CategoriaFinanceira.Salarios, -Math.Round(FolhaMensal(clube) * 12 / rodadasNaTemporada));
    }

    /// <summary>Bilheteria de um jogo em casa.</summary>
    /// <param name="parteDoMandante">Fração da renda que fica com o mandante; o resto vai para o visitante (Copa: metade).</param>
    public static void ProcessarJogoEmCasa(Clube mandante, Clube visitante, double aproveitamentoRecente, RegrasFinanceiras regras,
        decimal parteDoMandante = 1m)
    {
        var preco = PrecoDoIngresso(mandante, regras);
        var publico = Publico(mandante, visitante, preco, aproveitamentoRecente, regras);
        var renda = publico * preco;
        var doMandante = Math.Round(renda * parteDoMandante);
        mandante.Lancar(CategoriaFinanceira.Bilheteria, doMandante);
        if (renda - doMandante != 0)
        {
            visitante.Lancar(CategoriaFinanceira.Bilheteria, renda - doMandante);
        }

        mandante.RegistrarJogoEmCasa(publico, renda);
    }

    /// <summary>Premiação pela posição final: o campeão leva o valor cheio e o último, 1/N dele.</summary>
    public static decimal Premio(int posicao, int clubes, Divisao divisao, RegrasFinanceiras regras)
    {
        var cheio = divisao.Nivel == 1 ? regras.PremioCampeao : regras.PremioCampeao * regras.FatorPremioDivisaoInferior;
        return Math.Round(cheio * (clubes - posicao + 1) / clubes);
    }
}
