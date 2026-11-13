namespace Jogo.Dominio;

/// <summary>Validações de invariantes compartilhadas pelas entidades do domínio.</summary>
internal static class Validacao
{
    public static int Faixa(int valor, int minimo, int maximo, string nome)
    {
        if (valor < minimo || valor > maximo)
        {
            throw new ArgumentOutOfRangeException(nome, valor, $"{nome} deve estar entre {minimo} e {maximo}.");
        }

        return valor;
    }

    public static string Texto(string? valor, string nome)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new ArgumentException($"{nome} não pode ser vazio.", nome);
        }

        return valor.Trim();
    }

    /// <summary>
    /// Identificador de texto usado nas definições JSON: letras minúsculas sem acento, dígitos e hífen.
    /// Ex.: "porto-alegre-fc", "serie-a".
    /// </summary>
    public static string Identificador(string? valor, string nome)
    {
        var texto = Texto(valor, nome);
        var valido = texto.All(c => c is (>= 'a' and <= 'z') or (>= '0' and <= '9') or '-')
                     && !texto.StartsWith('-') && !texto.EndsWith('-');

        if (!valido)
        {
            throw new ArgumentException(
                $"{nome} \"{texto}\" é inválido: use só letras minúsculas sem acento, dígitos e hífen.", nome);
        }

        return texto;
    }

    public static decimal NaoNegativo(decimal valor, string nome)
    {
        if (valor < 0)
        {
            throw new ArgumentOutOfRangeException(nome, valor, $"{nome} não pode ser negativo.");
        }

        return valor;
    }
}
