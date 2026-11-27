using Jogo.Dominio.Competicoes;
using Jogo.Simulacao.Temporadas;

namespace Jogo.Simulacao.Testes.Temporadas;

public class ZonasDaTabelaTestes
{
    private static readonly string[] Vinte = [.. Enumerable.Range(1, 20).Select(i => $"c{i}")];

    [Fact]
    public void SerieA_VagasContinentaisERebaixamento()
    {
        var serieA = new Divisao("serie-a", "Série A", 1, Vinte, 0, 4);

        Assert.Equal(
            [
                new ZonaTabela(TipoDeZona.Libertadores, 1, 4),
                new ZonaTabela(TipoDeZona.LibertadoresQualificacao, 5, 5),
                new ZonaTabela(TipoDeZona.SulAmericana, 6, 11),
                new ZonaTabela(TipoDeZona.Rebaixamento, 17, 20),
            ],
            ZonasDaTabela.De(serieA));
    }

    [Fact]
    public void SerieB_AcessoDiretoEPlayoff()
    {
        var serieB = new Divisao("serie-b", "Série B", 2, Vinte, 4, 0, vagasDiretas: 2);

        Assert.Equal(
            [new ZonaTabela(TipoDeZona.Acesso, 1, 2), new ZonaTabela(TipoDeZona.Playoff, 3, 6)],
            ZonasDaTabela.De(serieB));
    }

    [Fact]
    public void Criterios_NaOrdemDoBrasileirao()
    {
        Assert.Equal(["Vitorias", "Saldo", "GolsPro"], ZonasDaTabela.CriteriosDeDesempate.Take(3));
    }
}
