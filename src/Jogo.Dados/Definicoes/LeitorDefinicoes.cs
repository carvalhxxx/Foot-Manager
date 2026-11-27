using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Text.RegularExpressions;
using Jogo.Dominio;
using Jogo.Dominio.Clubes;
using Jogo.Dominio.Competicoes;
using Jogo.Dominio.Jogadores;

namespace Jogo.Dados.Definicoes;

/// <summary>
/// Lê definições (jogo base ou patch) de uma <see cref="IFonteDefinicoes"/> e monta o <see cref="Mundo"/>.
/// Em vez de parar no primeiro problema, junta todos e lança <see cref="DefinicoesInvalidasException"/>.
/// </summary>
public static partial class LeitorDefinicoes
{
    /// <summary>Lê de uma pasta no disco.</summary>
    public static DefinicoesCarregadas Carregar(string pasta) => Carregar(new FontePasta(pasta));

    /// <summary>Lê de qualquer fonte (disco, memória no navegador, patch enviado pelo jogador).</summary>
    public static DefinicoesCarregadas Carregar(IFonteDefinicoes fonte)
    {
        ArgumentNullException.ThrowIfNull(fonte);

        var erros = new List<ErroDefinicao>();

        var manifestoJson = Ler(fonte, FormatoDefinicoes.ArquivoManifesto, FormatoDefinicoes.Contexto.ManifestoJson, j => j.VersaoFormato, erros);
        var manifesto = ConverterManifesto(manifestoJson, new Contexto(FormatoDefinicoes.ArquivoManifesto, erros), out var temporada);

        var divisoesJson = Ler(fonte, FormatoDefinicoes.ArquivoDivisoes, FormatoDefinicoes.Contexto.DivisoesJson, j => j.VersaoFormato, erros);
        var divisoes = ConverterDivisoes(divisoesJson, new Contexto(FormatoDefinicoes.ArquivoDivisoes, erros));

        var clubes = new List<Clube>();
        if (!fonte.TemPastaDeClubes)
        {
            erros.Add(new(FormatoDefinicoes.PastaClubes + "/", "", "Pasta de clubes não encontrada."));
        }
        else
        {
            // Ordem alfabética para o resultado não depender do sistema de arquivos.
            foreach (var relativo in fonte.ArquivosDeClubes().Order(StringComparer.Ordinal))
            {
                var json = Ler(fonte, relativo, FormatoDefinicoes.Contexto.ClubeJson, j => j.VersaoFormato, erros);
                var nomeDoArquivo = relativo[(relativo.LastIndexOf('/') + 1)..^".json".Length];
                var clube = ConverterClube(json, nomeDoArquivo, new Contexto(relativo, erros));
                if (clube is not null)
                {
                    clubes.Add(clube);
                }
            }
        }

        if (erros.Count == 0 && manifesto is not null && temporada is not null)
        {
            // Ordena os clubes pela ordem em que aparecem nas divisões (o arquivo de divisões manda).
            var ordem = divisoes.SelectMany(d => d.ClubeIds).Select((id, i) => (id, i))
                .ToDictionary(x => x.id, x => x.i, StringComparer.Ordinal);
            clubes = [.. clubes.OrderBy(c => ordem.GetValueOrDefault(c.Id, int.MaxValue)).ThenBy(c => c.Id, StringComparer.Ordinal)];

            erros.AddRange(Mundo.Inconsistencias(temporada.Value, divisoes, clubes)
                .Select(m => new ErroDefinicao("(conjunto)", "", m)));
        }

        if (erros.Count > 0)
        {
            throw new DefinicoesInvalidasException(erros);
        }

        return new DefinicoesCarregadas(manifesto!, new Mundo(temporada!.Value, divisoes, clubes));
    }

