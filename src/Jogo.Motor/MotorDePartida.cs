using Jogo.Dominio.Aleatoriedade;
using Jogo.Dominio.Jogadores;

namespace Jogo.Motor;

/// <summary>
/// Motor estatístico de partida, minuto a minuto (sem física):
/// 1. força dos 4 setores de cada time; 2. a cada minuto, a posse é sorteada pela força do meio-campo;
/// 3. o time com a bola pode criar um lance de perigo; 4. duelo ataque × defesa;
/// 5. finalização (gol, defesa, trave, travessão, fora, por cima) ou lance secundário (escanteio, falta).
/// </summary>
public static class MotorDePartida
{
    private const int MinutosPorTempo = 45;

    public static Partida Iniciar(
        TimeEscalado mandante,
        TimeEscalado visitante,
        IGeradorAleatorio aleatorio,
        CondicoesPartida? condicoes = null,
        ConfiguracaoMotor? configuracao = null,
        bool mandanteAutomatico = true,
        bool visitanteAutomatico = true)
    {
        ArgumentNullException.ThrowIfNull(mandante);
        ArgumentNullException.ThrowIfNull(visitante);
        ArgumentNullException.ThrowIfNull(aleatorio);

        var ids = mandante.Titulares.Concat(mandante.Reservas).Select(j => j.Id).ToHashSet();
        if (visitante.Titulares.Concat(visitante.Reservas).Any(j => ids.Contains(j.Id)))
        {
            throw new ArgumentException("Um jogador não pode estar nos dois times.", nameof(visitante));
        }

        var partida = new Partida(
            new TimeEmJogo(Equipe.Mandante, mandante, mandanteAutomatico),
            new TimeEmJogo(Equipe.Visitante, visitante, visitanteAutomatico),
            condicoes ?? CondicoesPartida.Padrao,
            configuracao ?? ConfiguracaoMotor.Padrao,
            aleatorio,
            acrescimoPrimeiroTempo: aleatorio.Proximo(0, 4),
            acrescimoSegundoTempo: aleatorio.Proximo(1, 7));

        partida.Registrar(TipoEvento.InicioDeJogo);
        return partida;
    }

    /// <summary>Simula do início ao fim sem intervenção (os dois times no automático).</summary>
    public static ResultadoPartida SimularRapido(
        TimeEscalado mandante,
        TimeEscalado visitante,
        IGeradorAleatorio aleatorio,
        CondicoesPartida? condicoes = null,
        ConfiguracaoMotor? configuracao = null) =>
        SimularAteOFim(Iniciar(mandante, visitante, aleatorio, condicoes, configuracao));

    public static ResultadoPartida SimularAteOFim(Partida partida)
    {
        ArgumentNullException.ThrowIfNull(partida);
        while (!partida.Encerrada)
        {
            AvancarMinuto(partida);
        }

        return ResultadoPartida.De(partida);
    }

    /// <summary>Joga um minuto e devolve o que aconteceu nele.</summary>
    public static IReadOnlyList<EventoPartida> AvancarMinuto(Partida partida)
    {
        ArgumentNullException.ThrowIfNull(partida);
        if (partida.Encerrada)
        {
            return [];
        }

        var antes = partida.Eventos.Count;
        partida.MinutoNoTempo++;
        partida.MinutosCorridos++;

        Desgastar(partida, partida.Mandante);
        Desgastar(partida, partida.Visitante);

        JogarLance(partida);

        foreach (var time in new[] { partida.Mandante, partida.Visitante })
        {
            if (time.Automatico)
            {
                foreach (var decisao in TreinadorAutomatico.Decidir(partida, time))
                {
                    AplicarDecisao(partida, time.Equipe, decisao);
                }
            }
        }

        var duracao = MinutosPorTempo + (partida.Tempo == 1 ? partida.AcrescimoPrimeiroTempo : partida.AcrescimoSegundoTempo);
        if (partida.MinutoNoTempo >= duracao)
        {
            if (partida.Tempo == 1)
            {
                partida.Registrar(TipoEvento.Intervalo);
                partida.Tempo = 2;
                partida.MinutoNoTempo = 0;
            }
            else
            {
                partida.Registrar(TipoEvento.FimDeJogo);
                partida.Encerrada = true;
                foreach (var jogador in partida.Mandante.EmCampo.Concat(partida.Visitante.EmCampo))
                {
                    jogador.SaiuNoMinuto = partida.MinutosCorridos;
                }
            }
        }

        return [.. partida.Eventos.Skip(antes)];
    }

