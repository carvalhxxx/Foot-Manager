using Jogo.Dominio.Aleatoriedade;
using Jogo.Simulacao.Temporadas;

namespace Jogo.Simulacao.Testes.Temporadas;

public class CalendarioTestes
{
    private static readonly string[] Clubes = [.. Enumerable.Range(1, 20).Select(i => $"clube-{i}")];
    private static readonly IReadOnlyList<IReadOnlyList<JogoAgendado>> Rodadas = Calendario.PontosCorridos(Clubes, new GeradorAleatorio(5));

    [Fact]
    public void VinteClubes_TrintaEOitoRodadasDeDezJogos()
    {
        Assert.Equal(38, Rodadas.Count);
        Assert.All(Rodadas, r => Assert.Equal(10, r.Count));
        Assert.Equal(Enumerable.Range(1, 38), Rodadas.Select(r => r[0].Rodada));
    }

    [Fact]
    public void CadaClubeJogaUmaVezPorRodada()
    {
        foreach (var rodada in Rodadas)
        {
            var participantes = rodada.SelectMany(j => new[] { j.MandanteId, j.VisitanteId }).ToArray();
            Assert.Equal(20, participantes.Distinct().Count());
        }
    }

    [Fact]
    public void CadaParSeEnfrentaDuasVezesUmaEmCadaCasa()
    {
        var jogos = Rodadas.SelectMany(r => r).ToArray();

        foreach (var a in Clubes)
        {
            foreach (var b in Clubes.Where(b => b != a))
            {
                Assert.Single(jogos, j => j.MandanteId == a && j.VisitanteId == b);
            }
        }
    }

    [Fact]
    public void ReturnoEspelhaOTurnoComMandoInvertido()
    {
        for (var r = 0; r < 19; r++)
        {
            Assert.Equal(
                Rodadas[r].Select(j => (j.VisitanteId, j.MandanteId)),
                Rodadas[r + 19].Select(j => (j.MandanteId, j.VisitanteId)));
        }
    }

    [Fact]
    public void MandosEquilibrados_NoMaximoTresSeguidos()
    {
        foreach (var clube in Clubes)
        {
            var emCasa = Rodadas.Select(r => r.Single(j => j.MandanteId == clube || j.VisitanteId == clube).MandanteId == clube).ToArray();

            Assert.Equal(19, emCasa.Count(x => x));
            var maior = 1;
            var atual = 1;
            for (var i = 1; i < emCasa.Length; i++)
            {
                atual = emCasa[i] == emCasa[i - 1] ? atual + 1 : 1;
                maior = Math.Max(maior, atual);
            }

            Assert.True(maior <= 3, $"{clube} joga {maior} seguidas no mesmo mando");
        }
    }

    [Fact]
    public void MesmaSeed_MesmoCalendario_SeedDiferente_Outro()
    {
        var igual = Calendario.PontosCorridos(Clubes, new GeradorAleatorio(5));
        var outro = Calendario.PontosCorridos(Clubes, new GeradorAleatorio(6));

        Assert.Equal(Rodadas.SelectMany(r => r), igual.SelectMany(r => r));
        Assert.NotEqual(Rodadas.SelectMany(r => r), outro.SelectMany(r => r));
    }

    [Fact]
    public void NumeroImparDeClubes_Lanca()
    {
        Assert.Throws<ArgumentException>(() => Calendario.PontosCorridos(["a", "b", "c"], new GeradorAleatorio(1)));
    }
}
