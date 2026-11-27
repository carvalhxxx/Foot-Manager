using Jogo.Dominio;
using Jogo.Simulacao.Geracao;
using Jogo.Simulacao.Temporadas;

namespace Jogo.Simulacao.Testes.Temporadas;

/// <summary>Uma temporada inteira simulada uma vez e compartilhada pelos testes da classe.</summary>
public sealed class TemporadaSimulada
{
    public TemporadaSimulada()
    {
        Mundo = GeradorDeMundo.Gerar(2027, 2027, CatalogoBase.Clubes);
        Temporada = new Temporada(Mundo, 11);
        Temporada.JogarAteOFim();
    }

    public Mundo Mundo { get; }
    public Temporada Temporada { get; }
}

public class TemporadaTestes(TemporadaSimulada simulada) : IClassFixture<TemporadaSimulada>
{
    private static Mundo NovoMundo() => GeradorDeMundo.Gerar(2027, 2027, CatalogoBase.Clubes);

    [Fact]
    public void TemporadaCompleta_TodosJogam38ComPontosCoerentes()
    {
        foreach (var divisao in simulada.Mundo.Divisoes)
        {
            var tabela = simulada.Temporada.Tabela(divisao.Id);

            Assert.Equal(20, tabela.Count);
            Assert.All(tabela, l => Assert.Equal(38, l.Jogos));
            Assert.Equal(380, simulada.Temporada.Competicao(divisao.Id).Disputados.Count);

            // Cada jogo distribui 3 pontos (vitória) ou 2 (empate).
            var jogos = simulada.Temporada.Competicao(divisao.Id).Disputados;
            var esperado = jogos.Sum(j => j.Placar.GolsMandante == j.Placar.GolsVisitante ? 2 : 3);
            Assert.Equal(esperado, tabela.Sum(l => l.Pontos));
            Assert.Equal(tabela.Sum(l => l.GolsPro), tabela.Sum(l => l.GolsContra));
        }
    }

    [Fact]
    public void Temporada_Encerrada_NaoJogaMais()
    {
        Assert.True(simulada.Temporada.Encerrada);
        Assert.Throws<InvalidOperationException>(() => simulada.Temporada.JogarRodada());
    }

    [Fact]
    public void Artilharia_OrdenadaEComGolsDoClube()
    {
        var artilharia = simulada.Temporada.Artilharia("serie-a", 10);

        Assert.Equal(10, artilharia.Count);
        Assert.True(artilharia.Zip(artilharia.Skip(1)).All(p => p.First.Gols >= p.Second.Gols));
        Assert.InRange(artilharia[0].Gols, 12, 40);
        Assert.Contains(simulada.Mundo.Clube(artilharia[0].ClubeId).Elenco, j => j.Id == artilharia[0].JogadorId);
    }

    [Fact]
    public void MesmaSeed_MesmaTemporada()
    {
        var a = new Temporada(NovoMundo(), 3);
        var b = new Temporada(NovoMundo(), 3);
        for (var i = 0; i < 5; i++)
        {
            a.JogarRodada();
            b.JogarRodada();
        }

        Assert.Equal(a.Tabela("serie-a"), b.Tabela("serie-a"));
    }

    [Fact]
    public void Rodada_JogaAsDuasDivisoes()
    {
        var temporada = new Temporada(NovoMundo(), 4);

        var relatorio = temporada.JogarRodada();

        Assert.Equal(1, relatorio.Rodada);
        Assert.Equal(10, relatorio.JogosPorDivisao["serie-a"].Count);
        Assert.Equal(10, relatorio.JogosPorDivisao["serie-b"].Count);
        Assert.Equal(1, temporada.RodadaAtual);
    }

    [Fact]
    public void Suspensos_NaoSaoEscalados()
    {
        var mundo = NovoMundo();
        var temporada = new Temporada(mundo, 5);
        var verificados = 0;

        for (var r = 0; r < 12; r++)
        {
            temporada.JogarRodada();
            foreach (var clube in mundo.Clubes)
            {
                var fora = clube.Elenco.Where(j => !j.Disponivel).Select(j => j.Id).ToHashSet();
                if (fora.Count == 0)
                {
                    continue;
                }

                var escalacao = temporada.Escalar(clube);
                Assert.DoesNotContain(escalacao.Titulares.Concat(escalacao.Reservas), j => fora.Contains(j.Id));
                verificados++;
            }
        }

        Assert.True(verificados > 10);
    }

    [Fact]
    public void Entrosamento_CresceComTitularesRepetidos()
    {
        var mundo = NovoMundo();
        var temporada = new Temporada(mundo, 6);
        var clube = mundo.Clube("pampa");
        var antes = temporada.Escalar(clube).BonusEntrosamento;

        for (var r = 0; r < 6; r++)
        {
            temporada.JogarRodada();
        }

        Assert.Equal(0, antes);
        Assert.True(temporada.Escalar(clube).BonusEntrosamento > 0.015);
    }
}
