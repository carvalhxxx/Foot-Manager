using Jogo.Dominio.Aleatoriedade;
using Jogo.Dominio.Jogadores;

namespace Jogo.Motor.Testes;

public class EstiloTestes
{
    private static TimeEscalado Com(TimeEscalado t, Marcacao marcacao = Marcacao.Normal, Concentracao concentracao = Concentracao.Variado,
        int? capitao = null, int? cobrador = null) =>
        new(t.ClubeId, t.Formacao, t.Titulares, t.Reservas, t.Mentalidade, t.BonusEntrosamento, marcacao, concentracao, capitao, cobrador);

    private static (int Faltas, int FinalizacoesSofridas, double CondicaoMedia) Medir(Marcacao marcacao, int jogos = 300)
    {
        var (m, v) = Apoio.DoisTimes();
        var teste = Com(m, marcacao);
        int faltas = 0, sofridas = 0;
        double condicao = 0;
        for (var i = 0; i < jogos; i++)
        {
            var partida = MotorDePartida.Iniciar(teste, v, new GeradorAleatorio((ulong)i + 1), mandanteAutomatico: false, visitanteAutomatico: false);
            MotorDePartida.SimularAteOFim(partida);
            faltas += partida.Mandante.Estatisticas.Faltas;
            sofridas += partida.Visitante.Estatisticas.Finalizacoes;
            condicao += partida.Mandante.Participantes.Average(j => j.Condicao);
        }

        return (faltas, sofridas, condicao / jogos);
    }

    [Fact]
    public void MarcacaoForte_MenosChancesMaisFaltasMaisCansaco()
    {
        var normal = Medir(Marcacao.Normal);
        var forte = Medir(Marcacao.Forte);
        var leve = Medir(Marcacao.Leve);

        Assert.True(forte.Faltas > normal.Faltas * 1.15 && leve.Faltas < normal.Faltas * 0.85);
        Assert.True(forte.FinalizacoesSofridas < normal.FinalizacoesSofridas && leve.FinalizacoesSofridas > normal.FinalizacoesSofridas);
        Assert.True(forte.CondicaoMedia < normal.CondicaoMedia && leve.CondicaoMedia > normal.CondicaoMedia);
    }

    [Fact]
    public void Concentracao_LadoMaisForteQueADefesaRival_AjudaOAtaque()
    {
        var (m, v) = Apoio.DoisTimes(forcaMandante: 80, forcaVisitante: 60);
        var partida = MotorDePartida.Iniciar(Com(m, concentracao: Concentracao.Esquerda), v, new GeradorAleatorio(1));
        var variado = MotorDePartida.Iniciar(m, v, new GeradorAleatorio(1));
        var contra = MotorDePartida.Iniciar(Com(v, concentracao: Concentracao.Esquerda), m, new GeradorAleatorio(1));

        Assert.Equal(1 + ConfiguracaoMotor.Padrao.ConcentracaoMaxima, MotorDePartida.FatorConcentracao(partida, partida.Mandante, partida.Visitante), 6);
        Assert.Equal(1, MotorDePartida.FatorConcentracao(variado, variado.Mandante, variado.Visitante));
        Assert.Equal(1 - ConfiguracaoMotor.Padrao.ConcentracaoMaxima, MotorDePartida.FatorConcentracao(contra, contra.Mandante, contra.Visitante), 6);
    }

    [Fact]
    public void Capitao_EmCampoDaBonus_ESubstituidoPerde()
    {
        var (m, v) = Apoio.DoisTimes();
        var capitao = m.Titulares[5];
        var time = Com(m, capitao: capitao.Id);
        var semBonus = ConfiguracaoMotor.Padrao with { BonusCapitao = 0 };
        Partida Nova(ConfiguracaoMotor cfg) =>
            MotorDePartida.Iniciar(time, v, new GeradorAleatorio(1), configuracao: cfg, mandanteAutomatico: false);

        var com = Nova(ConfiguracaoMotor.Padrao);
        var sem = Nova(semBonus);
        var bonus = 1 + (ConfiguracaoMotor.Padrao.BonusCapitao * capitao.Atributos.Determinacao / 20.0);
        Assert.Equal(capitao.Id, com.Mandante.CapitaoId);
        Assert.Equal(MotorDePartida.Forca(sem, sem.Mandante).Meio * bonus, MotorDePartida.Forca(com, com.Mandante).Meio, 6);

        // Capitão substituído: o bônus acaba (fica igual a jogar sem bônus de capitão).
        foreach (var p in new[] { com, sem })
        {
            MotorDePartida.AplicarDecisao(p, Equipe.Mandante, new Substituicao(capitao.Id, m.Reservas[0].Id));
        }

        Assert.Equal(MotorDePartida.Forca(sem, sem.Mandante).Meio, MotorDePartida.Forca(com, com.Mandante).Meio, 6);
    }

    [Fact]
    public void Cobrador_BateAsFaltasDiretas()
    {
        var (m, v) = Apoio.DoisTimes();
        var cobrador = m.Titulares[10];
        var time = Com(m, cobrador: cobrador.Id);
        int cobrancas = 0, doCobrador = 0;
        for (var i = 0; i < 200; i++)
        {
            var r = MotorDePartida.SimularRapido(time, v, new GeradorAleatorio((ulong)i + 1));
            var deFalta = r.Eventos.Where(e => e.Origem == OrigemLance.Falta && e.Equipe == Equipe.Mandante && e.JogadorId is not null).ToArray();
            cobrancas += deFalta.Length;
            doCobrador += deFalta.Count(e => e.JogadorId == cobrador.Id);
        }

        // Só quando ele sai (substituição ou lesão) outro assume.
        Assert.True(cobrancas > 10);
        Assert.True(doCobrador >= cobrancas * 0.8);
    }

    [Fact]
    public void TrocaDeEstilo_AoVivo_MudaOTimeERegistraEvento()
    {
        var partida = Apoio.Iniciar(automatico: false);

        var evento = MotorDePartida.AplicarDecisao(partida, Equipe.Visitante, new TrocaDeEstilo(Marcacao.Forte, Concentracao.Direita));

        Assert.Equal((Marcacao.Forte, Concentracao.Direita), (partida.Visitante.Marcacao, partida.Visitante.Concentracao));
        Assert.Equal(TipoEvento.TrocaDeEstilo, evento.Tipo);
    }

    [Fact]
    public void CapitaoForaDoTime_Lanca()
    {
        var (m, _) = Apoio.DoisTimes();

        Assert.Throws<ArgumentException>(() => Com(m, capitao: m.Reservas[0].Id));
    }

    [Fact]
    public void Padrao_CapitaoEOMaisDeterminado()
    {
        var (m, _) = Apoio.DoisTimes();

        Assert.Equal(m.Titulares.OrderByDescending(j => j.Atributos.Determinacao).ThenBy(j => j.Id).First().Id, m.CapitaoEfetivo);
        Assert.Null(m.CobradorId);
        Assert.Null(MotorDePartida.Iniciar(m, Apoio.DoisTimes().Visitante, new GeradorAleatorio(1)).Mandante.Cobrador);
    }
}
