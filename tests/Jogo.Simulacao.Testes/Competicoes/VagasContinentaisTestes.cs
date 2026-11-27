using Jogo.Simulacao.Competicoes;

namespace Jogo.Simulacao.Testes.Competicoes;

/// <summary>Os exemplos do regulamento de 2026 (D-037), um por teste.</summary>
public class VagasContinentaisTestes
{
    // Tabela final: 1º Flamengo, 2º Palmeiras, 3º São Paulo, 4º Corinthians, 5º Bahia, 6º Grêmio, 7º Fluminense...
    private static readonly string[] Tabela =
    [
        "flamengo", "palmeiras", "sao-paulo", "corinthians", "bahia", "gremio", "fluminense", "cruzeiro",
        "botafogo", "vasco", "santos", "internacional", "vitoria", "coritiba", "bragantino", "athletico-pr",
        "atletico-mg", "mirassol", "remo", "chapecoense",
    ];

    private static readonly HashSet<string> Rebaixados = ["atletico-mg", "mirassol", "remo", "chapecoense"];

    private static IReadOnlyList<VagaContinental> Distribuir(
        string? libertadores = null, string? sulAmericana = null, string? copa = null, string? vice = null) =>
        VagasContinentais.Distribuir(new EntradaDasVagas(Tabela, Rebaixados, libertadores, sulAmericana, copa, vice));

    private static string[] Com(IReadOnlyList<VagaContinental> vagas, DestinoContinental destino) =>
        [.. vagas.Where(v => v.Destino == destino).Select(v => v.ClubeId)];

    [Fact]
    public void SemTitulos_DistribuicaoBase()
    {
        var vagas = Distribuir();

        Assert.Equal(["flamengo", "palmeiras", "sao-paulo", "corinthians"], Com(vagas, DestinoContinental.LibertadoresGrupos));
        Assert.Equal(["bahia"], Com(vagas, DestinoContinental.LibertadoresPreliminar));
        Assert.Equal(["gremio", "fluminense", "cruzeiro", "botafogo", "vasco", "santos"], Com(vagas, DestinoContinental.SulAmericana));
    }

    [Fact]
    public void CampeaoDaLibertadoresNoG5_AbreVagaParaOSexto()
    {
        var vagas = Distribuir(libertadores: "flamengo");

        Assert.Contains(new VagaContinental("flamengo", DestinoContinental.LibertadoresGrupos, OrigemDaVaga.CampeaoLibertadores), vagas);
        Assert.Contains(vagas, v => v.ClubeId == "gremio" && v.Destino != DestinoContinental.SulAmericana);
        Assert.Equal(6, vagas.Count(v => v.Destino != DestinoContinental.SulAmericana));
        // A Sul-Americana começa depois de quem foi à Libertadores.
        Assert.Equal("fluminense", Com(vagas, DestinoContinental.SulAmericana)[0]);
    }

    [Fact]
    public void CampeaoDaSulAmericanaNoG5_TambemAbreVaga()
    {
        var vagas = Distribuir(sulAmericana: "bahia");

        Assert.Contains(new VagaContinental("bahia", DestinoContinental.LibertadoresGrupos, OrigemDaVaga.CampeaoSulAmericana), vagas);
        Assert.Equal(["gremio"], Com(vagas, DestinoContinental.LibertadoresPreliminar));
    }

    [Fact]
    public void CampeaoContinentalForaDoG5_NaoTiraVagaDaTabela()
    {
        var vagas = Distribuir(libertadores: "vasco");

        Assert.Contains(new VagaContinental("vasco", DestinoContinental.LibertadoresGrupos, OrigemDaVaga.CampeaoLibertadores), vagas);
        Assert.Equal(["bahia"], Com(vagas, DestinoContinental.LibertadoresPreliminar));
        Assert.DoesNotContain("vasco", Com(vagas, DestinoContinental.SulAmericana));
    }

    [Fact]
    public void CampeaoDaCopaJaNoG4_ViceHerdaOsGruposEAPreliminarVoltaParaATabela()
    {
        var vagas = Distribuir(copa: "palmeiras", vice: "gremio");

        Assert.Contains(new VagaContinental("gremio", DestinoContinental.LibertadoresGrupos, OrigemDaVaga.CopaDoBrasil), vagas);
        Assert.Equal(["bahia", "fluminense"], Com(vagas, DestinoContinental.LibertadoresPreliminar));
        Assert.Equal(7, vagas.Count(v => v.Destino != DestinoContinental.SulAmericana));
    }

    [Fact]
    public void FinalistasDaCopaJaClassificados_AsDuasVagasVoltamParaOBrasileirao()
    {
        var vagas = Distribuir(copa: "palmeiras", vice: "sao-paulo");

        Assert.Equal(["flamengo", "palmeiras", "sao-paulo", "corinthians", "bahia"], Com(vagas, DestinoContinental.LibertadoresGrupos));
        Assert.Equal(["gremio", "fluminense"], Com(vagas, DestinoContinental.LibertadoresPreliminar));
        Assert.DoesNotContain(vagas, v => v.Origem == OrigemDaVaga.CopaDoBrasil);
    }

    [Fact]
    public void CampeaoDaCopaForaDoG4_FicaComOsGruposEViceComAPreliminar()
    {
        var vagas = Distribuir(copa: "cruzeiro", vice: "botafogo");

        Assert.Contains(new VagaContinental("cruzeiro", DestinoContinental.LibertadoresGrupos, OrigemDaVaga.CopaDoBrasil), vagas);
        Assert.Contains(new VagaContinental("botafogo", DestinoContinental.LibertadoresPreliminar, OrigemDaVaga.CopaDoBrasil), vagas);
        Assert.Equal(["bahia", "botafogo"], Com(vagas, DestinoContinental.LibertadoresPreliminar).Order());
    }

    [Fact]
    public void DuasClassificacoes_FicaComAMelhor()
    {
        // Bahia (5º, preliminar pela tabela) ganha a Copa: fica com os grupos e a preliminar dele passa adiante.
        var vagas = Distribuir(copa: "bahia", vice: "flamengo");

        Assert.Single(vagas, v => v.ClubeId == "bahia");
        Assert.Contains(new VagaContinental("bahia", DestinoContinental.LibertadoresGrupos, OrigemDaVaga.CopaDoBrasil), vagas);
        Assert.DoesNotContain(vagas, v => v.ClubeId == "flamengo" && v.Origem == OrigemDaVaga.CopaDoBrasil);
        Assert.Equal(vagas.Count, vagas.Select(v => v.ClubeId).Distinct().Count());
    }

    [Fact]
    public void Rebaixado_NaoJogaALibertadores()
    {
        var vagas = Distribuir(libertadores: "remo");

        Assert.DoesNotContain(vagas, v => v.ClubeId == "remo");
        // A vaga do título é redistribuída pela tabela.
        Assert.Equal(6, vagas.Count(v => v.Destino != DestinoContinental.SulAmericana));
    }
}
