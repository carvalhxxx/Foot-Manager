using Jogo.Dominio;
using Jogo.Dominio.Clubes;
using Jogo.Dominio.Competicoes;
using Jogo.Dominio.Jogadores;

namespace Jogo.Dados.Testes;

/// <summary>Pasta temporária apagada ao fim do teste.</summary>
internal sealed class PastaTemporaria : IDisposable
{
    public PastaTemporaria()
    {
        Caminho = Path.Combine(Path.GetTempPath(), "foot-manager-testes", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Caminho);
    }

    public string Caminho { get; }

    public string Arquivo(string relativo) => Path.Combine(Caminho, relativo);

    public void Dispose()
    {
        try
        {
            Directory.Delete(Caminho, recursive: true);
        }
        catch (IOException)
        {
            // Limpeza best-effort: não falha o teste por causa de arquivo preso.
        }
    }
}

/// <summary>Mundo pequeno e completo usado nos testes: 2 divisões com 2 clubes cada.</summary>
internal static class MundoDeExemplo
{
    public static Mundo Criar()
    {
        var clubes = new[]
        {
            Clube("uniao-paulista", "União Paulista", "UNP", "São Paulo", "SP", 1),
            Clube("porto-alegre-fc", "Porto Alegre FC", "PAF", "Porto Alegre", "RS", 10),
            Clube("ceara-atletico", "Ceará Atlético", "CEA", "Fortaleza", "CE", 20),
            Clube("goiania-ec", "Goiânia EC", "GEC", "Goiânia", "GO", 30),
        };

        var divisoes = new[]
        {
            new Divisao("serie-a", "Série A", 1, ["uniao-paulista", "porto-alegre-fc"], 0, 1),
            new Divisao("serie-b", "Série B", 2, ["ceara-atletico", "goiania-ec"], 1, 0),
        };

        return new Mundo(2027, divisoes, clubes);
    }

    private static Clube Clube(string id, string nome, string sigla, string cidade, string uf, int primeiroId)
    {
        var clube = new Clube(id, nome, sigla, cidade, uf, new Cores("#0A3D91", "#FFFFFF"),
            new Estadio($"Estádio {cidade}", 30_000 + primeiroId), torcida: 60, reputacao: 55, caixa: 1_234_567.89m);

        clube.AdicionarAoElenco(Jogador(primeiroId, "Zé Goleiro", Posicao.Goleiro, [], id));
        clube.AdicionarAoElenco(Jogador(primeiroId + 1, "Luizão", Posicao.Centroavante, [Posicao.SegundoAtacante], id));
        clube.AdicionarAoElenco(Jogador(primeiroId + 2, "Pedrinho", Posicao.LateralEsquerdo, [Posicao.AlaEsquerdo, Posicao.MeiaEsquerdo], id));
        return clube;
    }

    private static Jogador Jogador(int id, string nome, Posicao posicao, Posicao[] secundarias, string clubeId)
    {
        var jogador = new Jogador(id, nome, 18 + (id % 15), posicao, secundarias,
            Atributos.Uniformes(10) with { Finalizacao = 1 + (id % 20), Reflexos = 2, Posicionamento = 3 },
            new AtributosOcultos { Potencial = 75, PropensaoLesao = 7, Profissionalismo = 14 });
        jogador.AssinarContrato(new Contrato(clubeId, 8_500.50m, 2029));
        return jogador;
    }
}
