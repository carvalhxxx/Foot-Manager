using Jogo.Dominio;
using Jogo.Dominio.Aleatoriedade;
using Jogo.Dominio.Clubes;
using Jogo.Dominio.Jogadores;
using Jogo.Simulacao.Geracao;

namespace Jogo.Simulacao.Mercado;

/// <summary>Oferta de um clube por um jogador.</summary>
public sealed record Oferta(Clube Clube, decimal Valor);

/// <summary>Uma transferência concluída (origem nula = agente livre).</summary>
public sealed record Transferencia(int JogadorId, string Jogador, string? DeClubeId, string ParaClubeId, decimal Valor);

/// <summary>
/// Compra e venda de jogadores. Duas janelas: pré-temporada (até a rodada 3) e meio do ano (rodadas 18 a 21).
/// Agentes livres podem ser contratados a qualquer momento.
/// </summary>
public static class Transferencias
{
    public const int ElencoMaximo = 32;
    public const int ElencoMinimoParaVender = 18;

    /// <summary>Quanto o clube dono pede acima do valor de mercado.</summary>
    public const decimal Agio = 1.2m;

    /// <summary>A janela está aberta antes de jogar a rodada seguinte a <paramref name="rodadasDisputadas"/>?</summary>
    public static bool JanelaAberta(int rodadasDisputadas) =>
        rodadasDisputadas < 3 || rodadasDisputadas is >= 17 and < 21;

    /// <summary>Preço fixo pedido pelo clube (zero para agente livre).</summary>
    public static decimal Preco(Jogador jogador, int temporadaAtual) =>
        jogador.Contrato is null ? 0 : Math.Round(Valores.ValorDeMercado(jogador, temporadaAtual) * Agio / 50_000m) * 50_000m;

    /// <summary>Anos do contrato assinado na contratação.</summary>
    public static int AnosDeContrato(Jogador jogador) => jogador.Idade switch { <= 25 => 4, <= 29 => 3, <= 32 => 2, _ => 1 };

    /// <summary>Motivo pelo qual a compra não pode acontecer (nulo = pode).</summary>
    public static string? Impedimento(Mundo mundo, Clube comprador, Jogador jogador, int rodadasDisputadas)
    {
        var vendedor = DonoDe(mundo, jogador);
        if (vendedor == comprador)
        {
            return $"{jogador.Nome} já é do seu elenco.";
        }

        if (vendedor is not null && !JanelaAberta(rodadasDisputadas))
        {
            return "A janela de transferências está fechada. Agentes livres podem ser contratados a qualquer momento.";
        }

        if (comprador.Elenco.Count >= ElencoMaximo)
        {
            return $"O elenco já tem {ElencoMaximo} jogadores.";
        }

        if (vendedor is not null && vendedor.Elenco.Count <= ElencoMinimoParaVender)
        {
            return $"O {vendedor.Nome} não pode ficar com menos de {ElencoMinimoParaVender} jogadores.";
        }

        var preco = Preco(jogador, mundo.TemporadaAtual);
        return preco > comprador.Caixa ? "Não há dinheiro em caixa para essa compra." : null;
    }

    /// <summary>Compra pelo preço fixo e assina pelo salário pedido.</summary>
    public static void Comprar(Mundo mundo, Clube comprador, Jogador jogador, int rodadasDisputadas)
    {
        ArgumentNullException.ThrowIfNull(mundo);
        ArgumentNullException.ThrowIfNull(comprador);
        ArgumentNullException.ThrowIfNull(jogador);
        if (Impedimento(mundo, comprador, jogador, rodadasDisputadas) is { } motivo)
        {
            throw new InvalidOperationException(motivo);
        }

        var preco = Preco(jogador, mundo.TemporadaAtual);
        var vendedor = DonoDe(mundo, jogador);
        if (vendedor is null)
        {
            mundo.RemoverAgenteLivre(jogador.Id);
        }
        else
        {
            vendedor.RemoverDoElenco(jogador.Id);
            vendedor.Lancar(CategoriaFinanceira.VendaDeJogadores, preco);
            comprador.Lancar(CategoriaFinanceira.CompraDeJogadores, -preco);
        }

        Transferir(comprador, jogador, mundo.TemporadaAtual);
    }

    /// <summary>
    /// Melhor oferta imediata da IA: clubes que teriam o jogador entre os 18 melhores, com espaço no elenco
    /// e dinheiro. Pagam o valor de mercado (até 10% a mais se ele chegar como um dos 11 melhores).
    /// </summary>
    public static Oferta? MelhorOferta(Mundo mundo, Clube vendedor, Jogador jogador)
    {
        ArgumentNullException.ThrowIfNull(mundo);
        var valor = Valores.ValorDeMercado(jogador, mundo.TemporadaAtual);
        return mundo.Clubes
            .Where(c => c != vendedor && c.Elenco.Count < ElencoMaximo)
            .Select(c => (Clube: c, Posto: c.Elenco.Count(j => j.ForcaGeral > jogador.ForcaGeral)))
            .Where(x => x.Posto < 18)
            .Select(x => new Oferta(x.Clube, Math.Round(valor * (x.Posto < 11 ? 1.1m : 1.0m) / 50_000m) * 50_000m))
            .Where(o => o.Valor <= o.Clube.Caixa)
            .OrderByDescending(o => o.Valor).ThenByDescending(o => o.Clube.Caixa).ThenBy(o => o.Clube.Id, StringComparer.Ordinal)
            .FirstOrDefault();
    }

