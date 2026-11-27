using System.Text.Json;
using System.Text.Json.Nodes;

namespace Jogo.Aplicacao.Testes;

/// <summary>Testa a API exatamente como o JavaScript a usa: texto JSON entrando e saindo.</summary>
public class ApiJsonTestes
{
    private static readonly string ArquivosDaBase = JsonSerializer.Serialize(LerBase());

    private static Dictionary<string, string> LerBase()
    {
        var raiz = new DirectoryInfo(AppContext.BaseDirectory);
        while (!File.Exists(Path.Combine(raiz!.FullName, "FootManagerBR.slnx")))
        {
            raiz = raiz.Parent;
        }

        var pasta = Path.Combine(raiz.FullName, "dados", "definicoes");
        return Directory.GetFiles(pasta, "*.json", SearchOption.AllDirectories)
            .ToDictionary(f => Path.GetRelativePath(pasta, f).Replace('\\', '/'), File.ReadAllText);
    }

    private static JsonNode Ok(string resposta)
    {
        var no = JsonNode.Parse(resposta)!;
        Assert.True(no["ok"]!.GetValue<bool>(), resposta.Length > 400 ? resposta[..400] : resposta);
        return no["dados"]!;
    }

    private static string Falha(string resposta)
    {
        var no = JsonNode.Parse(resposta)!;
        Assert.False(no["ok"]!.GetValue<bool>());
        return no["erros"]![0]!["mensagem"]!.GetValue<string>();
    }

    private static ApiJson ComCarreira(string clube = "pampa")
    {
        var api = new ApiJson();
        Ok(api.CarregarDefinicoes(ArquivosDaBase));
        Ok(api.NovaCarreira(clube, "42"));
        return api;
    }

    [Fact]
    public void CarregarDefinicoes_DevolveResumoEmCamelCase()
    {
        var api = new ApiJson();

        var dados = Ok(api.CarregarDefinicoes(ArquivosDaBase));

        Assert.Equal(2027, dados["temporada"]!.GetValue<int>());
        Assert.Equal(2, dados["divisoes"]!.AsArray().Count);
        Assert.Equal(20, dados["divisoes"]![0]!["clubes"]!.AsArray().Count);
    }

    [Fact]
    public void EntradaMalformada_ViraErroSemExcecao()
    {
        var api = new ApiJson();

        Falha(api.CarregarDefinicoes("{isto não é json"));
        Falha(api.NovaCarreira("pampa", "abc"));
        Assert.Contains("Nenhuma carreira", Falha(api.Estado()), StringComparison.Ordinal);
    }

    [Fact]
    public void NovaCarreira_ClubeInexistente_Falha()
    {
        var api = new ApiJson();
        Ok(api.CarregarDefinicoes(ArquivosDaBase));

        Assert.Contains("não existe", Falha(api.NovaCarreira("nao-existe", "1")), StringComparison.Ordinal);
    }

    [Fact]
    public void Estado_NoComeco_MostraProximoJogoEPosicao()
    {
        var api = ComCarreira();

        var estado = Ok(api.Estado());

        Assert.Equal((2027, 0, 38), (estado["ano"]!.GetValue<int>(), estado["rodadaAtual"]!.GetValue<int>(), estado["totalRodadas"]!.GetValue<int>()));
        Assert.Equal("pampa", estado["clube"]!["id"]!.GetValue<string>());
        Assert.Equal("serie-a", estado["divisaoId"]!.GetValue<string>());
        Assert.Equal(1, estado["proximoJogo"]!["rodada"]!.GetValue<int>());
        Assert.Empty(estado["ultimosResultados"]!.AsArray());
    }

    [Fact]
    public void Elenco_TrazJogadoresComStatusEAtributos()
    {
        var api = ComCarreira();

        var elenco = Ok(api.Elenco()).AsArray();

        Assert.InRange(elenco.Count, 25, 30);
        var primeiro = elenco[0]!;
        Assert.Equal("GOL", primeiro["posicao"]!.GetValue<string>());
        Assert.True(primeiro["disponivel"]!.GetValue<bool>());
        Assert.InRange(primeiro["atributos"]!["reflexos"]!.GetValue<int>(), 1, 20);
    }

