using Jogo.Dominio.Aleatoriedade;
using Jogo.Dominio.Jogadores;

namespace Jogo.Motor;

/// <summary>Um jogador que está (ou esteve) em campo nesta partida.</summary>
public sealed class JogadorEmCampo
{
    private Posicao _posicao;

    internal JogadorEmCampo(Jogador jogador, Posicao posicao, int entrouNoMinuto)
    {
        Jogador = jogador;
        Posicao = posicao;
        Condicao = jogador.Condicao;
        EntrouNoMinuto = entrouNoMinuto;
    }

    public Jogador Jogador { get; }

    public Posicao Posicao
    {
        get => _posicao;
        internal set
        {
            _posicao = value;
            ForcaBase = Jogador.ForcaNaPosicao(value) * Setores.FatorImproviso(Jogador, value);
        }
    }

    /// <summary>Força na posição atual já com o fator de improviso (sem condição, moral e mando).</summary>
    public double ForcaBase { get; private set; }

    /// <summary>Condição física durante o jogo (0-100), cai a cada minuto.</summary>
    public double Condicao { get; internal set; }

    public int EntrouNoMinuto { get; }
    public int? SaiuNoMinuto { get; internal set; }
    public int Gols { get; internal set; }
    public int Amarelos { get; internal set; }
    public bool Expulso { get; internal set; }

    /// <summary>Rodadas que vai desfalcar por lesão (0 = não se lesionou).</summary>
    public int RodadasDeLesao { get; internal set; }

    public int MinutosJogados(int minutoAtual) => (SaiuNoMinuto ?? minutoAtual) - EntrouNoMinuto;
}

/// <summary>Estado de um dos times durante a partida.</summary>
public sealed class TimeEmJogo
{
    internal TimeEmJogo(Equipe equipe, TimeEscalado escalado, bool automatico)
    {
        Equipe = equipe;
        ClubeId = escalado.ClubeId;
        Formacao = escalado.Formacao;
        Mentalidade = escalado.Mentalidade;
        Automatico = automatico;

        var posicoes = escalado.Formacao.PosicoesComGoleiro;
        for (var i = 0; i < posicoes.Count; i++)
        {
            var jogador = new JogadorEmCampo(escalado.Titulares[i], posicoes[i], 0);
            EmCampoInterno.Add(jogador);
            TodosInterno.Add(jogador);
        }

        ReservasInterno.AddRange(escalado.Reservas);
    }

    public Equipe Equipe { get; }
    public string ClubeId { get; }
    public Formacao Formacao { get; internal set; }
    public Mentalidade Mentalidade { get; internal set; }

    /// <summary>Se verdadeiro, o treinador automático faz substituições e muda a mentalidade.</summary>
    public bool Automatico { get; }

    public int SubstituicoesFeitas { get; internal set; }
    public EstatisticasTime Estatisticas { get; } = new();

    internal List<JogadorEmCampo> EmCampoInterno { get; } = [];
    internal List<Jogador> ReservasInterno { get; } = [];
    internal List<JogadorEmCampo> TodosInterno { get; } = [];

    /// <summary>Posições deixadas por jogadores lesionados, à espera de substituição.</summary>
    internal List<(int JogadorId, Posicao Posicao)> VagasAbertas { get; } = [];

    /// <summary>Quem está em campo agora, na ordem da formação.</summary>
    public IReadOnlyList<JogadorEmCampo> EmCampo => EmCampoInterno;

    /// <summary>Reservas que ainda podem entrar.</summary>
    public IReadOnlyList<Jogador> Reservas => ReservasInterno;

    /// <summary>Todos que entraram em campo (titulares e substitutos), para o pós-jogo.</summary>
    public IReadOnlyList<JogadorEmCampo> Participantes => TodosInterno;
}

/// <summary>
/// Uma partida em andamento. Avança minuto a minuto com <see cref="MotorDePartida.AvancarMinuto"/>.
/// Todo sorteio usa o gerador da própria partida: mesma seed + mesmas decisões = mesmo jogo.
/// </summary>
public sealed class Partida
{
    private readonly List<EventoPartida> _eventos = [];

    internal Partida(
        TimeEmJogo mandante,
        TimeEmJogo visitante,
        CondicoesPartida condicoes,
        ConfiguracaoMotor configuracao,
        IGeradorAleatorio aleatorio,
        int acrescimoPrimeiroTempo,
        int acrescimoSegundoTempo)
    {
        Mandante = mandante;
        Visitante = visitante;
        Condicoes = condicoes;
        Configuracao = configuracao;
        Aleatorio = aleatorio;
        AcrescimoPrimeiroTempo = acrescimoPrimeiroTempo;
        AcrescimoSegundoTempo = acrescimoSegundoTempo;
    }

    public TimeEmJogo Mandante { get; }
    public TimeEmJogo Visitante { get; }
    public CondicoesPartida Condicoes { get; }
    public ConfiguracaoMotor Configuracao { get; }
    internal IGeradorAleatorio Aleatorio { get; }

    public int AcrescimoPrimeiroTempo { get; }
    public int AcrescimoSegundoTempo { get; }

    /// <summary>1 ou 2.</summary>
    public int Tempo { get; internal set; } = 1;

    /// <summary>Minutos jogados no tempo atual (inclui acréscimos).</summary>
    public int MinutoNoTempo { get; internal set; }

    /// <summary>Minutos de bola rolando desde o início (para contar minutos jogados).</summary>
    public int MinutosCorridos { get; internal set; }

    public bool Encerrada { get; internal set; }

    public IReadOnlyList<EventoPartida> Eventos => _eventos;

    public TimeEmJogo Time(Equipe equipe) => equipe == Equipe.Mandante ? Mandante : Visitante;

    public TimeEmJogo Adversario(Equipe equipe) => equipe == Equipe.Mandante ? Visitante : Mandante;

    /// <summary>Minuto do relógio e acréscimo, como aparece no placar (ex.: 45+2).</summary>
    public (int Minuto, int Acrescimo) Relogio
    {
        get
        {
            var basico = Math.Min(MinutoNoTempo, 45);
            var extra = Math.Max(0, MinutoNoTempo - 45);
            return (Tempo == 1 ? basico : 45 + basico, extra);
        }
    }

    internal EventoPartida Registrar(TipoEvento tipo, Equipe? equipe = null, int? jogadorId = null,
        int? outroJogadorId = null, OrigemLance? origem = null, string? detalhe = null)
    {
        var (minuto, acrescimo) = Relogio;
        var evento = new EventoPartida(minuto, acrescimo, tipo, equipe, jogadorId, outroJogadorId, origem, detalhe);
        _eventos.Add(evento);
        return evento;
    }
}
