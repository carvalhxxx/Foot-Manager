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
