namespace Jogo.Dominio.Jogadores;

/// <summary>Os atributos visíveis de um jogador (escala 1-20).</summary>
public enum TipoAtributo
{
    // Técnicos
    Finalizacao,
    Passe,
    Desarme,
    Drible,

    // Físicos
    Velocidade,
    Resistencia,

    // Mentais
    Decisao,
    Determinacao,

    // Goleiro (substituem Finalização e Drible na força do goleiro)
    Reflexos,
    Posicionamento,
}

/// <summary>
/// Atributos de um jogador, na escala 1-20.
/// Todo jogador tem os 10 atributos: o goleiro tem Finalização e Drible baixos, e os jogadores de linha
/// têm Reflexos e Posicionamento baixos. Assim, improvisar alguém fora da posição já cai naturalmente.
/// A tela mostra só os 8 relevantes para a posição.
/// </summary>
public sealed record Atributos
{
    public const int Minimo = 1;
    public const int Maximo = 20;

    public required int Finalizacao { get; init => field = Validar(value, nameof(Finalizacao)); }
    public required int Passe { get; init => field = Validar(value, nameof(Passe)); }
    public required int Desarme { get; init => field = Validar(value, nameof(Desarme)); }
    public required int Drible { get; init => field = Validar(value, nameof(Drible)); }
    public required int Velocidade { get; init => field = Validar(value, nameof(Velocidade)); }
    public required int Resistencia { get; init => field = Validar(value, nameof(Resistencia)); }
    public required int Decisao { get; init => field = Validar(value, nameof(Decisao)); }
    public required int Determinacao { get; init => field = Validar(value, nameof(Determinacao)); }
    public required int Reflexos { get; init => field = Validar(value, nameof(Reflexos)); }
    public required int Posicionamento { get; init => field = Validar(value, nameof(Posicionamento)); }

    public int this[TipoAtributo tipo] => tipo switch
    {
        TipoAtributo.Finalizacao => Finalizacao,
        TipoAtributo.Passe => Passe,
        TipoAtributo.Desarme => Desarme,
        TipoAtributo.Drible => Drible,
        TipoAtributo.Velocidade => Velocidade,
        TipoAtributo.Resistencia => Resistencia,
        TipoAtributo.Decisao => Decisao,
        TipoAtributo.Determinacao => Determinacao,
        TipoAtributo.Reflexos => Reflexos,
        TipoAtributo.Posicionamento => Posicionamento,
        _ => throw new ArgumentOutOfRangeException(nameof(tipo), tipo, null),
    };

    /// <summary>Atributos com o mesmo valor em todos os campos (útil em testes e valores padrão).</summary>
    public static Atributos Uniformes(int valor) => new()
    {
        Finalizacao = valor,
        Passe = valor,
        Desarme = valor,
        Drible = valor,
        Velocidade = valor,
        Resistencia = valor,
        Decisao = valor,
        Determinacao = valor,
        Reflexos = valor,
        Posicionamento = valor,
    };

    private static int Validar(int valor, string nome) => Validacao.Faixa(valor, Minimo, Maximo, nome);
}

/// <summary>Atributos que o jogador não vê, mas que guiam evolução e lesões.</summary>
public sealed record AtributosOcultos
{
    /// <summary>Teto de força geral que o jogador pode atingir (escala 1-99).</summary>
    public required int Potencial { get; init => field = Validacao.Faixa(value, 1, 99, nameof(Potencial)); }

    /// <summary>Quanto maior, mais lesões (escala 1-20).</summary>
    public required int PropensaoLesao { get; init => field = Validacao.Faixa(value, 1, 20, nameof(PropensaoLesao)); }

    /// <summary>Quanto maior, melhor a evolução e mais lento o declínio (escala 1-20).</summary>
    public required int Profissionalismo { get; init => field = Validacao.Faixa(value, 1, 20, nameof(Profissionalismo)); }
}