    /// <summary>Aplica uma decisão do treinador. Lança <see cref="InvalidOperationException"/> se ela não for permitida.</summary>
    public static EventoPartida AplicarDecisao(Partida partida, Equipe equipe, Decisao decisao)
    {
        ArgumentNullException.ThrowIfNull(partida);
        ArgumentNullException.ThrowIfNull(decisao);
        if (partida.Encerrada)
        {
            throw new InvalidOperationException("A partida já terminou.");
        }

        var time = partida.Time(equipe);
        return decisao switch
        {
            Substituicao s => Substituir(partida, time, s),
            TrocaDeMentalidade m => TrocarMentalidade(partida, time, m.Nova),
            TrocaDeEstilo e => TrocarEstilo(partida, time, e),
            TrocaDeFormacao f => TrocarFormacao(partida, time, f),
            _ => throw new InvalidOperationException("Decisão desconhecida."),
        };
    }

    // ------------------------------------------------------------------ força

    /// <summary>Força atual dos setores de um time (com condição, moral, mando e jogadores a menos).</summary>
    public static ForcaSetores Forca(Partida partida, TimeEmJogo time)
    {
        ArgumentNullException.ThrowIfNull(partida);
        ArgumentNullException.ThrowIfNull(time);

        var mando = Mando(partida, time) * (1 + time.BonusEntrosamento) * Lideranca(partida.Configuracao, time);
        var forca = Setores.Calcular(
            time.EmCampo.Select(j => (j.Posicao, j.ForcaBase * FatorIndividual(partida.Configuracao, j) * mando)),
            partida.Configuracao.ExpoenteOcupacao);

        return forca;
    }

    /// <summary>Bônus do capitão enquanto ele estiver em campo, proporcional à Determinação.</summary>
    private static double Lideranca(ConfiguracaoMotor cfg, TimeEmJogo time) =>
        time.EmCampoInterno.Find(j => j.Jogador.Id == time.CapitaoId) is { } capitao
            ? 1 + (cfg.BonusCapitao * capitao.Jogador.Atributos.Determinacao / 20.0)
            : 1;

    // Lados do campo para "concentrar ataques": quem ataca por um lado e quem defende aquele lado no rival.
    private static readonly Posicao[] AtaqueEsquerda = [Posicao.LateralEsquerdo, Posicao.AlaEsquerdo, Posicao.MeiaEsquerdo, Posicao.PontaEsquerda];
    private static readonly Posicao[] AtaqueDireita = [Posicao.LateralDireito, Posicao.AlaDireito, Posicao.MeiaDireito, Posicao.PontaDireita];
    private static readonly Posicao[] AtaqueMeio = [Posicao.MeiaCentral, Posicao.MeiaOfensivo, Posicao.SegundoAtacante, Posicao.Centroavante];
    private static readonly Posicao[] DefesaDireita = [Posicao.LateralDireito, Posicao.AlaDireito, Posicao.MeiaDireito];
    private static readonly Posicao[] DefesaEsquerda = [Posicao.LateralEsquerdo, Posicao.AlaEsquerdo, Posicao.MeiaEsquerdo];
    private static readonly Posicao[] DefesaMeio = [Posicao.Zagueiro, Posicao.Volante];

    /// <summary>
    /// Multiplicador do ataque por concentrar as jogadas num lado: compara a força média de quem ataca
    /// por ali com a de quem defende aquele lado no adversário. "Variado" é neutro.
    /// </summary>
    public static double FatorConcentracao(Partida partida, TimeEmJogo atacante, TimeEmJogo defensor)
    {
        ArgumentNullException.ThrowIfNull(partida);
        ArgumentNullException.ThrowIfNull(atacante);
        ArgumentNullException.ThrowIfNull(defensor);
        var cfg = partida.Configuracao;
        Posicao[] meus, deles;
        switch (atacante.Concentracao)
        {
            // Atacar pela esquerda é enfrentar o lado direito da defesa rival (e vice-versa).
            case Concentracao.Esquerda:
                (meus, deles) = (AtaqueEsquerda, DefesaDireita);
                break;
            case Concentracao.Direita:
                (meus, deles) = (AtaqueDireita, DefesaEsquerda);
                break;
            case Concentracao.Meio:
                (meus, deles) = (AtaqueMeio, DefesaMeio);
                break;
            default:
                return 1;
        }

        double? Media(TimeEmJogo time, Posicao[] posicoes)
        {
            var valores = time.EmCampoInterno.Where(j => posicoes.Contains(j.Posicao))
                .Select(j => j.ForcaBase * FatorIndividual(cfg, j)).ToArray();
            return valores.Length == 0 ? null : valores.Average();
        }

        var ataque = Media(atacante, meus);
        var defesa = Media(defensor, deles);
        double vantagem;
        if (ataque is null)
        {
            vantagem = -cfg.ConcentracaoReferencia; // ninguém para atacar por ali
        }
        else if (defesa is null)
        {
            vantagem = cfg.ConcentracaoReferencia; // ninguém defendendo aquele lado
        }
        else
        {
            vantagem = (ataque.Value / defesa.Value) - 1;
        }

        return 1 + (cfg.ConcentracaoMaxima * Math.Clamp(vantagem / cfg.ConcentracaoReferencia, -1, 1));
    }

