namespace Jogo.Dominio.Jogadores;

/// <summary>
/// Um jogador. Tem uma posição principal e até duas secundárias, nas quais rende 100%.
/// A força geral exibida é sempre a da posição principal.
/// </summary>
public sealed class Jogador
{
    public const int IdadeMinima = 15;
    public const int IdadeMaxima = 45;
    public const int MaximoPosicoesSecundarias = 2;
    public const int EscalaMaxima = 100;

    public Jogador(
        int id,
        string nome,
        int idade,
        Posicao posicaoPrincipal,
        IEnumerable<Posicao> posicoesSecundarias,
        Atributos atributos,
        AtributosOcultos ocultos)
    {
        ArgumentNullException.ThrowIfNull(posicoesSecundarias);
        ArgumentNullException.ThrowIfNull(atributos);
        ArgumentNullException.ThrowIfNull(ocultos);

        Id = Validacao.Faixa(id, 1, int.MaxValue, nameof(id));
        Nome = Validacao.Texto(nome, nameof(nome));
        Idade = Validacao.Faixa(idade, IdadeMinima, IdadeMaxima, nameof(idade));
        PosicaoPrincipal = posicaoPrincipal;
        PosicoesSecundarias = ValidarSecundarias(posicaoPrincipal, posicoesSecundarias);
        Atributos = atributos;
        Ocultos = ocultos;
    }

    public int Id { get; }
    public string Nome { get; }
    public int Idade { get; private set; }
    public Posicao PosicaoPrincipal { get; }
    public IReadOnlyList<Posicao> PosicoesSecundarias { get; }
    public Atributos Atributos { get; private set; }
    public AtributosOcultos Ocultos { get; }

    /// <summary>Condição física, 0 (exausto) a 100 (descansado).</summary>
    public int Condicao { get; private set; } = EscalaMaxima;

    /// <summary>Moral individual, 0 a 100. A moral do time é a média dos titulares (D-012).</summary>
    public int Moral { get; private set; } = 50;

    /// <summary>Contrato atual; nulo quando o jogador está sem clube.</summary>
    public Contrato? Contrato { get; private set; }

    public int ForcaGeral => CalculadoraForca.Calcular(Atributos, PosicaoPrincipal);

    /// <summary>Força calculada com os pesos de outra posição (sem penalidade de improviso, que é do motor).</summary>
    public int ForcaNaPosicao(Posicao posicao) => CalculadoraForca.Calcular(Atributos, posicao);

    /// <summary>Verdadeiro se a posição é a principal ou uma das secundárias.</summary>
    public bool JogaNaPosicao(Posicao posicao) =>
        posicao == PosicaoPrincipal || PosicoesSecundarias.Contains(posicao);

    public void AjustarMoral(int variacao) => Moral = Math.Clamp(Moral + variacao, 0, EscalaMaxima);

    public void AjustarCondicao(int variacao) => Condicao = Math.Clamp(Condicao + variacao, 0, EscalaMaxima);

    public void AssinarContrato(Contrato contrato)
    {
        ArgumentNullException.ThrowIfNull(contrato);
        Contrato = contrato;
    }

    public void EncerrarContrato() => Contrato = null;

    /// <summary>Envelhece um ano (chamado no fim da temporada).</summary>
    public void Envelhecer() => Idade = Validacao.Faixa(Idade + 1, IdadeMinima, IdadeMaxima, nameof(Idade));

    /// <summary>Substitui os atributos (evolução/declínio no fim da temporada).</summary>
    public void AtualizarAtributos(Atributos novos)
    {
        ArgumentNullException.ThrowIfNull(novos);
        Atributos = novos;
    }

    // Estado de carreira (muda a cada rodada; não faz parte das definições JSON) ----------

    /// <summary>Rodadas que ainda vai desfalcar por lesão.</summary>
    public int RodadasLesionado { get; private set; }