    private static T? Ler<T>(IFonteDefinicoes fonte, string relativo, JsonTypeInfo<T> tipo, Func<T, int?> versao, List<ErroDefinicao> erros)
        where T : class
    {
        var texto = fonte.LerTexto(relativo);
        if (texto is null)
        {
            erros.Add(new(relativo, "", "Arquivo não encontrado."));
            return null;
        }

        T? conteudo;
        try
        {
            conteudo = JsonSerializer.Deserialize(texto, tipo);
        }
        catch (JsonException ex)
        {
            var linha = ex.LineNumber is { } l ? $" (linha {l + 1})" : "";
            erros.Add(new(relativo, ex.Path ?? "", $"JSON inválido{linha}: verifique vírgulas, aspas e tipos dos valores."));
            return null;
        }

        if (conteudo is null)
        {
            erros.Add(new(relativo, "", "Arquivo vazio."));
            return null;
        }

        switch (versao(conteudo))
        {
            case null:
                erros.Add(new(relativo, "versaoFormato", "Campo obrigatório ausente."));
                return null;
            case > FormatoDefinicoes.VersaoAtual:
                erros.Add(new(relativo, "versaoFormato",
                    $"Feito para a versão {versao(conteudo)} do formato; este jogo lê até a versão {FormatoDefinicoes.VersaoAtual}. Atualize o jogo."));
                return null;
            default:
                return conteudo;
        }
    }

    private static Manifesto? ConverterManifesto(ManifestoJson? json, Contexto ctx, out int? temporada)
    {
        temporada = null;
        if (json is null)
        {
            return null;
        }

        temporada = ctx.Obrigatorio(json.TemporadaInicial, "temporadaInicial");
        var nome = ctx.Texto(json.Nome, "nome");
        return nome is null ? null : new Manifesto(nome, json.Autor, json.Descricao);
    }

    private static List<Divisao> ConverterDivisoes(DivisoesJson? json, Contexto ctx)
    {
        var divisoes = new List<Divisao>();
        if (json is null)
        {
            return divisoes;
        }

        if (json.Divisoes is null)
        {
            ctx.Erro("divisoes", "Campo obrigatório ausente.");
            return divisoes;
        }

        for (var i = 0; i < json.Divisoes.Count; i++)
        {
            var d = json.Divisoes[i];
            var local = $"divisoes[{i}]";
            var nivel = ctx.Obrigatorio(d.Nivel, $"{local}.nivel");
            var acesso = ctx.Obrigatorio(d.VagasAcesso, $"{local}.vagasAcesso");
            var rebaixamento = ctx.Obrigatorio(d.VagasRebaixamento, $"{local}.vagasRebaixamento");
            var clubes = ctx.Exigir(d.Clubes, $"{local}.clubes");

            if (nivel is null || acesso is null || rebaixamento is null || clubes is null)
            {
                continue;
            }

            var divisao = ctx.Criar(local, () => new Divisao(d.Id!, d.Nome!, nivel.Value, clubes, acesso.Value, rebaixamento.Value, d.VagasDiretas));
            if (divisao is not null)
            {
                divisoes.Add(divisao);
            }
        }

        return divisoes;
    }

