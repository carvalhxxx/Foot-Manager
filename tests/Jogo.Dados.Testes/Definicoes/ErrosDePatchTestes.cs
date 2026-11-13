using System.Text.Json.Nodes;
using Jogo.Dados.Definicoes;

namespace Jogo.Dados.Testes.Definicoes;

/// <summary>
/// Quem edita um patch precisa saber exatamente o que corrigir:
/// arquivo, campo e motivo, e todos os erros de uma vez.
/// </summary>
public sealed class ErrosDePatchTestes : IDisposable
{
    private readonly PastaTemporaria _pasta = new();

    public ErrosDePatchTestes()
    {
        EscritorDefinicoes.Salvar(MundoDeExemplo.Criar(), new Manifesto("Base"), _pasta.Caminho);
    }

    public void Dispose() => _pasta.Dispose();

    [Fact]
    public void JsonQuebrado_ApontaArquivoELinha()
    {
        File.WriteAllText(_pasta.Arquivo("divisoes.json"), "{\n  \"versaoFormato\": 1,\n  \"divisoes\": [ ,\n}");

        var erro = Assert.Single(CarregarComErros());
        Assert.Equal("divisoes.json", erro.Arquivo);
        Assert.Contains("linha 3", erro.Mensagem, StringComparison.Ordinal);
    }

    [Fact]
    public void VersaoMaisNova_PedeParaAtualizarOJogo()
    {
        EditarJson("manifesto.json", j => j["versaoFormato"] = 99);

        var erro = Assert.Single(CarregarComErros());
        Assert.Equal("versaoFormato", erro.Local);
        Assert.Contains("Atualize o jogo", erro.Mensagem, StringComparison.Ordinal);
    }

    [Fact]
    public void VariosErros_SaoReportadosJuntos()
    {
        EditarJson("clubes/porto-alegre-fc.json", j =>
        {
            j["sigla"] = "pa";
            j["elenco"]![1]!["idade"] = 50;
            j["elenco"]![2]!["posicao"] = "XYZ";
        });

        var erros = CarregarComErros();

        Assert.Equal(3, erros.Count);
        Assert.Contains(erros, e => e.Local == "" && e.Mensagem.Contains("Sigla", StringComparison.Ordinal));
        Assert.Contains(erros, e => e.Local == "elenco[1]" && e.Mensagem.Contains("idade", StringComparison.Ordinal));
        Assert.Contains(erros, e => e.Local == "elenco[2].posicao" && e.Mensagem.Contains("GOL, ZAG, LD", StringComparison.Ordinal));
        Assert.All(erros, e => Assert.Equal("clubes/porto-alegre-fc.json", e.Arquivo));
    }

    [Fact]
    public void ErroNoJogador_ApontaAPosicaoNoElencoSemTextoTecnico()
    {
        EditarJson("clubes/porto-alegre-fc.json", j => j["elenco"]![1]!["idade"] = 50);

        var erro = Assert.Single(CarregarComErros());
        Assert.Equal("elenco[1]", erro.Local);
        Assert.Equal("idade deve estar entre 15 e 45.", erro.Mensagem);
    }

    [Fact]
    public void AtributoForaDaEscala_ApontaOAtributo()
    {
        EditarJson("clubes/goiania-ec.json", j => j["elenco"]![0]!["atributos"]!["passe"] = 25);

        var erro = Assert.Single(CarregarComErros());
        Assert.Equal("elenco[0].atributos", erro.Local);
        Assert.Equal("Passe deve estar entre 1 e 20.", erro.Mensagem);
    }

    [Fact]
    public void CampoObrigatorioAusente_ApontaOCampo()
    {
        EditarJson("clubes/goiania-ec.json", j => j.AsObject().Remove("torcida"));

        var erro = Assert.Single(CarregarComErros());
        Assert.Equal("torcida", erro.Local);
        Assert.Equal("Campo obrigatório ausente.", erro.Mensagem);
    }

    [Fact]
    public void IdDiferenteDoNomeDoArquivo_EhErro()
    {
        File.Move(_pasta.Arquivo("clubes/goiania-ec.json"), _pasta.Arquivo("clubes/outro-nome.json"));

        var erros = CarregarComErros();

        Assert.Contains(erros, e => e.Arquivo == "clubes/outro-nome.json" && e.Local == "id");
    }

    [Fact]
    public void DivisaoCitaClubeInexistente_EhErroDeConjunto()
    {
        File.Delete(_pasta.Arquivo("clubes/goiania-ec.json"));

        var erro = Assert.Single(CarregarComErros());
        Assert.Equal("(conjunto)", erro.Arquivo);
        Assert.Contains("\"goiania-ec\", que não existe", erro.Mensagem, StringComparison.Ordinal);
    }

    [Fact]
    public void ArquivoAusente_EhErro()
    {
        File.Delete(_pasta.Arquivo("manifesto.json"));

        var erro = Assert.Single(CarregarComErros());
        Assert.Equal("manifesto.json", erro.Arquivo);
        Assert.Equal("Arquivo não encontrado.", erro.Mensagem);
    }

    [Fact]
    public void ComentariosEVirgulaFinal_SaoAceitos()
    {
        var caminho = _pasta.Arquivo("manifesto.json");
        var texto = File.ReadAllText(caminho).Replace("{", "{\n  // patch de teste", StringComparison.Ordinal);
        File.WriteAllText(caminho, texto.Replace("\n}", ",\n}", StringComparison.Ordinal));

        var carregado = LeitorDefinicoes.Carregar(_pasta.Caminho);

        Assert.Equal("Base", carregado.Manifesto.Nome);
    }

    [Fact]
    public void PastaInexistente_Lanca()
    {
        Assert.Throws<DirectoryNotFoundException>(() => LeitorDefinicoes.Carregar(_pasta.Arquivo("nao-existe")));
    }

    private List<ErroDefinicao> CarregarComErros() =>
        [.. Assert.Throws<DefinicoesInvalidasException>(() => LeitorDefinicoes.Carregar(_pasta.Caminho)).Erros];

    private void EditarJson(string relativo, Action<JsonNode> edicao)
    {
        var caminho = _pasta.Arquivo(relativo);
        var no = JsonNode.Parse(File.ReadAllText(caminho))!;
        edicao(no);
        File.WriteAllText(caminho, no.ToJsonString());
    }
}