    /// <summary>Rodadas que ainda vai cumprir de suspensão.</summary>
    public int RodadasSuspenso { get; private set; }

    /// <summary>Amarelos acumulados desde a última suspensão por cartões.</summary>
    public int AmarelosAcumulados { get; private set; }

    /// <summary>Rodadas seguidas como titular (base do entrosamento).</summary>
    public int SequenciaComoTitular { get; private set; }

    /// <summary>Pode ser escalado (não está lesionado nem suspenso).</summary>
    public bool Disponivel => RodadasLesionado == 0 && RodadasSuspenso == 0;

    public void Lesionar(int rodadas) =>
        RodadasLesionado = Math.Max(RodadasLesionado, Validacao.Faixa(rodadas, 1, 60, nameof(rodadas)));

    public void Suspender(int rodadas) => RodadasSuspenso += Validacao.Faixa(rodadas, 1, 20, nameof(rodadas));

    public void AdicionarAmarelos(int quantidade) => AmarelosAcumulados += Validacao.Faixa(quantidade, 0, 2, nameof(quantidade));

    /// <summary>Desconta amarelos que viraram suspensão.</summary>
    public void DescontarAmarelos(int quantidade) =>
        AmarelosAcumulados = Math.Max(0, AmarelosAcumulados - Validacao.Faixa(quantidade, 0, 99, nameof(quantidade)));

    public void RegistrarRodada(bool titular) => SequenciaComoTitular = titular ? SequenciaComoTitular + 1 : 0;

    /// <summary>Uma rodada passou: desconta lesão e suspensão.</summary>
    public void CumprirRodada()
    {
        RodadasLesionado = Math.Max(0, RodadasLesionado - 1);
        RodadasSuspenso = Math.Max(0, RodadasSuspenso - 1);
    }

    /// <summary>Restaura o estado de carreira lido de um save.</summary>
    public void RestaurarEstado(int condicao, int moral, int rodadasLesionado, int rodadasSuspenso, int amarelosAcumulados, int sequenciaComoTitular)
    {
        Condicao = Validacao.Faixa(condicao, 0, EscalaMaxima, nameof(condicao));
        Moral = Validacao.Faixa(moral, 0, EscalaMaxima, nameof(moral));
        RodadasLesionado = Validacao.Faixa(rodadasLesionado, 0, 60, nameof(rodadasLesionado));
        RodadasSuspenso = Validacao.Faixa(rodadasSuspenso, 0, 20, nameof(rodadasSuspenso));
        AmarelosAcumulados = Validacao.Faixa(amarelosAcumulados, 0, 99, nameof(amarelosAcumulados));
        SequenciaComoTitular = Validacao.Faixa(sequenciaComoTitular, 0, 999, nameof(sequenciaComoTitular));
    }

    /// <summary>Pré-temporada: zera cartões, suspensões e lesões, recupera o físico e aproxima a moral do neutro.</summary>
    public void ComecarNovaTemporada()
    {
        RodadasLesionado = 0;
        RodadasSuspenso = 0;
        AmarelosAcumulados = 0;
        SequenciaComoTitular = 0;
        Condicao = EscalaMaxima;
        Moral = (Moral + 50) / 2;
    }

    private static Posicao[] ValidarSecundarias(Posicao principal, IEnumerable<Posicao> secundarias)
    {
        var lista = secundarias.ToArray();

        if (lista.Length > MaximoPosicoesSecundarias)
        {
            throw new ArgumentException(
                $"Um jogador tem no máximo {MaximoPosicoesSecundarias} posições secundárias.", nameof(secundarias));
        }

        if (lista.Contains(principal))
        {
            throw new ArgumentException("A posição principal não pode se repetir nas secundárias.", nameof(secundarias));
        }

        if (lista.Distinct().Count() != lista.Length)
        {
            throw new ArgumentException("Posições secundárias repetidas.", nameof(secundarias));
        }

        return lista;
    }
}
