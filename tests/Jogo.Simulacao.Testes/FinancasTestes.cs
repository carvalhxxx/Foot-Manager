using Jogo.Dominio.Clubes;
using Jogo.Simulacao.Financas;
using Jogo.Simulacao.Geracao;
using Jogo.Simulacao.Temporadas;

namespace Jogo.Simulacao.Testes;

public class FinancasTestes
{
    private static readonly RegrasFinanceiras Regras = RegrasFinanceiras.Padrao;

    private static Clube Clube(int torcida = 60, int reputacao = 60, int capacidade = 40_000) =>
        new("clube", "Clube", "CLU", "Recife", "PE", new Cores("#000000", "#FFFFFF"),
            new Estadio("Estádio", capacidade), torcida, reputacao, 0m);

    [Fact]
    public void Publico_IngressoMaisCaroDiminui()
    {
        var mandante = Clube();
        var visitante = Clube();
        var sugerido = Financas.Financas.PrecoSugerido(mandante, Regras);

        var barato = Financas.Financas.Publico(mandante, visitante, sugerido / 2, 0.5, Regras);
        var normal = Financas.Financas.Publico(mandante, visitante, sugerido, 0.5, Regras);
        var caro = Financas.Financas.Publico(mandante, visitante, sugerido * 2, 0.5, Regras);

        Assert.True(barato > normal && normal > caro);
    }

    [Fact]
    public void Publico_NuncaPassaDaCapacidade()
    {
        var mandante = Clube(torcida: 100, capacidade: 5_000);

        Assert.Equal(5_000, Financas.Financas.Publico(mandante, Clube(torcida: 100), 1m, 1.0, Regras));
    }

    [Fact]
    public void Publico_BoaFaseEAdversarioGrandeLotamMais()
    {
        var mandante = Clube();
        var preco = Financas.Financas.PrecoSugerido(mandante, Regras);

        Assert.True(Financas.Financas.Publico(mandante, Clube(), preco, 1.0, Regras) > Financas.Financas.Publico(mandante, Clube(), preco, 0.0, Regras));
        Assert.True(Financas.Financas.Publico(mandante, Clube(torcida: 95), preco, 0.5, Regras) > Financas.Financas.Publico(mandante, Clube(torcida: 20), preco, 0.5, Regras));
    }

    [Fact]
    public void Premio_CampeaoLevaMaisQueOUltimo()
    {
        var serieA = new Dominio.Competicoes.Divisao("a", "A", 1, ["x", "y"], 0, 0);

        Assert.Equal(Regras.PremioCampeao, Financas.Financas.Premio(1, 20, serieA, Regras));
        Assert.True(Financas.Financas.Premio(20, 20, serieA, Regras) < Financas.Financas.Premio(10, 20, serieA, Regras));
    }

    [Fact]
    public void Rodada_LancaReceitasESalariosNoExtrato()
    {
        var mundo = GeradorDeMundo.Gerar(2027, 2027, CatalogoBase.Clubes);
        var temporada = new Temporada(mundo, 1);
        var clube = mundo.Clube("pampa");
        var caixaAntes = clube.Caixa;

        temporada.JogarRodada();

        Assert.True(clube.Extrato[CategoriaFinanceira.CotaDeTv] > 0);
        Assert.True(clube.Extrato[CategoriaFinanceira.Patrocinio] > 0);
        Assert.True(clube.Extrato[CategoriaFinanceira.Salarios] < 0);
        Assert.Equal(caixaAntes + clube.Extrato.Values.Sum(), clube.Caixa);
    }

    [Fact]
    public void Temporada_MandanteRecebeBilheteriaComPrecoDoClube()
    {
        var mundo = GeradorDeMundo.Gerar(2027, 2027, CatalogoBase.Clubes);
        var temporada = new Temporada(mundo, 1);
        var mandante = mundo.Clube(temporada.Competicao("serie-a").Calendario[0][0].MandanteId);
        mandante.DefinirPrecoIngresso(80m);

        temporada.JogarRodada();

        var (publico, renda) = mandante.UltimoJogoEmCasa!.Value;
        Assert.Equal(publico * 80m, renda);
        Assert.Equal(renda, mandante.Extrato[CategoriaFinanceira.Bilheteria]);
    }

    [Fact]
    public void FimDeTemporada_PagaPremioEZeraOExtrato()
    {
        var mundo = GeradorDeMundo.Gerar(2027, 2027, CatalogoBase.Clubes);
        var temporada = new Temporada(mundo, 1);
        temporada.JogarAteOFim();
        var campeao = temporada.Tabela("serie-a")[0].ClubeId;

        var resultado = FimDeTemporada.Encerrar(temporada, mundo, 1);

        Assert.Equal(Regras.PremioCampeao, resultado.Extratos[campeao][CategoriaFinanceira.Premiacao]);
        Assert.Empty(mundo.Clube(campeao).Extrato);
    }

    [Fact]
    public void PrecoIngresso_ForaDaFaixa_Lanca()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Clube().DefinirPrecoIngresso(0m));
    }
}