    [Fact]
    public void Tatica_DefinirValidaEInvalida()
    {
        var api = ComCarreira();
        var tatica = Ok(api.Tatica());
        var titulares = tatica["titulares"]!.AsArray().Select(t => t!["jogadorId"]!.GetValue<int>()).ToArray();
        Assert.Equal(11, titulares.Length);
        Assert.Equal(7, tatica["formacoes"]!.AsArray().Count);
        Assert.Equal(
            ["MuitoDefensiva", "Defensiva", "Equilibrada", "Ofensiva", "MuitoOfensiva"],
            tatica["mentalidades"]!.AsArray().Select(m => m!.GetValue<string>()));

        var nova = JsonSerializer.Serialize(new { formacao = "4-4-2", mentalidade = "Ofensiva", titulares, reservas = Array.Empty<int>() });
        var definida = Ok(api.DefinirTatica(nova));
        Assert.Equal(("4-4-2", "Ofensiva"), (definida["formacao"]!.GetValue<string>(), definida["mentalidade"]!.GetValue<string>()));

        var poucos = JsonSerializer.Serialize(new { formacao = "4-4-2", mentalidade = "Ofensiva", titulares = titulares[..10], reservas = Array.Empty<int>() });
        Assert.Contains("11 titulares", Falha(api.DefinirTatica(poucos)), StringComparison.Ordinal);

        var semFormacao = JsonSerializer.Serialize(new { formacao = "9-9-9", mentalidade = "Ofensiva", titulares, reservas = Array.Empty<int>() });
        Assert.Contains("Formação", Falha(api.DefinirTatica(semFormacao)), StringComparison.Ordinal);
    }

    [Fact]
    public void AvancarRodada_DevolveMinhaPartidaEOsOutrosJogos()
    {
        var api = ComCarreira();

        var rodada = Ok(api.AvancarRodada());

        Assert.Equal(1, rodada["rodada"]!.GetValue<int>());
        var minha = rodada["minhaPartida"]!;
        Assert.True(minha["mandante"]!["id"]!.GetValue<string>() == "pampa" || minha["visitante"]!["id"]!.GetValue<string>() == "pampa");
        Assert.Contains(minha["eventos"]!.AsArray(), e => e!["tipo"]!.GetValue<string>() == "FimDeJogo");
        Assert.Equal(10, rodada["divisoes"]![0]!["jogos"]!.AsArray().Count);
        Assert.Equal(1, Ok(api.Estado())["rodadaAtual"]!.GetValue<int>());
    }

    [Fact]
    public void AoVivo_PausaNoIntervaloDecideEConclui()
    {
        var api = ComCarreira();

        var inicio = Ok(api.IniciarAoVivo());
        Assert.Equal(1, inicio["rodada"]!.GetValue<int>());
        Assert.Equal(0, inicio["minhaPartida"]!["minuto"]!.GetValue<int>());
        Assert.True(Ok(api.Estado())["rodadaAoVivo"]!.GetValue<bool>());

        // Pede muitos minutos: tem que parar sozinho no intervalo.
        var intervalo = Ok(api.AvancarAoVivo(200));
        Assert.Equal("intervalo", intervalo["pausa"]!.GetValue<string>());
        Assert.Equal(45, intervalo["minhaPartida"]!["minuto"]!.GetValue<int>());
        Assert.Equal(2, intervalo["placares"]!.AsArray().Count);

        var minha = intervalo["minhaPartida"]!;
        var sai = minha["emCampo"]!.AsArray().First(j => j!["posicao"]!.GetValue<string>() != "GOL")!["id"]!.GetValue<int>();
        var entra = minha["reservas"]!.AsArray().First(j => j!["posicao"]!.GetValue<string>() != "GOL")!["id"]!.GetValue<int>();

        var depois = Ok(api.DecidirAoVivo(JsonSerializer.Serialize(new { tipo = "substituicao", saiId = sai, entraId = entra })));
        Assert.Equal(4, depois["minhaPartida"]!["substituicoesRestantes"]!.GetValue<int>());

        var formacao = Ok(api.DecidirAoVivo(JsonSerializer.Serialize(new { tipo = "formacao", formacao = "3-5-2" })));
        Assert.Equal("3-5-2", formacao["minhaPartida"]!["formacao"]!.GetValue<string>());

        var mentalidade = Ok(api.DecidirAoVivo(JsonSerializer.Serialize(new { tipo = "mentalidade", mentalidade = "MuitoOfensiva" })));
        Assert.Equal("MuitoOfensiva", mentalidade["minhaPartida"]!["mentalidade"]!.GetValue<string>());

        Assert.Contains("desconhecida", Falha(api.DecidirAoVivo(JsonSerializer.Serialize(new { tipo = "magica" }))), StringComparison.Ordinal);
        Assert.Contains("rodada em andamento", Falha(api.AvancarRodada()), StringComparison.Ordinal);

        var rodada = Ok(api.ConcluirAoVivo());
        Assert.Equal(1, rodada["rodada"]!.GetValue<int>());
        Assert.Contains(rodada["minhaPartida"]!["eventos"]!.AsArray(), e => e!["tipo"]!.GetValue<string>() == "Substituicao");
        var estado = Ok(api.Estado());
        Assert.Equal((1, false), (estado["rodadaAtual"]!.GetValue<int>(), estado["rodadaAoVivo"]!.GetValue<bool>()));
    }

