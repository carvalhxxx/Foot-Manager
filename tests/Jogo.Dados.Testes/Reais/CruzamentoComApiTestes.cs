using Jogo.Dados.Reais;
using Jogo.Dominio.Jogadores;
using Jogo.Dominio.Reais;

namespace Jogo.Dados.Testes.Reais;

public class CruzamentoComApiTestes
{
    private static JogadorReal Nosso(string nome, Posicao? posicao, GrupoDePosicao grupo, decimal? valor = null, int? idade = null) =>
        new(nome, posicao, grupo, idade, valor);

    [Theory]
    [InlineData("Agustín Rossi", "A. Rossi", true)]
    [InlineData("Giorgian De Arrascaeta", "G. De Arrascaeta", true)]
    [InlineData("Léo Ortiz", "Leo Ortiz", true)]
    [InlineData("Danilo", "Danilo", true)]
    [InlineData("Andrew Ventura", "Andrew", true)]
    [InlineData("Dyogo Alves", "Dyogo", true)]
    [InlineData("Léo Pereira", "L. Ortiz", false)]
    [InlineData("Pedro", "Pedro Henrique", true)]
    [InlineData("Ana", "Ana Souza", false)]
    public void Parecido_InicialESobrenome(string nosso, string api, bool esperado)
    {
        Assert.Equal(esperado, CruzamentoComApi.Igual(nosso, api) || CruzamentoComApi.Parecido(nosso, api));
    }

    [Fact]
    public void Cruzar_ApiMandaNoElencoENossosDadosCompletam()
    {
        IReadOnlyList<JogadorReal> nossos =
        [
            Nosso("Agustín Rossi", Posicao.Goleiro, GrupoDePosicao.Goleiro, 1_000_000m, 31),
            Nosso("Léo Ortiz", Posicao.Zagueiro, GrupoDePosicao.Defensor, 13_000_000m),
            Nosso("Saiu Silva", Posicao.MeiaCentral, GrupoDePosicao.Meia, 500_000m),
        ];
        IReadOnlyList<JogadorApi> daApi =
        [
            new(1, "A. Rossi", 30, GrupoDePosicao.Goleiro),
            new(2, "Leo Ortiz", 29, GrupoDePosicao.Defensor),
            new(3, "Novo Reforço", 22, GrupoDePosicao.Atacante),
            new(4, "Garoto da Base", 16, GrupoDePosicao.Meia),
        ];

        var r = CruzamentoComApi.Cruzar(nossos, daApi);

        Assert.Equal(["Agustín Rossi", "Léo Ortiz", "Novo Reforço"], r.Elenco.Select(j => j.Nome));
        Assert.Equal((30, 1_000_000m, Posicao.Goleiro), (r.Elenco[0].Idade!.Value, r.Elenco[0].ValorEmEuros!.Value, r.Elenco[0].Posicao!.Value));
        Assert.Equal((Posicao.Zagueiro, 13_000_000m), (r.Elenco[1].Posicao!.Value, r.Elenco[1].ValorEmEuros!.Value));
        Assert.Null(r.Elenco[2].ValorEmEuros);
        Assert.Equal(GrupoDePosicao.Atacante, r.Elenco[2].Grupo);
        Assert.Equal(["Novo Reforço"], r.Novos);
        Assert.Equal(["Saiu Silva"], r.Sairam);
    }

    [Fact]
    public void Cruzar_ApelidoAmbiguo_NaoCasa()
    {
        var r = CruzamentoComApi.Cruzar(
            [Nosso("Pedro Henrique", Posicao.PontaEsquerda, GrupoDePosicao.Atacante, 1_000_000m), Nosso("Pedro Rocha", Posicao.Centroavante, GrupoDePosicao.Atacante, 2_000_000m)],
            [new(1, "Pedro", 29, GrupoDePosicao.Atacante)]);

        Assert.Equal(["Pedro"], r.Novos);
        Assert.Equal(2, r.Sairam.Count);
    }

    [Fact]
    public void Cruzar_NomeExatoTemPrioridadeSobreApelido()
    {
        var r = CruzamentoComApi.Cruzar(
            [Nosso("Higor Meritão", Posicao.Volante, GrupoDePosicao.Meia, 800_000m), Nosso("Higor", Posicao.Zagueiro, GrupoDePosicao.Defensor)],
            [new(1, "Higor", 21, GrupoDePosicao.Defensor), new(2, "Higor Meritão", 32, GrupoDePosicao.Meia)]);

        Assert.Empty(r.Novos);
        Assert.Equal(800_000m, r.Elenco.Single(j => j.Nome == "Higor Meritão").ValorEmEuros);
    }

    [Fact]
    public void Cruzar_GrupoDiferenteNaApi_UsaOGrupoDaApi()
    {
        var r = CruzamentoComApi.Cruzar(
            [Nosso("Danilo", Posicao.Zagueiro, GrupoDePosicao.Defensor)],
            [new(1, "Danilo", 35, GrupoDePosicao.Meia)]);

        Assert.Null(r.Elenco[0].Posicao);
        Assert.Equal(GrupoDePosicao.Meia, r.Elenco[0].Grupo);
    }

    [Fact]
    public void Cruzar_ApiVazia_MantemOElencoPesquisado()
    {
        IReadOnlyList<JogadorReal> nossos = [Nosso("Fulano", Posicao.Centroavante, GrupoDePosicao.Atacante)];

        var r = CruzamentoComApi.Cruzar(nossos, []);

        Assert.Same(nossos, r.Elenco);
        Assert.Empty(r.Sairam);
    }
}