    /// <summary>Vende para a melhor oferta (janela aberta e elenco acima do mínimo).</summary>
    public static Oferta Vender(Mundo mundo, Clube vendedor, Jogador jogador, int rodadasDisputadas)
    {
        ArgumentNullException.ThrowIfNull(mundo);
        ArgumentNullException.ThrowIfNull(vendedor);
        ArgumentNullException.ThrowIfNull(jogador);
        if (!JanelaAberta(rodadasDisputadas))
        {
            throw new InvalidOperationException("A janela de transferências está fechada.");
        }

        if (vendedor.Elenco.Count <= ElencoMinimoParaVender)
        {
            throw new InvalidOperationException($"O elenco precisa ter pelo menos {ElencoMinimoParaVender} jogadores.");
        }

        var oferta = MelhorOferta(mundo, vendedor, jogador)
            ?? throw new InvalidOperationException($"Nenhum clube fez proposta por {jogador.Nome}.");
        vendedor.RemoverDoElenco(jogador.Id);
        vendedor.Lancar(CategoriaFinanceira.VendaDeJogadores, oferta.Valor);
        oferta.Clube.Lancar(CategoriaFinanceira.CompraDeJogadores, -oferta.Valor);
        Transferir(oferta.Clube, jogador, mundo.TemporadaAtual);
        return oferta;
    }

    /// <summary>
    /// IA na janela: clubes no vermelho vendem um reserva valioso; os outros, de vez em quando, reforçam a
    /// posição mais fraca com alguém de outro clube da IA ou um agente livre que caiba no orçamento.
    /// Nunca mexe no elenco do jogador.
    /// </summary>
    public static IReadOnlyList<Transferencia> MovimentarIa(Mundo mundo, string clubeDoJogador, int rodadasDisputadas, IGeradorAleatorio aleatorio)
    {
        ArgumentNullException.ThrowIfNull(mundo);
        ArgumentNullException.ThrowIfNull(aleatorio);
        var negocios = new List<Transferencia>();
        if (!JanelaAberta(rodadasDisputadas))
        {
            return negocios;
        }

        foreach (var clube in mundo.Clubes.Where(c => c.Id != clubeDoJogador).ToArray())
        {
            if (clube.Caixa < 0 && clube.Elenco.Count > 22)
            {
                var titulares = clube.Elenco.OrderByDescending(j => j.ForcaGeral).ThenBy(j => j.Id).Take(11).ToHashSet();
                var venda = clube.Elenco.Where(j => !titulares.Contains(j))
                    .OrderByDescending(j => Valores.ValorDeMercado(j, mundo.TemporadaAtual)).ThenBy(j => j.Id)
                    .FirstOrDefault(j => MelhorOferta(mundo, clube, j) is { } o && o.Clube.Id != clubeDoJogador);
                if (venda is not null)
                {
                    var oferta = MelhorOferta(mundo, clube, venda)!;
                    clube.RemoverDoElenco(venda.Id);
                    clube.Lancar(CategoriaFinanceira.VendaDeJogadores, oferta.Valor);
                    oferta.Clube.Lancar(CategoriaFinanceira.CompraDeJogadores, -oferta.Valor);
                    Transferir(oferta.Clube, venda, mundo.TemporadaAtual);
                    negocios.Add(new Transferencia(venda.Id, venda.Nome, clube.Id, oferta.Clube.Id, oferta.Valor));
                }

                continue;
            }

            if (clube.Caixa <= 0 || !aleatorio.Chance(0.25) || clube.Elenco.Count >= ElencoMaximo - 2)
            {
                continue;
            }

            if (Reforco(mundo, clube, clubeDoJogador, rodadasDisputadas) is { } alvo)
            {
                var origem = alvo.Contrato?.ClubeId;
                var preco = Preco(alvo, mundo.TemporadaAtual);
                Comprar(mundo, clube, alvo, rodadasDisputadas);
                negocios.Add(new Transferencia(alvo.Id, alvo.Nome, origem, clube.Id, preco));
            }
        }

        return negocios;
    }

    /// <summary>Melhor contratação barata para a posição mais fraca do time (nula se não houver).</summary>
    private static Jogador? Reforco(Mundo mundo, Clube clube, string clubeDoJogador, int rodadasDisputadas)
    {
        var posicao = GeradorDeMundo.ElencoBase.Distinct()
            .Select(p => (Posicao: p, Melhor: clube.Elenco.Where(j => j.PosicaoPrincipal == p).Select(j => j.ForcaGeral).DefaultIfEmpty(0).Max()))
            .OrderBy(x => x.Melhor).ThenBy(x => x.Posicao)
            .First();
        var orcamento = clube.Caixa * 0.5m;
        var tetoSalarial = GeradorDeJogadores.Salario(posicao.Melhor + 8);

        return mundo.Clubes.Where(c => c.Id != clubeDoJogador && c != clube).SelectMany(c => c.Elenco)
            .Concat(mundo.AgentesLivres)
            .Where(j => j.PosicaoPrincipal == posicao.Posicao && j.ForcaGeral >= posicao.Melhor + 3)
            .Where(j => Preco(j, mundo.TemporadaAtual) <= orcamento && Valores.SalarioPedido(j) <= tetoSalarial)
            .Where(j => Impedimento(mundo, clube, j, rodadasDisputadas) is null)
            .OrderByDescending(j => j.ForcaGeral).ThenBy(j => j.Id)
            .FirstOrDefault();
    }

    private static Clube? DonoDe(Mundo mundo, Jogador jogador) =>
        jogador.Contrato is { } c ? mundo.Clube(c.ClubeId) : null;

    private static void Transferir(Clube destino, Jogador jogador, int temporadaAtual)
    {
        jogador.AssinarContrato(new Contrato(destino.Id, Valores.SalarioPedido(jogador), temporadaAtual + AnosDeContrato(jogador)));
        destino.AdicionarAoElenco(jogador);
    }
}
