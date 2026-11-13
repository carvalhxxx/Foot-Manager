namespace Jogo.Dominio.Clubes;

/// <summary>
/// Estádio do clube. Tipo e estado do gramado entram na Fase 2 (D-014) como campos opcionais.
/// </summary>
public sealed record Estadio
{
    public Estadio(string nome, int capacidade)
    {
        Nome = Validacao.Texto(nome, nameof(nome));
        Capacidade = Validacao.Faixa(capacidade, 1_000, 250_000, nameof(capacidade));
    }

    public string Nome { get; }
    public int Capacidade { get; }
}

/// <summary>Cores do clube em hexadecimal (#RRGGBB).</summary>
public sealed record Cores
{
    public Cores(string primaria, string secundaria)
    {
        Primaria = ValidarHex(primaria, nameof(primaria));
        Secundaria = ValidarHex(secundaria, nameof(secundaria));
    }

    public string Primaria { get; }
    public string Secundaria { get; }

    private static string ValidarHex(string cor, string nome)
    {
        var texto = Validacao.Texto(cor, nome).ToUpperInvariant();
        if (texto.Length != 7 || texto[0] != '#' || !texto[1..].All(char.IsAsciiHexDigit))
        {
            throw new ArgumentException($"Cor \"{cor}\" inválida: use o formato #RRGGBB.", nome);
        }

        return texto;
    }
}