    private static double Mando(Partida partida, TimeEmJogo time) =>
        time.Equipe == Equipe.Mandante && !partida.Condicoes.CampoNeutro ? 1 + partida.Configuracao.BonusMando : 1;

    private static double FatorIndividual(ConfiguracaoMotor cfg, JogadorEmCampo j)
    {
        var condicao = cfg.RendimentoSemCondicao + ((1 - cfg.RendimentoSemCondicao) * j.Condicao / 100);
        var moral = 1 + (cfg.EfeitoMoral * (j.Jogador.Moral - 50) / 50.0);
        return condicao * moral;
    }

    // ------------------------------------------------------------------ minuto

    private static void Desgastar(Partida partida, TimeEmJogo time)
    {
        var cfg = partida.Configuracao;
        // Cópia: uma lesão remove o jogador da lista durante o laço.
        foreach (var jogador in time.EmCampo.ToArray())
        {
            var resistencia = jogador.Jogador.Atributos.Resistencia;
            var marcacao = 1 + (cfg.MarcacaoDesgaste * (int)time.Marcacao);
            jogador.Condicao = Math.Max(0, jogador.Condicao - (cfg.DesgastePorMinuto * (1.3 - (0.03 * resistencia)) * marcacao));

            var chanceLesao = cfg.ChanceLesaoPorMinuto * (jogador.Jogador.Ocultos.PropensaoLesao / 10.0)
                              * (1 + ((100 - jogador.Condicao) / 100));
            if (partida.Aleatorio.Chance(chanceLesao))
            {
                Lesionar(partida, time, jogador);
            }
        }
    }

