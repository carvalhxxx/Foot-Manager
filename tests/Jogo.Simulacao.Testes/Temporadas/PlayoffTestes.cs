using Jogo.Dominio;
using Jogo.Dominio.Competicoes;
using Jogo.Simulacao.Geracao;
using Jogo.Simulacao.Temporadas;

namespace Jogo.Simulacao.Testes.Temporadas;

public class PlayoffTestes
{
    /// <summary>Mundo base com a Série B no formato de 2026: 2 sobem direto e 3º a 6º vão ao playoff.</summary>
    public static Mundo MundoComPlayoff()
    {
        var mundo = GeradorDeMundo.Gerar(2027, 2027, CatalogoBase.Clubes);
        mundo.ReorganizarDivisoes(mundo.Divisoes.Select(d => d.Nivel == 1
            ? d
            : new Divisao(d.Id, d.Nome, d.Nivel, d.ClubeIds, d.VagasAcesso, d.VagasRebaixamento, vagasDiretas: 2)));
        return mundo;
    }

    [Fact]
    public void Divisao_ConfrontosTerceiroContraSextoEQuartoContraQuinto()
    {
        var serieB = MundoComPlayoff().Divisoes[1];

        Assert.Equal([(3, 6), (4, 5)], serieB.ConfrontosDoPlayoff);
        Assert.Equal((2, 2), (serieB.VagasDiretas, serieB.VagasNoPlayoff));
    }

    [Fact]
    public void Temporada_DuasRodadasExtrasSoParaQuemTemPlayoff()
    {
        var temporada = new Temporada(MundoComPlayoff(), 3);

        Assert.Equal(40, temporada.TotalRodadas);
        Assert.Empty(temporada.Playoff("serie-b"));
    }

    [Fact]
    public void FimDosPontosCorridos_DefineOsJogosComMandoDoMelhorNaVolta()
    {
        var temporada = new Temporada(MundoComPlayoff(), 3);
        for (var r = 0; r < 38; r++)
        {
            temporada.JogarRodada();
        }

        var tabela = temporada.Tabela("serie-b");
        var calendario = temporada.Competicao("serie-b").Calendario;
        Assert.Equal(40, calendario.Count);
        Assert.Equal(
            [new JogoAgendado(39, tabela[5].ClubeId, tabela[2].ClubeId), new JogoAgendado(39, tabela[4].ClubeId, tabela[3].ClubeId)],
            calendario[38]);
        Assert.Equal(
            [new JogoAgendado(40, tabela[2].ClubeId, tabela[5].ClubeId), new JogoAgendado(40, tabela[3].ClubeId, tabela[4].ClubeId)],
            calendario[39]);
        Assert.Equal(38, temporada.Competicao("serie-a").Calendario.Count);
    }

    [Fact]
    public void Playoff_NaoMudaATabelaEDecideOAcesso()
    {
        var mundo = MundoComPlayoff();
        var temporada = new Temporada(mundo, 3);
        temporada.JogarAteOFim();

        var tabela = temporada.Tabela("serie-b");
        Assert.All(tabela, l => Assert.Equal(38, l.Jogos));

        var playoff = temporada.Playoff("serie-b");
        Assert.Equal(2, playoff.Count);
        foreach (var c in playoff)
        {
            // Agregado; empate classifica o melhor colocado.
            var esperado = c.GolsPior > c.GolsMelhor ? c.PiorId : c.MelhorId;
            Assert.Equal(esperado, c.VencedorId);
        }

        var resultado = FimDeTemporada.Encerrar(temporada, mundo, 3);
        var promovidos = resultado.Divisoes[1].Promovidos;
        Assert.Equal([tabela[0].ClubeId, tabela[1].ClubeId, playoff[0].VencedorId!, playoff[1].VencedorId!], promovidos);
        Assert.All(promovidos, id => Assert.Contains(id, mundo.Divisoes[0].ClubeIds));
        Assert.Equal(2, mundo.Divisoes[1].VagasDiretas);
    }

    [Fact]
    public void Restaurar_NoMeioDoPlayoff_RecriaOsConfrontos()
    {
        var temporada = new Temporada(MundoComPlayoff(), 3);
        for (var r = 0; r < 39; r++)
        {
            temporada.JogarRodada();
        }

        var copia = new Temporada(MundoComPlayoff(), 3);
        copia.Restaurar(39, temporada.Competicoes.SelectMany(c => c.Disputados.Select(j => (c.Divisao.Id, j))));

        Assert.Equal(temporada.Playoff("serie-b"), copia.Playoff("serie-b"));
        Assert.Equal(temporada.Competicao("serie-b").Calendario[39], copia.Competicao("serie-b").Calendario[39]);
    }
}
