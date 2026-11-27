using Jogo.Dominio.Clubes;
using Jogo.Dominio.Jogadores;
using Jogo.Motor;

namespace Jogo.Simulacao.Ia;

/// <summary>
/// Monta a escalação de um clube controlado pela IA: para cada formação, escolhe os melhores titulares
/// posição por posição e fica com a formação que soma mais força. Determinístico (sem sorteio).
/// </summary>
public static class Escalador
{
    public const int Reservas = 7;

    /// <summary>Escala na melhor formação disponível para o elenco.</summary>
    public static TimeEscalado Escalar(
        Clube clube,
        IEnumerable<Formacao>? formacoes = null,
        IReadOnlySet<int>? indisponiveis = null,
        Mentalidade mentalidade = Mentalidade.Equilibrada)
    {
        ArgumentNullException.ThrowIfNull(clube);

        var candidatas = (formacoes ?? FormacoesPadrao.Todas).ToArray();
        var disponiveis = Disponiveis(clube, indisponiveis);

        var melhor = candidatas
            .Select(f => (Formacao: f, Titulares: EscolherTitulares(disponiveis, f)))
            .Select(x => (x.Formacao, x.Titulares, Soma: Soma(x.Titulares, x.Formacao)))
            .OrderByDescending(x => x.Soma)
            .First();

        return Montar(clube.Id, melhor.Formacao, melhor.Titulares, disponiveis, mentalidade);
    }

    /// <summary>Escala numa formação específica (usado pelo jogador humano como sugestão e pela calibração).</summary>
    public static TimeEscalado EscalarNaFormacao(
        Clube clube,
        Formacao formacao,
        IReadOnlySet<int>? indisponiveis = null,
        Mentalidade mentalidade = Mentalidade.Equilibrada)
    {
        ArgumentNullException.ThrowIfNull(clube);
        ArgumentNullException.ThrowIfNull(formacao);

        var disponiveis = Disponiveis(clube, indisponiveis);
        return Montar(clube.Id, formacao, EscolherTitulares(disponiveis, formacao), disponiveis, mentalidade);
    }

    private static List<Jogador> Disponiveis(Clube clube, IReadOnlySet<int>? indisponiveis)
    {
        var lista = clube.Elenco.Where(j => indisponiveis?.Contains(j.Id) != true).ToList();
        if (lista.Count < Formacao.JogadoresDeLinha + 1)
        {
            throw new InvalidOperationException($"{clube.Nome} não tem 11 jogadores disponíveis.");
        }

        return lista;
    }

    private static Jogador[] EscolherTitulares(List<Jogador> disponiveis, Formacao formacao) =>
        Preencher(disponiveis, formacao.PosicoesComGoleiro);

    /// <summary>
    /// Distribui exatamente estes jogadores nestas posições, colocando cada um onde rende mais
    /// (usado na troca de formação durante o jogo). A ordem do resultado acompanha <paramref name="posicoes"/>.
    /// </summary>
    public static IReadOnlyList<Jogador> Distribuir(IReadOnlyList<Jogador> jogadores, IReadOnlyList<Posicao> posicoes)
    {
        ArgumentNullException.ThrowIfNull(jogadores);
        ArgumentNullException.ThrowIfNull(posicoes);
        if (jogadores.Count != posicoes.Count)
        {
            throw new ArgumentException("A quantidade de jogadores precisa ser igual à de posições.", nameof(posicoes));
        }

        return Preencher([.. jogadores], posicoes);
    }

    private static Jogador[] Preencher(List<Jogador> disponiveis, IReadOnlyList<Posicao> posicoes)
    {
        var escolhidos = new Jogador[posicoes.Count];
        var usados = new HashSet<int>();

        // Preenche primeiro as posições com menos candidatos naturais (goleiro, laterais...),
        // para não "gastar" um especialista numa posição genérica.
        var ordem = Enumerable.Range(0, posicoes.Count)
            .OrderBy(i => disponiveis.Count(j => j.JogaNaPosicao(posicoes[i])))
            .ThenBy(i => i);

        foreach (var i in ordem)
        {
            var escolhido = disponiveis
                .Where(j => !usados.Contains(j.Id))
                .OrderByDescending(j => Valor(j, posicoes[i]))
                .ThenBy(j => j.Id)
                .First();
            escolhidos[i] = escolhido;
            usados.Add(escolhido.Id);
        }

        return escolhidos;
    }

    private static TimeEscalado Montar(string clubeId, Formacao formacao, Jogador[] titulares, List<Jogador> disponiveis, Mentalidade mentalidade)
    {
        var ids = titulares.Select(t => t.Id).ToHashSet();
        var sobra = disponiveis.Where(j => !ids.Contains(j.Id)).ToList();

        // Banco: um goleiro reserva + os melhores do restante.
        var banco = new List<Jogador>();
        var goleiroReserva = sobra.Where(j => j.PosicaoPrincipal == Posicao.Goleiro)
            .OrderByDescending(j => j.ForcaGeral).ThenBy(j => j.Id).FirstOrDefault();
        if (goleiroReserva is not null)
        {
            banco.Add(goleiroReserva);
        }

        banco.AddRange(sobra
            .Where(j => j.PosicaoPrincipal != Posicao.Goleiro)
            .OrderByDescending(j => j.ForcaGeral * (0.75 + (0.25 * j.Condicao / 100)))
            .ThenBy(j => j.Id)
            .Take(Reservas - banco.Count));

        return new TimeEscalado(clubeId, formacao, titulares, banco, mentalidade);
    }

    private static double Valor(Jogador jogador, Posicao posicao) =>
        jogador.ForcaNaPosicao(posicao) * Setores.FatorImproviso(jogador, posicao) * (0.75 + (0.25 * jogador.Condicao / 100));

    private static double Soma(Jogador[] titulares, Formacao formacao) =>
        titulares.Select((j, i) => Valor(j, formacao.PosicoesComGoleiro[i])).Sum();
}