    private static Clube? ConverterClube(ClubeJson? json, string nomeDoArquivo, Contexto ctx)
    {
        if (json is null)
        {
            return null;
        }

        if (json.Id is not null && json.Id != nomeDoArquivo)
        {
            ctx.Erro("id", $"O id \"{json.Id}\" precisa ser igual ao nome do arquivo (\"{nomeDoArquivo}.json\").");
        }

        var cores = ctx.Exigir(json.Cores, "cores");
        var estadioJson = ctx.Exigir(json.Estadio, "estadio");
        var torcida = ctx.Obrigatorio(json.Torcida, "torcida");
        var reputacao = ctx.Obrigatorio(json.Reputacao, "reputacao");
        var caixa = ctx.Obrigatorio(json.Caixa, "caixa");
        var capacidade = estadioJson is null ? null : ctx.Obrigatorio(estadioJson.Capacidade, "estadio.capacidade");

        // Os jogadores são validados mesmo se o cabeçalho do clube tiver erro,
        // para o autor do patch ver todos os problemas do arquivo de uma vez.
        // O contrato usa o nome do arquivo, que obrigatoriamente é igual ao id do clube.
        var elenco = json.Elenco ?? [];
        var jogadores = new List<(int Indice, Jogador Jogador)>();
        for (var i = 0; i < elenco.Count; i++)
        {
            if (ConverterJogador(elenco[i], nomeDoArquivo, $"elenco[{i}]", ctx) is { } jogador)
            {
                jogadores.Add((i, jogador));
            }
        }

        if (cores is null || estadioJson is null || torcida is null || reputacao is null || caixa is null || capacidade is null)
        {
            return null;
        }

        var clube = ctx.Criar("", () => new Clube(
            json.Id!, json.Nome!, json.Sigla!, json.Cidade!, json.Uf!,
            new Cores(cores.Primaria!, cores.Secundaria!),
            new Estadio(estadioJson.Nome!, capacidade.Value),
            torcida.Value, reputacao.Value, caixa.Value));

        if (clube is null)
        {
            return null;
        }

        foreach (var (indice, jogador) in jogadores)
        {
            ctx.Criar($"elenco[{indice}]", () => { clube.AdicionarAoElenco(jogador); return jogador; });
        }

        return clube;
    }

    private static Jogador? ConverterJogador(JogadorJson j, string clubeId, string local, Contexto ctx)
    {
        var id = ctx.Obrigatorio(j.Id, $"{local}.id");
        var idade = ctx.Obrigatorio(j.Idade, $"{local}.idade");
        var salario = ctx.Obrigatorio(j.Salario, $"{local}.salario");
        var contratoAte = ctx.Obrigatorio(j.ContratoAte, $"{local}.contratoAte");
        var atributos = ConverterAtributos(ctx.Exigir(j.Atributos, $"{local}.atributos"), $"{local}.atributos", ctx);
        var ocultos = ConverterOcultos(ctx.Exigir(j.Ocultos, $"{local}.ocultos"), $"{local}.ocultos", ctx);
        var posicao = ConverterPosicao(j.Posicao, $"{local}.posicao", ctx);

        var secundarias = new List<Posicao>();
        var listaSecundarias = j.PosicoesSecundarias ?? [];
        for (var i = 0; i < listaSecundarias.Count; i++)
        {
            if (ConverterPosicao(listaSecundarias[i], $"{local}.posicoesSecundarias[{i}]", ctx) is { } p)
            {
                secundarias.Add(p);
            }
        }

        if (id is null || idade is null || salario is null || contratoAte is null
            || atributos is null || ocultos is null || posicao is null
            || secundarias.Count != listaSecundarias.Count)
        {
            return null;
        }

        return ctx.Criar(local, () =>
        {
            var jogador = new Jogador(id.Value, j.Nome!, idade.Value, posicao.Value, secundarias, atributos, ocultos);
            jogador.AssinarContrato(new Contrato(clubeId, salario.Value, contratoAte.Value));
            return jogador;
        });
    }

    private static Posicao? ConverterPosicao(string? sigla, string local, Contexto ctx)
    {
        if (sigla is null)
        {
            ctx.Erro(local, "Campo obrigatório ausente.");
            return null;
        }

        if (!PosicaoExtensoes.TentarDaSigla(sigla, out var posicao))
        {
            var validas = string.Join(", ", PosicaoExtensoes.Todas.Select(p => p.Sigla()));
            ctx.Erro(local, $"Posição \"{sigla}\" desconhecida. Use uma de: {validas}.");
            return null;
        }

        return posicao;
    }

