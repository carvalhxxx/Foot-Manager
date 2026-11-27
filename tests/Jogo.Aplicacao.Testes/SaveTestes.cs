using System.Text.Json.Nodes;
using Jogo.Aplicacao.Saves;
using Jogo.Dominio;
using Jogo.Motor;
using Jogo.Simulacao;
using Jogo.Simulacao.Geracao;

namespace Jogo.Aplicacao.Testes;

public class SaveTestes
{
    private static Mundo NovoMundo() => GeradorDeMundo.Gerar(2027, 2027, CatalogoBase.Clubes);

    private static void JogarRodadas(Carreira carreira, int quantidade)
    {
        for (var i = 0; i < quantidade; i++)
        {
            if (carreira.Temporada.Encerrada)
            {
                carreira.EncerrarTemporada();
            }

            carreira.AvancarRodada();
        }
    }

    /// <summary>
    /// A garantia principal: salvar no meio e continuar dá exatamente o mesmo jogo que jogar direto,
    /// inclusive atravessando o fim de temporada (acesso, evolução, aposentadoria, base).
    /// </summary>
    [Fact]
    public void SalvarNoMeioECarregar_ContinuaIgualAJogarDireto()
    {
        var direto = new Carreira(NovoMundo(), "cerrado", 77);
        var comSave = new Carreira(NovoMundo(), "cerrado", 77);
        comSave.DefinirTatica(comSave.Tatica with { Mentalidade = Mentalidade.Ofensiva });
        direto.DefinirTatica(direto.Tatica with { Mentalidade = Mentalidade.Ofensiva });

        JogarRodadas(direto, 45);

        JogarRodadas(comSave, 12);
        var retomada = ConversorSave.Carregar(ConversorSave.Salvar(comSave));
        JogarRodadas(retomada, 20);
        retomada = ConversorSave.Carregar(ConversorSave.Salvar(retomada));
        JogarRodadas(retomada, 13);

        Assert.Equal(2028, retomada.Mundo.TemporadaAtual);
        Assert.Equal(direto.Temporada.Tabela("serie-a"), retomada.Temporada.Tabela("serie-a"));
        Assert.Equal(ConversorSave.Salvar(direto), ConversorSave.Salvar(retomada));
    }

    [Fact]
    public void SalvarCarregarSalvar_TextoIdentico()
    {
        var carreira = new Carreira(NovoMundo(), "pampa", 3);
        JogarRodadas(carreira, 5);

        var primeiro = ConversorSave.Salvar(carreira);
        var segundo = ConversorSave.Salvar(ConversorSave.Carregar(primeiro));

        Assert.Equal(primeiro, segundo);
    }

    [Fact]
    public void Carregar_RestauraEstadoDeCarreiraDosJogadores()
    {
        var carreira = new Carreira(NovoMundo(), "pampa", 3);
        JogarRodadas(carreira, 8);
        var antes = carreira.Mundo.Clubes.SelectMany(c => c.Elenco)
            .Select(j => (j.Id, j.Condicao, j.Moral, j.RodadasLesionado, j.RodadasSuspenso, j.AmarelosAcumulados, j.SequenciaComoTitular, j.Atributos))
            .ToArray();

        var carregada = ConversorSave.Carregar(ConversorSave.Salvar(carreira));

        Assert.Equal(antes, carregada.Mundo.Clubes.SelectMany(c => c.Elenco)
            .Select(j => (j.Id, j.Condicao, j.Moral, j.RodadasLesionado, j.RodadasSuspenso, j.AmarelosAcumulados, j.SequenciaComoTitular, j.Atributos)));
        Assert.Equal(8, carregada.Temporada.RodadaAtual);
        Assert.Equal(carreira.Temporada.Artilharia("serie-a"), carregada.Temporada.Artilharia("serie-a"));
        Assert.Equal(carreira.Tatica.Titulares, carregada.Tatica.Titulares);
    }

    [Fact]
    public void Carregar_ReconstroiAUltimaRodadaComOsLancesDoMeuJogo()
    {
        var carreira = new Carreira(NovoMundo(), "pampa", 3);
        JogarRodadas(carreira, 3);
        var meuJogoAntes = carreira.UltimaRodada!.JogosPorDivisao["serie-a"].Single(j => j.Resultado.MandanteId == "pampa" || j.Resultado.VisitanteId == "pampa");

        var carregada = ConversorSave.Carregar(ConversorSave.Salvar(carreira));

        var meuJogo = carregada.UltimaRodada!.JogosPorDivisao["serie-a"].Single(j => j.Resultado.MandanteId == "pampa" || j.Resultado.VisitanteId == "pampa");
        Assert.Equal(3, carregada.UltimaRodada.Rodada);
        Assert.Equal(meuJogoAntes.Resultado.Eventos, meuJogo.Resultado.Eventos);
        Assert.Equal(meuJogoAntes.Resultado.PosseMandante, meuJogo.Resultado.PosseMandante);
    }

    [Fact]
    public void Carregar_MantemAgentesLivres()
    {
        var carreira = new Carreira(NovoMundo(), "cerrado", 5);
        var dispensado = carreira.Clube.Elenco[0];
        carreira.Dispensar(dispensado.Id);

        var carregada = ConversorSave.Carregar(ConversorSave.Salvar(carreira));

        var livre = Assert.Single(carregada.Mundo.AgentesLivres);
        Assert.Equal((dispensado.Id, dispensado.Nome, dispensado.ForcaGeral), (livre.Id, livre.Nome, livre.ForcaGeral));
        Assert.Null(livre.Contrato);
    }

    [Fact]
    public void Save_FicaPequeno()
    {
        var carreira = new Carreira(NovoMundo(), "pampa", 3);
        JogarRodadas(carreira, 38);

        var tamanho = ConversorSave.Salvar(carreira).Length;

        Assert.True(tamanho < 900_000, $"save com {tamanho / 1024} KB");
    }

    [Fact]
    public void Salvar_NoMeioDoJogoAoVivo_Lanca()
    {
        var carreira = new Carreira(NovoMundo(), "pampa", 3);
        carreira.IniciarRodadaAoVivo();

        Assert.Throws<InvalidOperationException>(() => ConversorSave.Salvar(carreira));
    }

    [Theory]
    [InlineData("isto não é json")]
    [InlineData("{}")]
    [InlineData("{\"versaoFormato\": 1}")]
    public void Carregar_ArquivoInvalido_MensagemClara(string conteudo)
    {
        var erro = Assert.Throws<SaveInvalidoException>(() => ConversorSave.Carregar(conteudo));

        Assert.Contains("save válido", erro.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Carregar_VersaoMaisNova_PedeParaAtualizar()
    {
        var no = JsonNode.Parse(ConversorSave.Salvar(new Carreira(NovoMundo(), "pampa", 3)))!;
        no["versaoFormato"] = 99;

        var erro = Assert.Throws<SaveInvalidoException>(() => ConversorSave.Carregar(no.ToJsonString()));

        Assert.Contains("versão mais nova", erro.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Carregar_JogoQueNaoBateComOCalendario_Corrompido()
    {
        var carreira = new Carreira(NovoMundo(), "pampa", 3);
        JogarRodadas(carreira, 2);
        var no = JsonNode.Parse(ConversorSave.Salvar(carreira))!;
        no["temporada"]!["jogos"]![0]!["m"] = "jalapao";

        var erro = Assert.Throws<SaveInvalidoException>(() => ConversorSave.Carregar(no.ToJsonString()));

        Assert.Contains("corrompido", erro.Message, StringComparison.Ordinal);
    }
}
