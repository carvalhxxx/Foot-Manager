using Jogo.Dominio;
using Jogo.Simulacao.Calibracao;
using Jogo.Simulacao.Geracao;

namespace Jogo.Simulacao.Testes.Calibracao;

/// <summary>
/// Versão menor da calibração (a completa roda no CI pela CLI). Usa o mesmo mundo da base
/// (seed 2027) para que mudanças no motor que quebrem o equilíbrio falhem já nos testes.
/// </summary>
public class CalibradorTestes
{
    private static readonly Mundo Mundo = GeradorDeMundo.Gerar(2027, 2027, CatalogoBase.Clubes);

    [Fact]
    public void Calibracao_MotorPadrao_AtingeTodasAsMetas()
    {
        var relatorio = Calibrador.Executar(Mundo, partidasLiga: 4000, partidasPorFormacao: 800, seed: 7);

        var falhas = relatorio.Liga.Concat(relatorio.Formacoes).Where(m => !m.Atingida)
            .Select(m => $"{m.Nome}: {m.Valor:F3} (faixa {m.Minimo}..{m.Maximo})");
        Assert.True(relatorio.Aprovado, string.Join("\n", falhas));
    }

    [Fact]
    public void Calibracao_EhDeterministica()
    {
        var a = Calibrador.Executar(Mundo, 300, 50, 3);
        var b = Calibrador.Executar(Mundo, 300, 50, 3);

        Assert.Equal(a.Liga.Select(m => m.Valor), b.Liga.Select(m => m.Valor));
        Assert.Equal(a.Formacoes.Select(m => m.Valor), b.Formacoes.Select(m => m.Valor));
    }

    [Fact]
    public void Meta_ForaDaFaixa_NaoEhAtingida()
    {
        Assert.True(new Meta("x", 0.5, 0.4, 0.6).Atingida);
        Assert.False(new Meta("x", 0.7, 0.4, 0.6).Atingida);
        Assert.True(new Meta("info", 123, null, null).Atingida);
        Assert.False(new Meta("teto", 0.5, null, 0.4).Atingida);
    }
}

public class CalibradorTemporadaTestes
{
    [Fact]
    public void Temporadas_MedemCampeaoEPontosDaSerieA()
    {
        var metas = Calibrador.Temporadas(() => GeradorDeMundo.Gerar(2027, 2027, CatalogoBase.Clubes), 3, 5);

        Assert.Equal(8, metas.Count);
        Assert.InRange(metas.Single(m => m.Nome == "Clubes no vermelho").Valor, 0, 0.2);
        Assert.True(metas.Single(m => m.Nome.StartsWith("Resultado dos grandes", StringComparison.Ordinal)).Valor
                    > metas.Single(m => m.Nome.StartsWith("Resultado dos pequenos", StringComparison.Ordinal)).Valor);
        Assert.InRange(metas.Single(m => m.Nome == "Pontos do campeão").Valor, 60, 100);
        Assert.InRange(metas.Single(m => m.Nome == "Pontos do lanterna").Valor, 5, 45);
        Assert.Equal(1.0, metas.Single(m => m.Nome == "Campeão entre os 4 mais fortes").Valor, 6);
    }
}