    [Fact]
    public void Save_ExportarECarregarEmOutraInstancia()
    {
        var api = ComCarreira("cerrado");
        Ok(api.AvancarRodada());
        Ok(api.AvancarRodada());

        var save = Ok(api.ExportarSave());
        Assert.Equal(("cerrado", 2027, 2, "Série A"),
            (save["clubeId"]!.GetValue<string>(), save["ano"]!.GetValue<int>(), save["rodada"]!.GetValue<int>(), save["divisao"]!.GetValue<string>()));

        // Outra aba/aparelho: sem carregar definições, só o save.
        var outra = new ApiJson();
        var estado = Ok(outra.CarregarSave(save["conteudo"]!.GetValue<string>()));
        Assert.Equal(("cerrado", 2), (estado["clube"]!["id"]!.GetValue<string>(), estado["rodadaAtual"]!.GetValue<int>()));
        Assert.Equal(2, Ok(outra.UltimaRodada())["rodada"]!.GetValue<int>());
        Assert.Equal(3, Ok(outra.AvancarRodada())["rodada"]!.GetValue<int>());
    }

    [Fact]
    public void Financas_ExtratoEPrecoDoIngresso()
    {
        var api = ComCarreira();
        Ok(api.AvancarRodada());

        var financas = Ok(api.Financas());
        Assert.Contains(financas["extrato"]!.AsArray(), l => l!["categoria"]!.GetValue<string>() == "Salarios");
        Assert.True(financas["folhaMensal"]!.GetValue<decimal>() > 0);
        Assert.False(financas["precoPersonalizado"]!.GetValue<bool>());

        var novo = Ok(api.DefinirPrecoIngresso("75.5"));
        Assert.Equal((75.5m, true), (novo["precoIngresso"]!.GetValue<decimal>(), novo["precoPersonalizado"]!.GetValue<bool>()));

        var sugerido = Ok(api.DefinirPrecoIngresso(""));
        Assert.False(sugerido["precoPersonalizado"]!.GetValue<bool>());
        Assert.Contains("entre R$ 1", Falha(api.DefinirPrecoIngresso("0")), StringComparison.Ordinal);
    }

    [Fact]
    public void Save_DuranteJogoAoVivo_FalhaComMensagem()
    {
        var api = ComCarreira();
        Ok(api.IniciarAoVivo());

        Assert.Contains("ao vivo", Falha(api.ExportarSave()), StringComparison.Ordinal);
    }

    [Fact]
    public void Save_ArquivoInvalido_FalhaComMensagem()
    {
        Assert.Contains("save válido", Falha(new ApiJson().CarregarSave("{\"oi\": 1}")), StringComparison.Ordinal);
    }

    [Fact]
    public void AoVivo_SemRodadaIniciada_Falha()
    {
        var api = ComCarreira();

        Assert.Contains("Não há rodada ao vivo", Falha(api.AvancarAoVivo(1)), StringComparison.Ordinal);
        Assert.Contains("Não há rodada ao vivo", Falha(api.ConcluirAoVivo()), StringComparison.Ordinal);
    }

    [Fact]
    public void TemporadaInteira_EncerrarEComecarAProxima()
    {
        var api = ComCarreira("cerrado");
        for (var r = 0; r < 38; r++)
        {
            Ok(api.AvancarRodada());
        }

        Assert.True(Ok(api.Estado())["temporadaEncerrada"]!.GetValue<bool>());
        Assert.Contains("acabou", Falha(api.AvancarRodada()), StringComparison.Ordinal);

        var tabela = Ok(api.Classificacao("serie-a"));
        Assert.All(tabela["linhas"]!.AsArray(), l => Assert.Equal(38, l!["jogos"]!.GetValue<int>()));
        Assert.Equal(10, tabela["artilharia"]!.AsArray().Count);

        var fim = Ok(api.EncerrarTemporada());
        Assert.Equal((2027, 2028), (fim["ano"]!.GetValue<int>(), fim["proximoAno"]!.GetValue<int>()));
        Assert.Equal(4, fim["divisoes"]![0]!["rebaixados"]!.AsArray().Count);

        var estado = Ok(api.Estado());
        Assert.Equal((2028, 0, false), (estado["ano"]!.GetValue<int>(), estado["rodadaAtual"]!.GetValue<int>(), estado["temporadaEncerrada"]!.GetValue<bool>()));
        Assert.Contains("ainda não terminou", Falha(api.EncerrarTemporada()), StringComparison.Ordinal);
    }
}