    private static Atributos? ConverterAtributos(AtributosJson? a, string local, Contexto ctx)
    {
        if (a is null)
        {
            return null;
        }

        int?[] valores =
        [
            ctx.Obrigatorio(a.Finalizacao, $"{local}.finalizacao"),
            ctx.Obrigatorio(a.Passe, $"{local}.passe"),
            ctx.Obrigatorio(a.Desarme, $"{local}.desarme"),
            ctx.Obrigatorio(a.Drible, $"{local}.drible"),
            ctx.Obrigatorio(a.Velocidade, $"{local}.velocidade"),
            ctx.Obrigatorio(a.Resistencia, $"{local}.resistencia"),
            ctx.Obrigatorio(a.Decisao, $"{local}.decisao"),
            ctx.Obrigatorio(a.Determinacao, $"{local}.determinacao"),
            ctx.Obrigatorio(a.Reflexos, $"{local}.reflexos"),
            ctx.Obrigatorio(a.Posicionamento, $"{local}.posicionamento"),
        ];

        if (valores.Any(v => v is null))
        {
            return null;
        }

        return ctx.Criar(local, () => new Atributos
        {
            Finalizacao = a.Finalizacao!.Value,
            Passe = a.Passe!.Value,
            Desarme = a.Desarme!.Value,
            Drible = a.Drible!.Value,
            Velocidade = a.Velocidade!.Value,
            Resistencia = a.Resistencia!.Value,
            Decisao = a.Decisao!.Value,
            Determinacao = a.Determinacao!.Value,
            Reflexos = a.Reflexos!.Value,
            Posicionamento = a.Posicionamento!.Value,
        });
    }

    private static AtributosOcultos? ConverterOcultos(OcultosJson? o, string local, Contexto ctx)
    {
        if (o is null)
        {
            return null;
        }

        var potencial = ctx.Obrigatorio(o.Potencial, $"{local}.potencial");
        var lesao = ctx.Obrigatorio(o.PropensaoLesao, $"{local}.propensaoLesao");
        var profissionalismo = ctx.Obrigatorio(o.Profissionalismo, $"{local}.profissionalismo");

        if (potencial is null || lesao is null || profissionalismo is null)
        {
            return null;
        }

        return ctx.Criar(local, () => new AtributosOcultos
        {
            Potencial = potencial.Value,
            PropensaoLesao = lesao.Value,
            Profissionalismo = profissionalismo.Value,
        });
    }

    /// <summary>Acumula erros de um arquivo, com o caminho do campo.</summary>
    private sealed class Contexto(string arquivo, List<ErroDefinicao> erros)
    {
        public void Erro(string local, string mensagem) => erros.Add(new(arquivo, local, mensagem));

        public T? Obrigatorio<T>(T? valor, string local) where T : struct
        {
            if (valor is null)
            {
                Erro(local, "Campo obrigatório ausente.");
            }

            return valor;
        }

        public T? Exigir<T>(T? valor, string local) where T : class
        {
            if (valor is null)
            {
                Erro(local, "Campo obrigatório ausente.");
            }

            return valor;
        }

        public string? Texto(string? valor, string local)
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                Erro(local, "Campo obrigatório ausente.");
                return null;
            }

            return valor;
        }

        /// <summary>Executa a criação de uma entidade; se o domínio recusar, registra a mensagem dele.</summary>
        public T? Criar<T>(string local, Func<T> fabrica) where T : class
        {
            try
            {
                return fabrica();
            }
            catch (ArgumentException ex)
            {
                Erro(local, MensagemLimpa(ex));
                return null;
            }
            catch (InvalidOperationException ex)
            {
                Erro(local, ex.Message);
                return null;
            }
        }
    }

    /// <summary>Remove o sufixo técnico " (Parameter 'x')" e o "Actual value was..." das mensagens do .NET.</summary>
    private static string MensagemLimpa(ArgumentException ex)
    {
        var primeiraLinha = ex.Message.Split('\n')[0].TrimEnd('\r');
        return SufixoParametro().Replace(primeiraLinha, "").Trim();
    }

    [GeneratedRegex(@"\s*\(\w+ '[^']*'\)$")]
    private static partial Regex SufixoParametro();
}
