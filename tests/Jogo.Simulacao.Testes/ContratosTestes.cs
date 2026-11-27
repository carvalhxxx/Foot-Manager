using Jogo.Dominio;
using Jogo.Dominio.Clubes;
using Jogo.Dominio.Jogadores;
using Jogo.Simulacao.Geracao;
using Jogo.Simulacao.Mercado;
using Jogo.Simulacao.Temporadas;

namespace Jogo.Simulacao.Testes;

public class ContratosTestes
{
    private static Mundo NovoMundo() => GeradorDeMundo.Gerar(2027, 2027, CatalogoBase.Clubes);

    private static Jogador ComContratoVencendo(Clube clube, int ano)
    {
        var jogador = clube.Elenco.OrderByDescending(j => j.ForcaGeral).First();
        jogador.AssinarContrato(new Contrato(clube.Id, jogador.Contrato!.SalarioMensal, ano));
        return jogador;
    }

    [Fact]
    public void ValorDeMercado_MaisForteValeMais()
    {
        var elenco = NovoMundo().Clube("pampa").Elenco;
        var forte = elenco.MaxBy(j => j.ForcaGeral)!;
        var fraco = elenco.MinBy(j => j.ForcaGeral)!;

        Assert.True(Valores.ValorDeMercado(forte, 2027) > Valores.ValorDeMercado(fraco, 2027));
    }

    [Fact]
    public void ValorDeMercado_ContratoNoFimValeMenos()
    {
        var clube = NovoMundo().Clube("pampa");
        var jogador = clube.Elenco[0];
        jogador.AssinarContrato(new Contrato(clube.Id, 10_000m, 2030));
        var longo = Valores.ValorDeMercado(jogador, 2027);
        jogador.AssinarContrato(new Contrato(clube.Id, 10_000m, 2027));

        Assert.True(Valores.ValorDeMercado(jogador, 2027) < longo);
    }

    [Fact]
    public void Renovar_UltimoAno_AssinaPeloSalarioPedido()
    {
        var clube = NovoMundo().Clube("pampa");
        var jogador = ComContratoVencendo(clube, 2027);
        var pedido = Valores.SalarioPedido(jogador);

        Contratos.Renovar(clube, jogador, 2027, 3);

        Assert.Equal(2030, jogador.Contrato!.TemporadaFinal);
        Assert.Equal(pedido, jogador.Contrato.SalarioMensal);
    }

    [Fact]
    public void Renovar_ContratoLongo_Lanca()
    {
        var clube = NovoMundo().Clube("pampa");
        var jogador = clube.Elenco[0];
        jogador.AssinarContrato(new Contrato(clube.Id, 10_000m, 2029));

        Assert.Throws<InvalidOperationException>(() => Contratos.Renovar(clube, jogador, 2027, 2));
    }

    [Fact]
    public void Renovar_AnosForaDaFaixa_Lanca()
    {
        var clube = NovoMundo().Clube("pampa");
        var jogador = ComContratoVencendo(clube, 2027);

        Assert.Throws<ArgumentOutOfRangeException>(() => Contratos.Renovar(clube, jogador, 2027, 5));
    }

    [Fact]
    public void Dispensar_PagaTresSalariosEViraAgenteLivre()
    {
        var mundo = NovoMundo();
        var clube = mundo.Clube("pampa");
        var jogador = clube.Elenco[0];
        var salario = jogador.Contrato!.SalarioMensal;
        var caixa = clube.Caixa;

        Contratos.Dispensar(mundo, clube, jogador);

        Assert.DoesNotContain(jogador, clube.Elenco);
        Assert.Contains(jogador, mundo.AgentesLivres);
        Assert.Null(jogador.Contrato);
        Assert.Equal(caixa - (3 * salario), clube.Caixa);
        Assert.Equal(-3 * salario, clube.Extrato[CategoriaFinanceira.Rescisoes]);
    }

    [Fact]
    public void FimDeTemporada_ClubeDoJogadorPerdeQuemNaoRenovou()
    {
        var mundo = NovoMundo();
        var clube = mundo.Clube("pampa");
        var jogador = ComContratoVencendo(clube, 2027);
        var temporada = new Temporada(mundo, 1);
        temporada.JogarAteOFim();

        FimDeTemporada.Encerrar(temporada, mundo, 1, clubeDoJogador: "pampa");

        Assert.DoesNotContain(jogador, clube.Elenco);
        Assert.Contains(jogador, mundo.AgentesLivres);
    }

    [Fact]
    public void FimDeTemporada_IaRenovaQuemEUtil()
    {
        var mundo = NovoMundo();
        var clube = mundo.Clube("pampa");
        var jogador = ComContratoVencendo(clube, 2027);
        var temporada = new Temporada(mundo, 1);
        temporada.JogarAteOFim();

        FimDeTemporada.Encerrar(temporada, mundo, 1);

        if (jogador.Idade <= 33)
        {
            Assert.Contains(jogador, clube.Elenco);
            Assert.True(jogador.Contrato!.TemporadaFinal > 2027);
        }
    }

    [Fact]
    public void FimDeTemporada_TodosContratosValidosEElencosCompletos()
    {
        var mundo = NovoMundo();
        for (var ano = 0; ano < 3; ano++)
        {
            var temporada = new Temporada(mundo, (ulong)ano + 5);
            temporada.JogarAteOFim();
            FimDeTemporada.Encerrar(temporada, mundo, (ulong)ano + 5, clubeDoJogador: "pampa");
        }

        Assert.All(mundo.Clubes.SelectMany(c => c.Elenco), j => Assert.True(j.Contrato!.TemporadaFinal >= mundo.TemporadaAtual));
        Assert.All(mundo.Clubes, c => Assert.InRange(c.Elenco.Count, FimDeTemporada.ElencoMinimo, FimDeTemporada.ElencoMaximo + 5));
        Assert.NotEmpty(mundo.AgentesLivres);
        Assert.InRange(mundo.AgentesLivres.Count, 1, Contratos.MaximoDeAgentesLivres);
    }
}