    private static void Lesionar(Partida partida, TimeEmJogo time, JogadorEmCampo jogador)
    {
        var sorteio = partida.Aleatorio.Proximo(0, 100);
        jogador.RodadasDeLesao = sorteio switch
        {
            < 70 => partida.Aleatorio.Proximo(1, 3),
            < 95 => partida.Aleatorio.Proximo(3, 6),
            _ => partida.Aleatorio.Proximo(6, 13),
        };

        Retirar(partida, time, jogador);

        // Goleiro lesionado sem goleiro no banco: um jogador de linha vai para o gol
        // e a vaga aberta passa a ser a dele.
        var vaga = jogador.Posicao;
        if (vaga == Posicao.Goleiro && !time.Reservas.Any(r => r.PosicaoPrincipal == Posicao.Goleiro)
            && GarantirGoleiro(time) is { } improvisado)
        {
            vaga = improvisado;
        }

        time.VagasAbertas.Add((jogador.Jogador.Id, vaga));
        partida.Registrar(TipoEvento.Lesao, time.Equipe, jogador.Jogador.Id,
            detalhe: jogador.RodadasDeLesao.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    private static void JogarLance(Partida partida)
    {
        var cfg = partida.Configuracao;
        var forcaMandante = Forca(partida, partida.Mandante);
        var forcaVisitante = Forca(partida, partida.Visitante);

        var posseMandante = Disputa(forcaMandante.Meio, forcaVisitante.Meio, cfg.ExpoentePosse);
        var atacante = partida.Aleatorio.Chance(posseMandante) ? partida.Mandante : partida.Visitante;
        var defensor = partida.Adversario(atacante.Equipe);
        atacante.Estatisticas.MinutosComPosse++;

        // Falta comum, sem perigo: quem está sem a bola tenta recuperá-la.
        if (partida.Aleatorio.Chance(cfg.ChanceFaltaPorMinuto * (1 + (cfg.MarcacaoFaltas * (int)defensor.Marcacao))))
        {
            CometerFalta(partida, defensor);
        }

        var nivelAtaque = (int)atacante.Mentalidade;
        var nivelDefesa = (int)defensor.Mentalidade;
        var chanceDeLance = cfg.ChanceDeLance * (1 + (cfg.MentalidadeLances * nivelAtaque) + (cfg.MentalidadeLances * 0.5 * nivelDefesa))
                            * (1 - (cfg.MarcacaoLances * (int)defensor.Marcacao));
        if (!partida.Aleatorio.Chance(chanceDeLance))
        {
            return;
        }

        var forcaAtaque = atacante.Equipe == Equipe.Mandante ? forcaMandante : forcaVisitante;
        var forcaDefesa = defensor.Equipe == Equipe.Mandante ? forcaMandante : forcaVisitante;
        var ataque = forcaAtaque.Ataque * (1 + (cfg.MentalidadeAtaque * nivelAtaque)) * FatorConcentracao(partida, atacante, defensor);
        var defesa = forcaDefesa.Defesa * (1 - (cfg.MentalidadeDefesa * nivelDefesa));

        if (partida.Aleatorio.Chance(Disputa(ataque, defesa, cfg.ExpoenteDuelo)))
        {
            Finalizar(partida, atacante, defensor, OrigemLance.JogadaTrabalhada, 1.0);
            return;
        }

        var desfecho = partida.Aleatorio.ProximoReal();
        if (desfecho < cfg.ChanceEscanteio)
        {
            atacante.Estatisticas.Escanteios++;
            partida.Registrar(TipoEvento.Escanteio, atacante.Equipe);
            if (partida.Aleatorio.Chance(cfg.ChanceFinalizacaoDeEscanteio))
            {
                Finalizar(partida, atacante, defensor, OrigemLance.Escanteio, 0.8 * FatorCobranca(atacante));
            }
        }
        else if (desfecho < cfg.ChanceEscanteio + cfg.ChanceFalta)
        {
            CometerFalta(partida, defensor);
            if (!partida.Encerrada && partida.Aleatorio.Chance(cfg.ChanceFinalizacaoDeFalta))
            {
                Finalizar(partida, atacante, defensor, OrigemLance.Falta, 0.9);
            }
        }

        // Senão: a defesa afastou o perigo (sem evento).
    }

    /// <summary>Qualidade do cruzamento no escanteio pelo Passe do cobrador (Passe 10 = neutro, 20 = +15%).</summary>
    private static double FatorCobranca(TimeEmJogo time) =>
        time.Cobrador is { } c ? 0.85 + (0.015 * c.Jogador.Atributos.Passe) : 1;

    /// <summary>Probabilidade de A vencer B numa disputa: A^k / (A^k + B^k).</summary>
    private static double Disputa(double a, double b, double expoente)
    {
        if (a <= 0 && b <= 0)
        {
            return 0.5;
        }

        var pa = Math.Pow(Math.Max(a, 0), expoente);
        var pb = Math.Pow(Math.Max(b, 0), expoente);
        return pa / (pa + pb);
    }

    private static void Finalizar(Partida partida, TimeEmJogo atacante, TimeEmJogo defensor, OrigemLance origem, double fatorLance)
    {
        var cfg = partida.Configuracao;
        // Quem finaliza: atacantes mais, meias também; em escanteio, zagueiros sobem para cabecear.
        // Peso linear na Finalização para os gols não se concentrarem demais num jogador só.
        // Falta direta: quem bate é o cobrador.
        var finalizador = origem == OrigemLance.Falta && atacante.Cobrador is { } cobrador ? cobrador : SortearPorPeso(partida, atacante.EmCampo, j =>
        {
            var (defesa, meio, ataque) = Setores.Contribuicao(j.Posicao);
            var participacao = ataque + (0.4 * meio) + 0.05 + (origem == OrigemLance.Escanteio ? 0.5 * defesa : 0);
            return participacao * j.Jogador.Atributos.Finalizacao;
        });

        if (finalizador is null)
        {
            return;
        }

        atacante.Estatisticas.Finalizacoes++;
        var atributos = finalizador.Jogador.Atributos;
        var qualidade = (origem == OrigemLance.Falta
                            ? (0.55 * atributos.Finalizacao) + (0.45 * atributos.Passe)
                            : (0.65 * atributos.Finalizacao) + (0.35 * atributos.Decisao))
                        * Setores.FatorImproviso(finalizador.Jogador, finalizador.Posicao)
                        * FatorIndividual(cfg, finalizador) * Mando(partida, atacante) * fatorLance;

        var goleiro = defensor.EmCampo.FirstOrDefault(j => j.Posicao == Posicao.Goleiro);
        var nivelGoleiro = goleiro is null
            ? 3.0
            : ((0.5 * goleiro.Jogador.Atributos.Reflexos) + (0.35 * goleiro.Jogador.Atributos.Posicionamento)
               + (0.15 * goleiro.Jogador.Atributos.Decisao))
              * Setores.FatorImproviso(goleiro.Jogador, Posicao.Goleiro) * FatorIndividual(cfg, goleiro) * Mando(partida, defensor);

        var chanceNoAlvo = Math.Clamp(cfg.ChanceNoAlvoBase + (0.015 * (qualidade - 10)), 0.12, 0.65);
        if (!partida.Aleatorio.Chance(chanceNoAlvo))
        {
            var errou = partida.Aleatorio.ProximoReal() switch
            {
                < 0.08 => TipoEvento.FinalizacaoNaTrave,
                < 0.11 => TipoEvento.FinalizacaoNoTravessao,
                < 0.45 => TipoEvento.FinalizacaoPorCima,
                _ => TipoEvento.FinalizacaoParaFora,
            };
            partida.Registrar(errou, atacante.Equipe, finalizador.Jogador.Id, origem: origem);
            return;
        }

        atacante.Estatisticas.NoAlvo++;
        var chanceGol = Math.Clamp(cfg.ChanceGolNoAlvoBase * Math.Pow(qualidade / nivelGoleiro, cfg.ExpoenteGol), 0.04, 0.85);

        // Trava anti-goleada: quem já vence por 3 ou mais tem a chance reduzida a cada gol a mais.
        var vantagem = atacante.Estatisticas.Gols - defensor.Estatisticas.Gols;
        if (vantagem >= 3)
        {
            chanceGol *= Math.Pow(cfg.TravaGoleada, vantagem - 2);
        }

        if (partida.Aleatorio.Chance(chanceGol))
        {
            atacante.Estatisticas.Gols++;
            finalizador.Gols++;
            partida.Registrar(TipoEvento.Gol, atacante.Equipe, finalizador.Jogador.Id, origem: origem);
        }
        else
        {
            partida.Registrar(TipoEvento.FinalizacaoDefendida, atacante.Equipe, finalizador.Jogador.Id, goleiro?.Jogador.Id, origem);
        }
    }

    private static void CometerFalta(Partida partida, TimeEmJogo defensor)
    {
        var cfg = partida.Configuracao;
        var faltoso = SortearPorPeso(partida, defensor.EmCampo, j =>
        {
            var (def, meio, _) = Setores.Contribuicao(j.Posicao);
            return def + (0.6 * meio) + 0.1;
        });

        if (faltoso is null)
        {
            return;
        }

        defensor.Estatisticas.Faltas++;
        partida.Registrar(TipoEvento.Falta, defensor.Equipe, faltoso.Jogador.Id);

        if (partida.Aleatorio.Chance(cfg.ChanceVermelhoDiretoPorFalta))
        {
            Expulsar(partida, defensor, faltoso);
        }
        // Quem já tem amarelo joga com mais cuidado.
        else if (partida.Aleatorio.Chance(cfg.ChanceAmareloPorFalta * (faltoso.Amarelos > 0 ? cfg.CuidadoDeQuemTemAmarelo : 1)))
        {
            faltoso.Amarelos++;
            defensor.Estatisticas.Amarelos++;
            partida.Registrar(TipoEvento.CartaoAmarelo, defensor.Equipe, faltoso.Jogador.Id);
            if (faltoso.Amarelos >= 2)
            {
                Expulsar(partida, defensor, faltoso);
            }
        }
    }

    private static void Expulsar(Partida partida, TimeEmJogo time, JogadorEmCampo jogador)
    {
        jogador.Expulso = true;
        time.Estatisticas.Vermelhos++;
        Retirar(partida, time, jogador);
        partida.Registrar(TipoEvento.CartaoVermelho, time.Equipe, jogador.Jogador.Id);

        if (jogador.Posicao == Posicao.Goleiro)
        {
            GarantirGoleiro(time);
        }
    }

    /// <summary>
    /// Sem goleiro em campo, quem tiver mais jeito para o gol assume a posição.
    /// Devolve a posição que esse jogador ocupava (ou nulo se não houve troca).
    /// </summary>
    private static Posicao? GarantirGoleiro(TimeEmJogo time)
    {
        if (time.EmCampo.Count == 0 || time.EmCampo.Any(j => j.Posicao == Posicao.Goleiro))
        {
            return null;
        }

        var substituto = time.EmCampo.MaxBy(j => j.Jogador.ForcaNaPosicao(Posicao.Goleiro))!;
        var anterior = substituto.Posicao;
        substituto.Posicao = Posicao.Goleiro;
        return anterior;
    }

    private static void Retirar(Partida partida, TimeEmJogo time, JogadorEmCampo jogador)
    {
        jogador.SaiuNoMinuto = partida.MinutosCorridos;
        time.EmCampoInterno.Remove(jogador);
    }

    private static JogadorEmCampo? SortearPorPeso(Partida partida, IReadOnlyList<JogadorEmCampo> jogadores, Func<JogadorEmCampo, double> peso)
    {
        var total = jogadores.Sum(peso);
        if (total <= 0)
        {
            return null;
        }

        var alvo = partida.Aleatorio.ProximoReal() * total;
        foreach (var jogador in jogadores)
        {
            alvo -= peso(jogador);
            if (alvo < 0)
            {
                return jogador;
            }
        }

        return jogadores[^1];
    }

    // ------------------------------------------------------------------ decisões

    private static EventoPartida Substituir(Partida partida, TimeEmJogo time, Substituicao s)
    {
        if (time.SubstituicoesFeitas >= partida.Configuracao.MaximoSubstituicoes)
        {
            throw new InvalidOperationException($"Limite de {partida.Configuracao.MaximoSubstituicoes} substituições atingido.");
        }

        var entra = time.ReservasInterno.Find(j => j.Id == s.EntraId)
            ?? throw new InvalidOperationException("Quem vai entrar não está entre os reservas disponíveis.");

        Posicao posicao;
        int indice;
        var emCampo = time.EmCampoInterno.FindIndex(j => j.Jogador.Id == s.SaiId);
        var vaga = time.VagasAbertas.FindIndex(v => v.JogadorId == s.SaiId);
        if (emCampo >= 0)
        {
            var sai = time.EmCampoInterno[emCampo];
            posicao = sai.Posicao;
            indice = emCampo;
            Retirar(partida, time, sai);
        }
        else if (vaga >= 0)
        {
            posicao = time.VagasAbertas[vaga].Posicao;
            indice = time.EmCampoInterno.Count;
            time.VagasAbertas.RemoveAt(vaga);
        }
        else
        {
            throw new InvalidOperationException("Quem vai sair não está em campo.");
        }

        var novo = new JogadorEmCampo(entra, posicao, partida.MinutosCorridos);
        time.EmCampoInterno.Insert(Math.Min(indice, time.EmCampoInterno.Count), novo);
        time.TodosInterno.Add(novo);
        time.ReservasInterno.Remove(entra);
        time.SubstituicoesFeitas++;
        return partida.Registrar(TipoEvento.Substituicao, time.Equipe, s.SaiId, s.EntraId);
    }

    private static EventoPartida TrocarMentalidade(Partida partida, TimeEmJogo time, Mentalidade nova)
    {
        time.Mentalidade = nova;
        return partida.Registrar(TipoEvento.TrocaDeMentalidade, time.Equipe, detalhe: nova.ToString());
    }

    private static EventoPartida TrocarEstilo(Partida partida, TimeEmJogo time, TrocaDeEstilo troca)
    {
        time.Marcacao = troca.Marcacao;
        time.Concentracao = troca.Concentracao;
        return partida.Registrar(TipoEvento.TrocaDeEstilo, time.Equipe, detalhe: troca.Marcacao + "/" + troca.Concentracao);
    }

    private static EventoPartida TrocarFormacao(Partida partida, TimeEmJogo time, TrocaDeFormacao troca)
    {
        var deLinha = time.EmCampoInterno.Where(j => j.Posicao != Posicao.Goleiro).ToList();
        if (troca.JogadoresNaOrdem.Count != deLinha.Count
            || !troca.JogadoresNaOrdem.Order().SequenceEqual(deLinha.Select(j => j.Jogador.Id).Order()))
        {
            throw new InvalidOperationException("A nova formação precisa listar exatamente os jogadores de linha em campo.");
        }

        for (var i = 0; i < troca.JogadoresNaOrdem.Count; i++)
        {
            deLinha.First(j => j.Jogador.Id == troca.JogadoresNaOrdem[i]).Posicao = troca.Nova.Posicoes[i];
        }

        time.Formacao = troca.Nova;
        return partida.Registrar(TipoEvento.TrocaDeFormacao, time.Equipe, detalhe: troca.Nova.Nome);
    }
}
