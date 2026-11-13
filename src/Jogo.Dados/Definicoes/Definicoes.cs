using Jogo.Dominio;

namespace Jogo.Dados.Definicoes;

/// <summary>Dados descritivos de um pacote de definições (o jogo base ou um patch da comunidade).</summary>
public sealed record Manifesto(string Nome, string? Autor = null, string? Descricao = null);

/// <summary>Resultado da leitura de uma pasta de definições.</summary>
public sealed record DefinicoesCarregadas(Manifesto Manifesto, Mundo Mundo);

/// <summary>Um problema encontrado nas definições.</summary>
/// <param name="Arquivo">Caminho relativo à pasta de definições (ex.: "clubes/porto-alegre-fc.json").</param>
/// <param name="Local">Onde dentro do arquivo (ex.: "elenco[3].idade"); vazio quando é o arquivo todo.</param>
/// <param name="Mensagem">Explicação em português.</param>
public sealed record ErroDefinicao(string Arquivo, string Local, string Mensagem)
{
    public override string ToString() =>
        string.IsNullOrEmpty(Local) ? $"{Arquivo}: {Mensagem}" : $"{Arquivo} → {Local}: {Mensagem}";
}

/// <summary>Lançada quando a pasta de definições tem erros. Traz todos os erros encontrados, não só o primeiro.</summary>
public sealed class DefinicoesInvalidasException : Exception
{
    public DefinicoesInvalidasException(IReadOnlyList<ErroDefinicao> erros)
        : base($"As definições têm {erros.Count} erro(s):\n" + string.Join("\n", erros))
    {
        Erros = erros;
    }

    public IReadOnlyList<ErroDefinicao> Erros { get; }
}
