using Jogo.Dominio.Aleatoriedade;
using Jogo.Dominio.Jogadores;
using Jogo.Simulacao.Geracao;

namespace Jogo.Simulacao.Testes.Geracao;

public class GeradorDeJogadoresTestes
{
    private static List<Jogador> GerarVarios(Posicao posicao, int forcaAlvo, int idade, int quantidade = 300)
    {
        var gerador = new GeradorDeJogadores(new GeradorAleatorio(42));
        return [.. Enumerable.Range(1, quantidade)
            .Select(i => gerador.Gerar(i, $"J{i}", posicao, forcaAlvo, "clube", 2027, idade))];
    }

    [Theory]
    [InlineData(Posicao.Centroavante, 55)]
    [InlineData(Posicao.Zagueiro, 70)]
    [InlineData(Posicao.Goleiro, 75)]
    [InlineData(Posicao.MeiaOfensivo, 85)]
    public void ForcaMedia_FicaPertoDoAlvo(Posicao posicao, int alvo)
    {
        var media = GerarVarios(posicao, alvo, idade: 26).Average(j => j.ForcaGeral);

        Assert.InRange(media, alvo - 1.5, alvo + 1.5);
    }

    [Fact]
    public void Goleiro_TemFinalizacaoEDribleBaixos()
    {
        Assert.All(GerarVarios(Posicao.Goleiro, 75, 27), j =>
        {
            Assert.InRange(j.Atributos.Finalizacao, 1, 5);
            Assert.InRange(j.Atributos.Drible, 1, 5);
            Assert.True(j.Atributos.Reflexos > j.Atributos.Desarme);
        });
    }

    [Fact]
    public void JogadorDeLinha_TemReflexosEPosicionamentoBaixos()
    {
        Assert.All(GerarVarios(Posicao.PontaDireita, 75, 27), j =>
        {
            Assert.InRange(j.Atributos.Reflexos, 1, 4);
            Assert.InRange(j.Atributos.Posicionamento, 1, 4);
        });
    }

    [Fact]
    public void Potencial_NuncaMenorQueAForcaEJovensTemMaisMargem()
    {
        var jovens = GerarVarios(Posicao.MeiaCentral, 60, idade: 18);
        var veteranos = GerarVarios(Posicao.MeiaCentral, 60, idade: 32);

        Assert.All(jovens.Concat(veteranos), j => Assert.True(j.Ocultos.Potencial >= j.ForcaGeral));
        Assert.True(jovens.Average(j => j.Ocultos.Potencial - j.ForcaGeral) > 10);
        Assert.All(veteranos, j => Assert.Equal(j.ForcaGeral, j.Ocultos.Potencial));
    }

    [Fact]
    public void SecundariasSaoPlausiveis()
    {
        var laterais = GerarVarios(Posicao.LateralDireito, 65, 25);

        Assert.All(laterais.SelectMany(j => j.PosicoesSecundarias),
            p => Assert.Contains(p, new[] { Posicao.AlaDireito, Posicao.MeiaDireito, Posicao.Zagueiro }));
        Assert.Contains(laterais, j => j.PosicoesSecundarias.Count == 0);
        Assert.Contains(laterais, j => j.PosicoesSecundarias.Count == 2);
    }

    [Theory]
    [InlineData(50, 6_000)]
    [InlineData(70, 60_000)]
    [InlineData(85, 329_000)]
    public void Salario_CresceComAForca(int forca, decimal aproximado)
    {
        var salario = GeradorDeJogadores.Salario(forca);

        Assert.InRange(salario, aproximado * 0.95m, aproximado * 1.05m);
        Assert.Equal(0, salario % 500);
        Assert.True(GeradorDeJogadores.Salario(forca + 1) > salario);
    }

    [Fact]
    public void Contrato_TerminaEntreATemporadaAtualETresAnosDepois()
    {
        Assert.All(GerarVarios(Posicao.Volante, 60, 25), j => Assert.InRange(j.Contrato!.TemporadaFinal, 2027, 2030));
    }

    [Fact]
    public void GeradorDeNomes_NuncaRepeteNoMesmoConjunto()
    {
        var nomes = new GeradorDeNomes(new GeradorAleatorio(1));
        var usados = new HashSet<string>(StringComparer.Ordinal);

        var gerados = Enumerable.Range(0, 500).Select(_ => nomes.Gerar(usados)).ToList();

        Assert.Equal(500, gerados.Distinct().Count());
    }
}
