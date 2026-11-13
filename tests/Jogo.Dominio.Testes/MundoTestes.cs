using Jogo.Dominio.Clubes;
using Jogo.Dominio.Competicoes;
using Jogo.Dominio.Jogadores;

namespace Jogo.Dominio.Testes;

public class MundoTestes
{
    private static Clube Clube(string id, params int[] jogadores)
    {
        var clube = new Clube(id, id, "ABC", "Recife", "PE", new Cores("#000000", "#FFFFFF"),
            new Estadio("Estádio", 20_000), 50, 50, 0m);
        foreach (var jogadorId in jogadores)
        {
            var jogador = new Jogador(jogadorId, $"J{jogadorId}", 25, Posicao.Volante, [], Atributos.Uniformes(10),
                new AtributosOcultos { Potencial = 60, PropensaoLesao = 5, Profissionalismo = 10 });
            jogador.AssinarContrato(new Contrato(id, 1_000m, 2030));
            clube.AdicionarAoElenco(jogador);
        }

        return clube;
    }

    private static Divisao Divisao(string id, int nivel, int acesso, int rebaixamento, params string[] clubes) =>
        new(id, id, nivel, clubes, acesso, rebaixamento);

    [Fact]
    public void MundoValido_OrdenaDivisoesPorNivelEAchaClube()
    {
        var mundo = new Mundo(2027,
            [Divisao("b", 2, 1, 0, "c", "d"), Divisao("a", 1, 0, 1, "a", "b")],
            [Clube("a", 1), Clube("b", 2), Clube("c", 3), Clube("d", 4)]);

        Assert.Equal(["a", "b"], mundo.Divisoes.Select(d => d.Id));
        Assert.Equal("c", mundo.Clube("c").Id);
        Assert.Throws<KeyNotFoundException>(() => mundo.Clube("x"));
    }

    [Fact]
    public void AvancarTemporada_SomaUm()
    {
        var mundo = new Mundo(2027, [Divisao("a", 1, 0, 0, "a", "b")], [Clube("a"), Clube("b")]);

        mundo.AvancarTemporada();

        Assert.Equal(2028, mundo.TemporadaAtual);
    }

    [Fact]
    public void Inconsistencias_ListaTodosOsProblemasDeUmaVez()
    {
        var problemas = Mundo.Inconsistencias(2027,
            [
                Divisao("a", 1, 0, 1, "a", "b"),
                Divisao("c", 3, 0, 1, "b", "x"),
            ],
            [Clube("a", 1), Clube("b", 1), Clube("c")]);

        Assert.Contains(problemas, p => p.Contains("níveis", StringComparison.Ordinal));
        Assert.Contains(problemas, p => p.Contains("\"x\", que não existe", StringComparison.Ordinal));
        Assert.Contains(problemas, p => p.Contains("\"b\" está em mais de uma divisão", StringComparison.Ordinal));
        Assert.Contains(problemas, p => p.Contains("\"c\" não está em nenhuma divisão", StringComparison.Ordinal));
        Assert.Contains(problemas, p => p.Contains("rebaixa 1, mas \"c\" promove 0", StringComparison.Ordinal));
        Assert.Contains(problemas, p => p.Contains("última divisão", StringComparison.Ordinal));
        Assert.Contains(problemas, p => p.Contains("id 1 aparece mais de uma vez", StringComparison.Ordinal));
    }

    [Fact]
    public void Inconsistencias_ContratoComOutroClube()
    {
        var clube = Clube("a", 1);
        clube.Elenco[0].AssinarContrato(new Contrato("b", 1_000m, 2030));

        var problemas = Mundo.Inconsistencias(2027, [Divisao("a", 1, 0, 0, "a", "b")], [clube, Clube("b")]);

        Assert.Contains(problemas, p => p.Contains("tem contrato com \"b\"", StringComparison.Ordinal));
    }

    [Fact]
    public void Construtor_MundoInconsistente_Lanca()
    {
        Assert.Throws<ArgumentException>(() =>
            new Mundo(2027, [Divisao("a", 1, 0, 0, "a", "b")], [Clube("a")]));
    }

    [Theory]
    [InlineData(Posicao.Goleiro, "GOL")]
    [InlineData(Posicao.AlaEsquerdo, "ALE")]
    [InlineData(Posicao.SegundoAtacante, "SA")]
    [InlineData(Posicao.Centroavante, "ATA")]
    public void Sigla_IdaEVolta(Posicao posicao, string sigla)
    {
        Assert.Equal(sigla, posicao.Sigla());
        Assert.True(PosicaoExtensoes.TentarDaSigla(sigla.ToLowerInvariant(), out var lida));
        Assert.Equal(posicao, lida);
    }

    [Fact]
    public void Sigla_TodasDistintasEDesconhecidaFalha()
    {
        Assert.Equal(15, PosicaoExtensoes.Todas.Select(p => p.Sigla()).Distinct().Count());
        Assert.False(PosicaoExtensoes.TentarDaSigla("XYZ", out _));
        Assert.False(PosicaoExtensoes.TentarDaSigla(null, out _));
    }
}
