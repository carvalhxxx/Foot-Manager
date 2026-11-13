namespace Jogo.Arquitetura.Testes;

/// <summary>
/// Regras de arquitetura do projeto (docs/DECISOES.md, D-003 e D-004):
/// Dominio, Motor e Simulacao são puros: sem I/O, sem relógio, sem aleatoriedade própria,
/// sem bibliotecas externas. Cada camada só enxerga as camadas abaixo dela.
/// </summary>
public class RegrasDeCamadasTestes
{
    public static TheoryData<string, string[]> DependenciasPermitidas => new()
    {
        { "Jogo.Dominio", [] },
        { "Jogo.Motor", ["Jogo.Dominio"] },
        { "Jogo.Simulacao", ["Jogo.Dominio", "Jogo.Motor"] },
        { "Jogo.Dados", ["Jogo.Dominio"] },
    };

    public static TheoryData<string> ProjetosPuros => ["Jogo.Dominio", "Jogo.Motor", "Jogo.Simulacao"];

    [Theory]
    [MemberData(nameof(DependenciasPermitidas))]
    public void Camada_SoReferenciaCamadasPermitidas(string projeto, string[] permitidos)
    {
        var referencias = InspetorDeAssembly.AssembliesReferenciados(InspetorDeAssembly.Caminho(projeto));

        var indevidas = referencias
            .Where(r => r.StartsWith("Jogo.", StringComparison.Ordinal) && !permitidos.Contains(r))
            .ToList();

        Assert.True(indevidas.Count == 0, $"{projeto} referencia camadas proibidas: {string.Join(", ", indevidas)}");
    }

    [Theory]
    [MemberData(nameof(ProjetosPuros))]
    public void ProjetoPuro_SoDependeDaBibliotecaPadrao(string projeto)
    {
        var referencias = InspetorDeAssembly.AssembliesReferenciados(InspetorDeAssembly.Caminho(projeto));

        var externas = referencias
            .Where(r => !r.StartsWith("Jogo.", StringComparison.Ordinal)
                        && r != "System" && !r.StartsWith("System.", StringComparison.Ordinal)
                        && r != "netstandard")
            .ToList();

        Assert.True(externas.Count == 0, $"{projeto} depende de bibliotecas externas: {string.Join(", ", externas)}");
    }

    [Theory]
    [MemberData(nameof(ProjetosPuros))]
    public void ProjetoPuro_NaoUsaIoRelogioNemAleatoriedadePropria(string projeto)
    {
        var violacoes = InspetorDeAssembly.ViolacoesDePureza(InspetorDeAssembly.Caminho(projeto));

        Assert.True(
            violacoes.Count == 0,
            $"{projeto} usa recursos proibidos em código puro (use IGeradorAleatorio / receba datas por parâmetro):\n"
            + string.Join("\n", violacoes));
    }

    /// <summary>
    /// Garante que o inspetor de fato detecta violações (senão os testes acima passariam à toa).
    /// Este próprio assembly de testes contém a classe <see cref="ExemploDeCodigoImpuro"/>.
    /// </summary>
    [Fact]
    public void Inspetor_DetectaViolacoesConhecidas()
    {
        var violacoes = InspetorDeAssembly.ViolacoesDePureza(typeof(ExemploDeCodigoImpuro).Assembly.Location);

        Assert.Contains("tipo System.Random", violacoes);
        Assert.Contains("tipo System.IO.File", violacoes);
        Assert.Contains("membro System.DateTime::get_Now", violacoes);
        Assert.Contains("membro System.Guid::NewGuid", violacoes);
    }
}

/// <summary>Código deliberadamente impuro, usado só para validar o inspetor.</summary>
internal static class ExemploDeCodigoImpuro
{
    public static object[] Usar() =>
    [
        new Random(),
        System.IO.File.Exists("x"),
        DateTime.Now,
        Guid.NewGuid(),
    ];
}
