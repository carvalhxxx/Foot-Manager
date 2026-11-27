namespace Jogo.Simulacao.Competicoes;

/// <summary>Para onde a vaga leva.</summary>
public enum DestinoContinental
{
    LibertadoresGrupos,
    LibertadoresPreliminar,
    SulAmericana,
}

/// <summary>Por que o clube ganhou a vaga.</summary>
public enum OrigemDaVaga
{
    CampeaoLibertadores,
    CampeaoSulAmericana,
    CopaDoBrasil,
    Brasileirao,
}

public sealed record VagaContinental(string ClubeId, DestinoContinental Destino, OrigemDaVaga Origem);

/// <summary>
/// O que decide as vagas do ano seguinte. Campeões nulos = competição não disputada (ou sem campeão brasileiro).
/// </summary>
/// <param name="Tabela">Clubes da Série A na ordem final do Brasileirão.</param>
/// <param name="Inelegiveis">Clubes que não podem receber vaga (ex.: rebaixados, que não jogam a Série A no ano seguinte).</param>
public sealed record EntradaDasVagas(
    IReadOnlyList<string> Tabela,
    IReadOnlySet<string> Inelegiveis,
    string? CampeaoLibertadores = null,
    string? CampeaoSulAmericana = null,
    string? CampeaoCopaDoBrasil = null,
    string? ViceCopaDoBrasil = null);

/// <summary>
/// Distribuição das vagas continentais do Brasil (regulamento de 2026, D-037).
/// <list type="number">
/// <item>Campeões da Libertadores e da Sul-Americana têm vaga própria na fase de grupos.</item>
/// <item>Brasileirão: 4 vagas de grupos e 1 de preliminar, pela ordem da tabela, pulando quem já tem vaga
/// (por isso um campeão continental no G-5 abre vaga para o 6º).</item>
/// <item>Copa do Brasil: grupos para o campeão e preliminar para o vice. Se o campeão já está classificado à
/// fase de grupos, o vice herda a vaga de grupos; vaga que nenhum finalista use volta ao Brasileirão,
/// para o mais bem colocado ainda sem vaga.</item>
/// <item>Sul-Americana: 6 vagas para os seguintes da tabela que não foram à Libertadores.</item>
/// <item>Quem tem duas classificações fica com a melhor. Inelegíveis nunca recebem vaga.</item>
/// </list>
/// </summary>
public static class VagasContinentais
{
    public const int GruposPeloBrasileirao = 4;
    public const int PreliminarPeloBrasileirao = 1;
    public const int SulAmericanaPeloBrasileirao = 6;

    public static IReadOnlyList<VagaContinental> Distribuir(EntradaDasVagas entrada)
    {
        ArgumentNullException.ThrowIfNull(entrada);
        var vagas = new List<VagaContinental>();
        bool Classificado(string clube) => vagas.Exists(v => v.ClubeId == clube);
        bool Elegivel(string? clube) => clube is not null && !entrada.Inelegiveis.Contains(clube);

        // Vagas que vão para a tabela, na ordem em que são preenchidas.
        var daTabela = new List<DestinoContinental>();

        // 1. Títulos continentais: vaga própria; se o mesmo clube ganhar as duas, a sobra vai para a tabela.
        foreach (var (campeao, origem) in new[]
                 {
                     (entrada.CampeaoLibertadores, OrigemDaVaga.CampeaoLibertadores),
                     (entrada.CampeaoSulAmericana, OrigemDaVaga.CampeaoSulAmericana),
                 })
        {
            if (campeao is null)
            {
                continue;
            }

            if (Elegivel(campeao) && !Classificado(campeao))
            {
                vagas.Add(new(campeao, DestinoContinental.LibertadoresGrupos, origem));
            }
            else
            {
                daTabela.Add(DestinoContinental.LibertadoresGrupos);
            }
        }

        // 2. Brasileirão (posições do G-5), pulando quem já se classificou.
        var gruposDaTabela = Enumerable.Repeat(DestinoContinental.LibertadoresGrupos, GruposPeloBrasileirao)
            .Concat(Enumerable.Repeat(DestinoContinental.LibertadoresPreliminar, PreliminarPeloBrasileirao));
        daTabela.InsertRange(0, gruposDaTabela);

        // Quem está no G-4 conta como "já classificado à fase de grupos" para a Copa do Brasil.
        var g4 = entrada.Tabela.Where(Elegivel).Where(c => !Classificado(c)).Take(GruposPeloBrasileirao).ToHashSet();

        // 3. Copa do Brasil: grupos e preliminar passam pelos finalistas, na ordem campeão → vice.
        var finalistas = new[] { entrada.CampeaoCopaDoBrasil, entrada.ViceCopaDoBrasil }.Where(f => f is not null).Select(f => f!).ToList();
        if (entrada.CampeaoCopaDoBrasil is not null)
        {
            var paraOsFinalistas = new[] { DestinoContinental.LibertadoresGrupos, DestinoContinental.LibertadoresPreliminar };
            var candidatos = finalistas.Where(Elegivel).Where(f => !Classificado(f) && !g4.Contains(f)).ToList();
            foreach (var destino in paraOsFinalistas)
            {
                if (candidatos.Count > 0)
                {
                    vagas.Add(new(candidatos[0], destino, OrigemDaVaga.CopaDoBrasil));
                    candidatos.RemoveAt(0);
                }
                else
                {
                    daTabela.Add(destino);
                }
            }
        }

        // 4. Vagas da tabela (as do Brasileirão e as que sobraram), para os mais bem colocados ainda sem vaga.
        var fila = new Queue<string>(entrada.Tabela.Where(Elegivel));
        foreach (var destino in daTabela.OrderBy(d => d))
        {
            while (fila.Count > 0 && Classificado(fila.Peek()))
            {
                fila.Dequeue();
            }

            if (fila.Count == 0)
            {
                break;
            }

            vagas.Add(new(fila.Dequeue(), destino, OrigemDaVaga.Brasileirao));
        }

        // 5. Sul-Americana para os seguintes que não foram à Libertadores.
        foreach (var clube in entrada.Tabela.Where(Elegivel).Where(c => !Classificado(c)).Take(SulAmericanaPeloBrasileirao).ToArray())
        {
            vagas.Add(new(clube, DestinoContinental.SulAmericana, OrigemDaVaga.Brasileirao));
        }

        return vagas;
    }
}
