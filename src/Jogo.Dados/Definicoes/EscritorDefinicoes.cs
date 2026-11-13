using System.Text;
using System.Text.Json;
using Jogo.Dominio;
using Jogo.Dominio.Clubes;
using Jogo.Dominio.Jogadores;

namespace Jogo.Dados.Definicoes;

/// <summary>
/// Grava um <see cref="Mundo"/> como pasta de definições (manifesto, divisões e um arquivo por clube).
/// A saída é estável: o mesmo mundo gera sempre os mesmos bytes, o que facilita revisar patches no Git.
/// </summary>
public static class EscritorDefinicoes
{
    private static readonly UTF8Encoding Utf8SemBom = new(encoderShouldEmitUTF8Identifier: false);

    /// <summary>
    /// Grava as definições em <paramref name="pasta"/>. Arquivos .json antigos da pasta de clubes são
    /// apagados antes, para não sobrar clube que não existe mais no mundo.
    /// </summary>
    public static void Salvar(Mundo mundo, Manifesto manifesto, string pasta)
    {
        ArgumentNullException.ThrowIfNull(mundo);
        ArgumentNullException.ThrowIfNull(manifesto);

        var pastaClubes = Path.Combine(pasta, FormatoDefinicoes.PastaClubes);
        Directory.CreateDirectory(pastaClubes);
        foreach (var antigo in Directory.GetFiles(pastaClubes, "*.json"))
        {
            File.Delete(antigo);
        }

        var contexto = FormatoDefinicoes.Contexto;

        Gravar(Path.Combine(pasta, FormatoDefinicoes.ArquivoManifesto), JsonSerializer.Serialize(new ManifestoJson
        {
            VersaoFormato = FormatoDefinicoes.VersaoAtual,
            Nome = manifesto.Nome,
            Autor = manifesto.Autor,
            Descricao = manifesto.Descricao,
            TemporadaInicial = mundo.TemporadaAtual,
        }, contexto.ManifestoJson));

        Gravar(Path.Combine(pasta, FormatoDefinicoes.ArquivoDivisoes), JsonSerializer.Serialize(new DivisoesJson
        {
            VersaoFormato = FormatoDefinicoes.VersaoAtual,
            Divisoes = [.. mundo.Divisoes.Select(d => new DivisaoJson
            {
                Id = d.Id,
                Nome = d.Nome,
                Nivel = d.Nivel,
                VagasAcesso = d.VagasAcesso,
                VagasRebaixamento = d.VagasRebaixamento,
                Clubes = [.. d.ClubeIds],
            })],
        }, contexto.DivisoesJson));

        foreach (var clube in mundo.Clubes)
        {
            Gravar(Path.Combine(pastaClubes, clube.Id + ".json"), JsonSerializer.Serialize(ParaJson(clube), contexto.ClubeJson));
        }
    }

    private static ClubeJson ParaJson(Clube clube) => new()
    {
        VersaoFormato = FormatoDefinicoes.VersaoAtual,
        Id = clube.Id,
        Nome = clube.Nome,
        Sigla = clube.Sigla,
        Cidade = clube.Cidade,
        Uf = clube.Uf,
        Cores = new CoresJson { Primaria = clube.Cores.Primaria, Secundaria = clube.Cores.Secundaria },
        Estadio = new EstadioJson { Nome = clube.Estadio.Nome, Capacidade = clube.Estadio.Capacidade },
        Torcida = clube.Torcida,
        Reputacao = clube.Reputacao,
        Caixa = clube.Caixa,
        Elenco = [.. clube.Elenco.Select(ParaJson)],
    };

    private static JogadorJson ParaJson(Jogador j) => new()
    {
        Id = j.Id,
        Nome = j.Nome,
        Idade = j.Idade,
        Posicao = j.PosicaoPrincipal.Sigla(),
        PosicoesSecundarias = j.PosicoesSecundarias.Count > 0 ? [.. j.PosicoesSecundarias.Select(p => p.Sigla())] : null,
        Atributos = new AtributosJson
        {
            Finalizacao = j.Atributos.Finalizacao,
            Passe = j.Atributos.Passe,
            Desarme = j.Atributos.Desarme,
            Drible = j.Atributos.Drible,
            Velocidade = j.Atributos.Velocidade,
            Resistencia = j.Atributos.Resistencia,
            Decisao = j.Atributos.Decisao,
            Determinacao = j.Atributos.Determinacao,
            Reflexos = j.Atributos.Reflexos,
            Posicionamento = j.Atributos.Posicionamento,
        },
        Ocultos = new OcultosJson
        {
            Potencial = j.Ocultos.Potencial,
            PropensaoLesao = j.Ocultos.PropensaoLesao,
            Profissionalismo = j.Ocultos.Profissionalismo,
        },
        Salario = j.Contrato?.SalarioMensal,
        ContratoAte = j.Contrato?.TemporadaFinal,
    };

    private static void Gravar(string caminho, string json) => File.WriteAllText(caminho, json + "\n", Utf8SemBom);
}
