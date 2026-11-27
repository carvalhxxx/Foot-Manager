using Jogo.Dominio.Jogadores;

namespace Jogo.Dominio.Clubes;

/// <summary>Um clube fictício, com elenco, estádio e caixa.</summary>
public sealed class Clube
{
    public const int EscalaMaxima = 100;

    private readonly List<Jogador> _elenco = [];

    public Clube(
        string id,
        string nome,
        string sigla,
        string cidade,
        string uf,
        Cores cores,
        Estadio estadio,
        int torcida,
        int reputacao,
        decimal caixa)
    {
        ArgumentNullException.ThrowIfNull(cores);
        ArgumentNullException.ThrowIfNull(estadio);

        Id = Validacao.Identificador(id, nameof(id));
        Nome = Validacao.Texto(nome, nameof(nome));
        Sigla = ValidarSigla(sigla);
        Cidade = Validacao.Texto(cidade, nameof(cidade));
        Uf = ValidarUf(uf);
        Cores = cores;
        Estadio = estadio;
        Torcida = Validacao.Faixa(torcida, 1, EscalaMaxima, nameof(torcida));
        Reputacao = Validacao.Faixa(reputacao, 1, EscalaMaxima, nameof(reputacao));
        Caixa = caixa;
    }

    /// <summary>Identificador de texto usado nas definições JSON (ex.: "porto-alegre-fc").</summary>
    public string Id { get; }
    public string Nome { get; }

    /// <summary>Sigla de 3 letras maiúsculas exibida no placar (ex.: "PAF").</summary>
    public string Sigla { get; }
    public string Cidade { get; }
    public string Uf { get; }
    public Cores Cores { get; }
    public Estadio Estadio { get; }

    /// <summary>Tamanho da torcida, 1 a 100. Influencia público e receitas.</summary>
    public int Torcida { get; }

    /// <summary>Reputação, 1 a 100. Influencia atração de jogadores e patrocínios.</summary>
    public int Reputacao { get; }

    /// <summary>Saldo em reais. Pode ficar negativo (clube endividado).</summary>
    public decimal Caixa { get; private set; }

    /// <summary>Elenco em ordem de chegada ao clube.</summary>
    public IReadOnlyList<Jogador> Elenco => _elenco;

    public void AdicionarAoElenco(Jogador jogador)
    {
        ArgumentNullException.ThrowIfNull(jogador);

        if (_elenco.Exists(j => j.Id == jogador.Id))
        {
            throw new InvalidOperationException($"O jogador {jogador.Id} já está no elenco de {Nome}.");
        }

        _elenco.Add(jogador);
    }

    public bool RemoverDoElenco(int jogadorId) => _elenco.RemoveAll(j => j.Id == jogadorId) > 0;

    public void MovimentarCaixa(decimal valor) => Caixa += valor;

    // Finanças da temporada ----------------------------------------------------

    private readonly SortedDictionary<CategoriaFinanceira, decimal> _extrato = [];

    /// <summary>Totais da temporada atual por categoria (receitas positivas, despesas negativas).</summary>
    public IReadOnlyDictionary<CategoriaFinanceira, decimal> Extrato => _extrato;

    /// <summary>Preço do ingresso escolhido pelo clube; nulo = preço sugerido pelas regras.</summary>
    public decimal? PrecoIngresso { get; private set; }

    /// <summary>Público e renda do último jogo em casa (nulo antes do primeiro).</summary>
    public (int Publico, decimal Renda)? UltimoJogoEmCasa { get; private set; }

    /// <summary>Lança um valor no caixa e no extrato da temporada.</summary>
    public void Lancar(CategoriaFinanceira categoria, decimal valor)
    {
        Caixa += valor;
        _extrato[categoria] = _extrato.GetValueOrDefault(categoria) + valor;
    }

    public void DefinirPrecoIngresso(decimal? preco)
    {
        if (preco is { } p && (p < 1 || p > 1_000))
        {
            throw new ArgumentOutOfRangeException(nameof(preco), preco, "O ingresso deve custar entre R$ 1 e R$ 1.000.");
        }

        PrecoIngresso = preco;
    }

    public void RegistrarJogoEmCasa(int publico, decimal renda) => UltimoJogoEmCasa = (publico, renda);

    /// <summary>Começo de temporada: zera o extrato (o caixa continua).</summary>
    public void ComecarNovaTemporadaFinanceira() => _extrato.Clear();

    /// <summary>Restaura o estado financeiro lido de um save.</summary>
    public void RestaurarFinancas(IReadOnlyDictionary<CategoriaFinanceira, decimal> extrato, decimal? precoIngresso, (int Publico, decimal Renda)? ultimoJogoEmCasa)
    {
        ArgumentNullException.ThrowIfNull(extrato);
        _extrato.Clear();
        foreach (var (categoria, valor) in extrato)
        {
            _extrato[categoria] = valor;
        }

        DefinirPrecoIngresso(precoIngresso);
        UltimoJogoEmCasa = ultimoJogoEmCasa;
    }

    private static string ValidarSigla(string sigla)
    {
        var texto = Validacao.Texto(sigla, nameof(sigla));
        if (texto.Length != 3 || !texto.All(c => c is >= 'A' and <= 'Z'))
        {
            throw new ArgumentException($"Sigla \"{texto}\" inválida: use 3 letras maiúsculas sem acento.", nameof(sigla));
        }

        return texto;
    }

    private static string ValidarUf(string uf)
    {
        var texto = Validacao.Texto(uf, nameof(uf));
        if (!UnidadesFederativas.Contains(texto))
        {
            throw new ArgumentException($"UF \"{texto}\" desconhecida.", nameof(uf));
        }

        return texto;
    }

    private static readonly string[] UnidadesFederativas =
    [
        "AC", "AL", "AM", "AP", "BA", "CE", "DF", "ES", "GO", "MA", "MG", "MS", "MT", "PA",
        "PB", "PE", "PI", "PR", "RJ", "RN", "RO", "RR", "RS", "SC", "SE", "SP", "TO",
    ];
}
