using Jogo.Dominio;
using Jogo.Dominio.Aleatoriedade;
using Jogo.Dominio.Clubes;
using Jogo.Simulacao.Geracao;
using Jogo.Simulacao.Mercado;

namespace Jogo.Simulacao.Testes;

public class TransferenciasTestes
{
    private static Mundo NovoMundo() => GeradorDeMundo.Gerar(2027, 2027, CatalogoBase.Clubes);

    [Theory]
    [InlineData(0, true)]
    [InlineData(2, true)]
    [InlineData(3, false)]
    [InlineData(16, false)]
    [InlineData(17, true)]
    [InlineData(20, true)]
    [InlineData(21, false)]
    public void JanelaAberta_PreTemporadaEMeioDoAno(int rodadas, bool aberta)
    {
        Assert.Equal(aberta, Transferencias.JanelaAberta(rodadas));
    }

    [Fact]
    public void Comprar_PagaOPrecoAoVendedorEAssinaContrato()
    {
        var mundo = NovoMundo();
        var comprador = mundo.Clube("pampa");
        comprador.MovimentarCaixa(100_000_000m);
        var vendedor = mundo.Clubes.First(c => c != comprador);
        var jogador = vendedor.Elenco.OrderBy(j => j.ForcaGeral).First();
        var preco = Transferencias.Preco(jogador, 2027);
        var (caixaComprador, caixaVendedor) = (comprador.Caixa, vendedor.Caixa);

        Transferencias.Comprar(mundo, comprador, jogador, 0);

        Assert.Contains(jogador, comprador.Elenco);
        Assert.DoesNotContain(jogador, vendedor.Elenco);
        Assert.Equal(comprador.Id, jogador.Contrato!.ClubeId);
        Assert.Equal(caixaComprador - preco, comprador.Caixa);
        Assert.Equal(caixaVendedor + preco, vendedor.Caixa);
        Assert.Equal(-preco, comprador.Extrato[CategoriaFinanceira.CompraDeJogadores]);
    }

    [Fact]
    public void Comprar_JanelaFechada_LancaMasAgenteLivrePode()
    {
        var mundo = NovoMundo();
        var comprador = mundo.Clube("pampa");
        var outro = mundo.Clubes.First(c => c != comprador);
        var alvo = outro.Elenco[0];
        var livre = outro.Elenco[1];
        Contratos.Dispensar(mundo, outro, livre);

        Assert.Throws<InvalidOperationException>(() => Transferencias.Comprar(mundo, comprador, alvo, 10));
        Transferencias.Comprar(mundo, comprador, livre, 10);

        Assert.Contains(livre, comprador.Elenco);
        Assert.Empty(mundo.AgentesLivres);
    }

    [Fact]
    public void Comprar_SemDinheiro_Lanca()
    {
        var mundo = NovoMundo();
        var comprador = mundo.Clube("pampa");
        comprador.MovimentarCaixa(-comprador.Caixa);
        var alvo = mundo.Clubes.First(c => c != comprador).Elenco.MaxBy(j => j.ForcaGeral)!;

        var erro = Assert.Throws<InvalidOperationException>(() => Transferencias.Comprar(mundo, comprador, alvo, 0));
        Assert.Contains("dinheiro", erro.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Vender_VaiParaAMelhorOferta()
    {
        var mundo = NovoMundo();
        var vendedor = mundo.Clube("pampa");
        // O melhor jogador que algum clube consegue pagar.
        var jogador = vendedor.Elenco.OrderByDescending(j => j.ForcaGeral).ThenBy(j => j.Id)
            .First(j => Transferencias.MelhorOferta(mundo, vendedor, j) is not null);
        var oferta = Transferencias.MelhorOferta(mundo, vendedor, jogador);
        Assert.NotNull(oferta);
        var caixa = vendedor.Caixa;

        var feita = Transferencias.Vender(mundo, vendedor, jogador, 0);

        Assert.Equal(oferta, feita);
        Assert.Contains(jogador, feita.Clube.Elenco);
        Assert.Equal(caixa + feita.Valor, vendedor.Caixa);
    }

    [Fact]
    public void MovimentarIa_NaoMexeNoClubeDoJogadorEEDeterministico()
    {
        static (int Negocios, string Elencos) Rodar()
        {
            var mundo = NovoMundo();
            var antes = mundo.Clube("pampa").Elenco.Select(j => j.Id).ToArray();
            var negocios = 0;
            for (var r = 0; r < 3; r++)
            {
                negocios += Transferencias.MovimentarIa(mundo, "pampa", r, new GeradorAleatorio(42 + (ulong)r)).Count;
            }

            Assert.Equal(antes, mundo.Clube("pampa").Elenco.Select(j => j.Id));
            return (negocios, string.Join(";", mundo.Clubes.Select(c => string.Join(",", c.Elenco.Select(j => j.Id)))));
        }

        var a = Rodar();
        Assert.True(a.Negocios > 0);
        Assert.Equal(a, Rodar());
    }

    [Fact]
    public void NovaCarreira_ComecaComAgentesLivresDeTodasAsPosicoes()
    {
        var carreira = new Carreira(NovoMundo(), "pampa", 3);

        Assert.Equal(AgentesLivresIniciais.Quantidade, carreira.Mundo.AgentesLivres.Count);
        Assert.All(carreira.Mundo.AgentesLivres, j => Assert.Null(j.Contrato));
        Assert.Equal(GeradorDeMundo.ElencoBase.Distinct().Count(), carreira.Mundo.AgentesLivres.Select(j => j.PosicaoPrincipal).Distinct().Count());
    }

    [Fact]
    public void Carreira_TresTemporadas_EconomiaEElencosSaudaveis()
    {
        var carreira = new Carreira(NovoMundo(), "pampa", 9);
        for (var t = 0; t < 3; t++)
        {
            while (!carreira.Temporada.Encerrada)
            {
                carreira.AvancarRodada();
            }

            carreira.EncerrarTemporada();
        }

        var mundo = carreira.Mundo;
        Assert.All(mundo.Clubes, c => Assert.InRange(c.Elenco.Count, 20, Transferencias.ElencoMaximo + 3));
        Assert.True(mundo.Clubes.Count(c => c.Caixa < 0) <= mundo.Clubes.Count / 5);
    }
}
