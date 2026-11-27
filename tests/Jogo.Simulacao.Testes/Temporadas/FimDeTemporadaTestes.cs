using Jogo.Dominio.Aleatoriedade;
using Jogo.Dominio.Jogadores;
using Jogo.Simulacao.Geracao;
using Jogo.Simulacao.Temporadas;

namespace Jogo.Simulacao.Testes.Temporadas;

public class FimDeTemporadaTestes
{
    [Fact]
    public void Encerrar_QuatroSobemQuatroDescemEAnoAvanca()
    {
        var mundo = GeradorDeMundo.Gerar(2027, 2027, CatalogoBase.Clubes);
        var temporada = new Temporada(mundo, 21);
        temporada.JogarAteOFim();
        var tabelaA = temporada.Tabela("serie-a");
        var tabelaB = temporada.Tabela("serie-b");
        var idades = mundo.Clubes.SelectMany(c => c.Elenco).ToDictionary(j => j.Id, j => j.Idade);

        var resultado = FimDeTemporada.Encerrar(temporada, mundo, 21);

        var rebaixados = tabelaA.TakeLast(4).Select(l => l.ClubeId).ToArray();
        var promovidos = tabelaB.Take(4).Select(l => l.ClubeId).ToArray();
        Assert.Equal(rebaixados, resultado.Divisoes[0].Rebaixados);
        Assert.Equal(promovidos, resultado.Divisoes[1].Promovidos);
        Assert.Equal(tabelaA[0].ClubeId, resultado.Divisoes[0].CampeaoId);
        Assert.All(promovidos, id => Assert.Contains(id, mundo.Divisoes[0].ClubeIds));
        Assert.All(rebaixados, id => Assert.Contains(id, mundo.Divisoes[1].ClubeIds));
        Assert.Equal(2028, mundo.TemporadaAtual);

        // Quem continua envelheceu um ano; aposentados saíram; elencos voltaram para 25-30.
        foreach (var jogador in mundo.Clubes.SelectMany(c => c.Elenco).Where(j => idades.ContainsKey(j.Id)))
        {
            Assert.Equal(idades[jogador.Id] + 1, jogador.Idade);
        }

        Assert.All(resultado.Aposentados, id => Assert.DoesNotContain(mundo.Clubes.SelectMany(c => c.Elenco), j => j.Id == id));
        Assert.All(mundo.Clubes, c => Assert.InRange(c.Elenco.Count, FimDeTemporada.ElencoMinimo, FimDeTemporada.ElencoMaximo));
        Assert.NotEmpty(resultado.Revelacoes);
        Assert.All(mundo.Clubes.SelectMany(c => c.Elenco), j => Assert.True(j.Disponivel && j.Condicao == 100));
    }

    [Fact]
    public void Encerrar_DuasVezes_Lanca()
    {
        var mundo = GeradorDeMundo.Gerar(2027, 2027, CatalogoBase.Clubes);
        var temporada = new Temporada(mundo, 1);
        temporada.JogarAteOFim();
        FimDeTemporada.Encerrar(temporada, mundo, 1);

        Assert.Throws<InvalidOperationException>(() => FimDeTemporada.Encerrar(temporada, mundo, 1));
    }

    [Fact]
    public void Encerrar_AntesDoFim_Lanca()
    {
        var mundo = GeradorDeMundo.Gerar(2027, 2027, CatalogoBase.Clubes);
        var temporada = new Temporada(mundo, 1);

        Assert.Throws<InvalidOperationException>(() => FimDeTemporada.Encerrar(temporada, mundo, 1));
    }

    [Fact]
    public void VariasTemporadas_MundoContinuaSaudavel()
    {
        var mundo = GeradorDeMundo.Gerar(2027, 2027, CatalogoBase.Clubes);

        for (var t = 0; t < 4; t++)
        {
            var temporada = new Temporada(mundo, (ulong)(100 + t));
            temporada.JogarAteOFim();
            FimDeTemporada.Encerrar(temporada, mundo, (ulong)(100 + t));
        }

        var jogadores = mundo.Clubes.SelectMany(c => c.Elenco).ToArray();
        Assert.Equal(2031, mundo.TemporadaAtual);
        Assert.All(jogadores, j => Assert.InRange(j.Idade, 17, FimDeTemporada.IdadeLimite));
        Assert.Equal(jogadores.Length, jogadores.Select(j => j.Id).Distinct().Count());
        Assert.All(jogadores, j => Assert.True(j.Contrato!.TemporadaFinal >= 2030));
        Assert.All(mundo.Clubes, c => Assert.Contains(c.Elenco, j => j.PosicaoPrincipal == Posicao.Goleiro));
    }

    [Fact]
    public void Evolucao_JovemCresceAtePotencial_VeteranoCai()
    {
        var aleatorio = new GeradorAleatorio(3);
        var gerador = new GeradorDeJogadores(new GeradorAleatorio(4));
        var jovens = Enumerable.Range(1, 100).Select(i => gerador.Gerar(i, $"J{i}", Posicao.MeiaCentral, 60, "c", 2027, idade: 19)).ToArray();
        var veteranos = Enumerable.Range(200, 100).Select(i => gerador.Gerar(i, $"V{i}", Posicao.MeiaCentral, 70, "c", 2027, idade: 34)).ToArray();

        var antesJovens = jovens.Average(j => j.ForcaGeral);
        var antesVeteranos = veteranos.Average(j => j.ForcaGeral);
        foreach (var j in jovens.Concat(veteranos))
        {
            Evolucao.Aplicar(j, Evolucao.SortearVariacao(j, aleatorio), aleatorio);
        }

        Assert.True(jovens.Average(j => j.ForcaGeral) > antesJovens + 1.5);
        Assert.True(veteranos.Average(j => j.ForcaGeral) < antesVeteranos - 1);
        Assert.All(jovens, j => Assert.True(j.ForcaGeral <= Math.Max(j.Ocultos.Potencial, 60) + 1));
    }
}
