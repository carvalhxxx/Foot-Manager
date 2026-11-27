using Jogo.Simulacao.Temporadas;

namespace Jogo.Simulacao.Testes.Temporadas;

public class ClassificacaoTestes
{
    private static PlacarJogo Jogo(string mandante, string visitante, int gm, int gv, int amarelosM = 0, int vermelhosM = 0) =>
        new(new JogoAgendado(1, mandante, visitante), gm, gv, amarelosM, 0, vermelhosM, 0);

    /// <summary>Ordem relativa entre os dois clubes que interessam ao teste (ignora os adversários auxiliares).</summary>
    private static string[] Ordem(IEnumerable<PlacarJogo> jogos, params string[] clubes) =>
        [.. Classificacao.Calcular(clubes, jogos).Select(l => l.ClubeId).Where(id => id is "a" or "b")];

    [Fact]
    public void Pontos_VitoriaTresEmpateUm()
    {
        var tabela = Classificacao.Calcular(["a", "b", "c"], [Jogo("a", "b", 2, 0), Jogo("b", "c", 1, 1)]);

        var a = tabela.Single(l => l.ClubeId == "a");
        var b = tabela.Single(l => l.ClubeId == "b");
        Assert.Equal((3, 1, 1, 0, 0, 2, 0, 2), (a.Pontos, a.Jogos, a.Vitorias, a.Empates, a.Derrotas, a.GolsPro, a.GolsContra, a.Saldo));
        Assert.Equal((1, 2, 0, 1, 1), (b.Pontos, b.Jogos, b.Vitorias, b.Empates, b.Derrotas));
        Assert.Equal([1, 2, 3], tabela.Select(l => l.Posicao));
    }

    [Fact]
    public void Desempate_MaisVitoriasNaFrente()
    {
        // a: 1V 0E 1D = 3 pts; b: 0V 3E = 3 pts.
        var jogos = new[] { Jogo("a", "x", 1, 0), Jogo("a", "y", 0, 1), Jogo("b", "x", 0, 0), Jogo("b", "y", 0, 0), Jogo("b", "z", 0, 0) };

        Assert.Equal(["a", "b"], Ordem(jogos, "a", "b", "x", "y", "z").ToArray());
    }

    [Fact]
    public void Desempate_SaldoDepoisGolsPro()
    {
        var porSaldo = new[] { Jogo("a", "x", 3, 0), Jogo("b", "y", 1, 0) };
        var porGols = new[] { Jogo("a", "x", 3, 1), Jogo("b", "y", 2, 0) };

        Assert.Equal(["a", "b"], Ordem(porSaldo, "a", "b", "x", "y").ToArray());
        Assert.Equal(["a", "b"], Ordem(porGols, "b", "a", "x", "y").ToArray());
    }

    [Fact]
    public void Desempate_ConfrontoDiretoEntreDois()
    {
        // a e b: mesmos pontos, vitórias, saldo e gols; b venceu o confronto direto.
        var jogos = new[] { Jogo("b", "a", 1, 0), Jogo("a", "x", 1, 0), Jogo("y", "b", 1, 0) };
        var espelho = new[] { Jogo("a", "b", 1, 0), Jogo("b", "x", 1, 0), Jogo("y", "a", 1, 0) };

        // Os dois cenários garantem que quem decide é o confronto, não o sorteio.
        Assert.Equal(["b", "a"], Ordem(jogos, "a", "b", "x", "y").ToArray());
        Assert.Equal(["a", "b"], Ordem(espelho, "a", "b", "x", "y").ToArray());
    }

    [Fact]
    public void Desempate_MenosVermelhosDepoisMenosAmarelos()
    {
        var vermelhos = new[] { Jogo("a", "x", 1, 0, vermelhosM: 1), Jogo("b", "y", 1, 0) };
        var amarelos = new[] { Jogo("a", "x", 1, 0, amarelosM: 4), Jogo("b", "y", 1, 0, amarelosM: 1) };

        Assert.Equal(["b", "a"], Ordem(vermelhos, "a", "b", "x", "y").ToArray());
        Assert.Equal(["b", "a"], Ordem(amarelos, "a", "b", "x", "y").ToArray());
    }

    [Fact]
    public void Desempate_SorteioEhDeterministico()
    {
        var a = Classificacao.Calcular(["a", "b", "c", "d"], [], seedSorteio: 9).Select(l => l.ClubeId);
        var b = Classificacao.Calcular(["d", "c", "b", "a"], [], seedSorteio: 9).Select(l => l.ClubeId);

        Assert.Equal(a, b);
    }
}
