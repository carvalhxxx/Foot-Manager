using Jogo.Dominio.Competicoes;

namespace Jogo.Simulacao.Temporadas;

/// <summary>Tipo de faixa da tabela (usado também como classe de cor na interface).</summary>
public enum TipoDeZona
{
    Libertadores,
    LibertadoresQualificacao,
    SulAmericana,
    Acesso,
    Playoff,
    Rebaixamento,
}

/// <summary>Uma faixa da tabela, de uma posição a outra (inclusive).</summary>
public sealed record ZonaTabela(TipoDeZona Tipo, int De, int Ate);

/// <summary>
/// Faixas da tabela de cada divisão e os critérios de desempate (D-036). Na divisão de cima, as vagas
/// continentais do Brasileirão: 1º a 4º Libertadores, 5º e 6º fase preliminar da Libertadores,
/// 7º a 12º Sul-Americana. Por enquanto as vagas só aparecem na tabela; as competições
/// continentais ainda não são disputadas.
/// </summary>
public static class ZonasDaTabela
{
    public const int VagasLibertadores = 4;
    public const int VagasLibertadoresQualificacao = 2;
    public const int VagasSulAmericana = 6;

    /// <summary>
    /// Critérios de desempate (chaves; o texto fica na interface), na ordem em que
    /// <see cref="Classificacao"/> os aplica depois dos pontos.
    /// </summary>
    public static IReadOnlyList<string> CriteriosDeDesempate { get; } =
        ["Vitorias", "Saldo", "GolsPro", "ConfrontoDireto", "Vermelhos", "Amarelos", "Sorteio"];

    public static IReadOnlyList<ZonaTabela> De(Divisao divisao)
    {
        ArgumentNullException.ThrowIfNull(divisao);
        var total = divisao.ClubeIds.Count;
        var zonas = new List<ZonaTabela>();

        if (divisao.Nivel == 1)
        {
            var libertadores = Math.Min(VagasLibertadores, total);
            var qualificacao = Math.Min(libertadores + VagasLibertadoresQualificacao, total);
            var sulAmericana = Math.Min(qualificacao + VagasSulAmericana, total - divisao.VagasRebaixamento);
            zonas.Add(new(TipoDeZona.Libertadores, 1, libertadores));
            if (qualificacao > libertadores)
            {
                zonas.Add(new(TipoDeZona.LibertadoresQualificacao, libertadores + 1, qualificacao));
            }

            if (sulAmericana > qualificacao)
            {
                zonas.Add(new(TipoDeZona.SulAmericana, qualificacao + 1, sulAmericana));
            }
        }
        else if (divisao.VagasAcesso > 0)
        {
            if (divisao.VagasDiretas > 0)
            {
                zonas.Add(new(TipoDeZona.Acesso, 1, divisao.VagasDiretas));
            }

            if (divisao.TemPlayoff)
            {
                zonas.Add(new(TipoDeZona.Playoff, divisao.VagasDiretas + 1, divisao.VagasDiretas + (2 * divisao.VagasNoPlayoff)));
            }
        }

        if (divisao.VagasRebaixamento > 0)
        {
            zonas.Add(new(TipoDeZona.Rebaixamento, total - divisao.VagasRebaixamento + 1, total));
        }

        return zonas;
    }
}
