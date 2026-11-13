using Jogo.Dominio.Clubes;
using Jogo.Dominio.Competicoes;
using Jogo.Dominio.Jogadores;

namespace Jogo.Dominio.Testes.Clubes;

public class ClubeTestes
{
    private static Clube Criar(string id = "porto-alegre-fc", string sigla = "PAF", string uf = "RS") =>
        new(id, "Porto Alegre FC", sigla, "Porto Alegre", uf,
            new Cores("#003399", "#ffffff"), new Estadio("Arena do Guaíba", 45_000),
            torcida: 70, reputacao: 65, caixa: 5_000_000m);

    private static Jogador Jogador(int id) =>
        new(id, $"Jogador {id}", 22, Posicao.Volante, [], Atributos.Uniformes(10),
            new AtributosOcultos { Potencial = 70, PropensaoLesao = 5, Profissionalismo = 12 });

    [Fact]
    public void Construtor_NormalizaCoresParaMaiusculas()
    {
        Assert.Equal("#FFFFFF", Criar().Cores.Secundaria);
    }

    [Theory]
    [InlineData("Porto Alegre")]
    [InlineData("porto_alegre")]
    [InlineData("são-paulo")]
    [InlineData("-inicio")]
    public void Construtor_IdInvalido_Lanca(string id)
    {
        Assert.Throws<ArgumentException>(() => Criar(id: id));
    }

    [Theory]
    [InlineData("PA")]
    [InlineData("paf")]
    [InlineData("PAFC")]
    public void Construtor_SiglaInvalida_Lanca(string sigla)
    {
        Assert.Throws<ArgumentException>(() => Criar(sigla: sigla));
    }

    [Fact]
    public void Construtor_UfDesconhecida_Lanca()
    {
        Assert.Throws<ArgumentException>(() => Criar(uf: "XX"));
    }

    [Fact]
    public void Construtor_CorInvalida_Lanca()
    {
        Assert.Throws<ArgumentException>(() => new Cores("azul", "#FFFFFF"));
    }

    [Fact]
    public void Estadio_CapacidadeMuitoPequena_Lanca()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Estadio("Campinho", 100));
    }

    [Fact]
    public void Elenco_AdicionarERemover()
    {
        var clube = Criar();
        clube.AdicionarAoElenco(Jogador(1));
        clube.AdicionarAoElenco(Jogador(2));

        Assert.True(clube.RemoverDoElenco(1));
        Assert.False(clube.RemoverDoElenco(99));
        Assert.Equal([2], clube.Elenco.Select(j => j.Id));
    }

    [Fact]
    public void Elenco_JogadorRepetido_Lanca()
    {
        var clube = Criar();
        clube.AdicionarAoElenco(Jogador(1));

        Assert.Throws<InvalidOperationException>(() => clube.AdicionarAoElenco(Jogador(1)));
    }

    [Fact]
    public void Caixa_PodeFicarNegativo()
    {
        var clube = Criar();

        clube.MovimentarCaixa(-6_000_000m);

        Assert.Equal(-1_000_000m, clube.Caixa);
    }

    [Fact]
    public void Divisao_VinteClubes_TemTrintaEOitoRodadas()
    {
        var clubes = Enumerable.Range(1, 20).Select(i => $"clube-{i}");

        var serieA = new Divisao("serie-a", "Série A", 1, clubes, vagasAcesso: 0, vagasRebaixamento: 4);

        Assert.Equal(38, serieA.TotalRodadas);
    }

    [Fact]
    public void Divisao_NumeroImparDeClubes_Lanca()
    {
        Assert.Throws<ArgumentException>(() =>
            new Divisao("serie-b", "Série B", 2, ["a", "b", "c"], 1, 1));
    }

    [Fact]
    public void Divisao_ClubeRepetido_Lanca()
    {
        Assert.Throws<ArgumentException>(() =>
            new Divisao("serie-b", "Série B", 2, ["a", "a"], 1, 0));
    }

    [Fact]
    public void Divisao_PrimeiraComAcesso_Lanca()
    {
        Assert.Throws<ArgumentException>(() =>
            new Divisao("serie-a", "Série A", 1, ["a", "b"], 1, 0));
    }
}
