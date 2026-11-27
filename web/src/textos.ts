// Todos os textos da interface ficam aqui (preparado para tradução no futuro).
// Nada de texto fixo nos componentes.

import type { EventoPartida } from './nucleo/contrato'

export const textos = {
  nomeDoJogo: 'Foot Manager BR',
  menu: {
    inicio: 'Início',
    elenco: 'Elenco',
    tatica: 'Tática',
    partida: 'Partida',
    classificacao: 'Classificação',
    financas: 'Finanças',
    mercado: 'Mercado',
  },
  financas: {
    titulo: 'Finanças',
    caixa: 'Caixa',
    resultado: 'Resultado da temporada',
    folha: 'Folha salarial (mês)',
    receitasFixas: 'Receitas por temporada',
    tv: 'Cota de TV',
    patrocinio: 'Patrocínio',
    extrato: 'Extrato da temporada',
    semLancamentos: 'Nenhum lançamento ainda nesta temporada.',
    ingresso: 'Preço do ingresso',
    sugerido: (preco: string) => `Sugerido: ${preco}`,
    usarSugerido: 'Usar o sugerido',
    aplicar: 'Aplicar',
    dicaIngresso: 'Ingresso caro dá mais renda por torcedor, mas menos público. Boa fase e adversário grande lotam mais.',
    ultimoJogo: (publico: string, capacidade: string, renda: string) => `Último jogo em casa: ${publico} de ${capacidade} lugares, renda de ${renda}.`,
    categorias: {
      Bilheteria: 'Bilheteria',
      CotaDeTv: 'Cota de TV',
      Patrocinio: 'Patrocínio',
      Premiacao: 'Premiação',
      VendaDeJogadores: 'Venda de jogadores',
      Salarios: 'Salários',
      CompraDeJogadores: 'Compra de jogadores',
      Rescisoes: 'Rescisões',
    } as Record<string, string>,
  },
  tema: {
    alternarParaEscuro: 'Tema escuro',
    alternarParaClaro: 'Tema claro',
  },
  carregando: 'Carregando o núcleo do jogo…',
  simulando: 'Simulando a rodada…',
  erro: {
    titulo: 'Não foi possível iniciar o jogo',
    tentarDeNovo: 'Tentar de novo',
    inesperado: 'Erro inesperado ao carregar o núcleo.',
  },
  avancar: {
    rodada: 'Jogar rodada',
    voltarAoJogo: 'Voltar ao jogo',
    simularRapido: 'Simular rápido',
    atalho: 'Espaço',
    encerrarTemporada: 'Encerrar temporada',
  },
  aoVivo: {
    menu: 'Ao vivo',
    intervalo: 'Intervalo',
    encerrado: 'Encerrado',
    continuar: 'Continuar',
    pausar: 'Pausar',
    velocidade: 'Velocidade',
    velocidades: { normal: '1×', rapida: '3×', turbo: '10×' } as Record<string, string>,
    pularParaOFim: 'Pular para o fim',
    verResumo: 'Ver resumo da rodada',
    narracao: 'Narração',
    meuTime: 'Seu time',
    placarDaRodada: 'Placar da rodada',
    substituicao: 'Substituição',
    sai: 'Sai',
    entra: 'Entra',
    substituir: 'Substituir',
    escolha: 'Escolha…',
    restantes: (n: number) => `${n} ${n === 1 ? 'substituição restante' : 'substituições restantes'}`,
    semSubstituicoes: 'Sem substituições restantes.',
    vaga: (nome: string) => `${nome} (lesionado, saiu)`,
    pausas: {
      intervalo: 'Intervalo! Faça seus ajustes e continue quando quiser.',
      lesao: '🚑 Um jogador seu se machucou. Faça a substituição para não ficar com um a menos.',
      expulsao: '🟥 Seu time teve um jogador expulso. Que tal reorganizar o time?',
      fim: 'Fim de jogo!',
    } as Record<string, string>,
  },
  saves: {
    titulo: 'Suas carreiras',
    subtitulo: 'O jogo salva sozinho depois de cada rodada, aqui no seu navegador.',
    continuar: 'Continuar',
    apagar: 'Apagar',
    confirmarApagar: 'Confirmar exclusão',
    novoJogo: 'Novo jogo',
    importar: 'Importar save',
    exportar: 'Exportar save',
    menuInicial: 'Menu inicial',
    resumo: (ano: number, divisao: string, rodada: number) => `${ano} · ${divisao} · rodada ${rodada}`,
    salvoEm: (quando: string) => `Salvo ${quando}`,
    erroAoSalvar: 'Não foi possível salvar neste navegador. Exporte o save para não perder o progresso.',
    erroAoImportar: 'Não foi possível abrir este arquivo.',
    semArmazenamento: 'Este navegador não permite guardar dados: exporte o save para continuar depois.',
    nomeDoArquivo: (clube: string, ano: number, rodada: number) =>
      `foot-manager-${clube}-${ano}-rodada-${rodada}.json`,
  },
  escolher: {
    titulo: 'Escolha seu clube',
    subtitulo: 'Clubes grandes têm elenco forte e cobrança alta. Os pequenos são um desafio de verdade.',
    forca: 'Força',
    torcida: 'Torcida',
    comecar: (nome: string) => `Assumir o ${nome}`,
  },
  inicio: {
    temporada: (ano: number) => `Temporada ${ano}`,
    rodada: (atual: number, total: number) => `Rodada ${atual} de ${total}`,
    proximoJogo: 'Próximo jogo',
    emCasa: 'em casa',
    fora: 'fora',
    contra: 'contra',
    posicao: 'Posição',
    pontos: (p: number) => `${p} pontos`,
    ultimos: 'Últimos resultados',
    semJogos: 'Nenhum jogo disputado ainda.',
    ajustes: 'Ajustes na escalação para o próximo jogo',
    fimDaTemporada: 'A temporada acabou! Encerre-a para ver o resumo e começar a próxima.',
    janelaAberta: 'A janela de transferências está aberta: veja o Mercado.',
    vitoria: 'V',
    empate: 'E',
    derrota: 'D',
  },
  elenco: {
    titulo: 'Elenco',
    pos: 'Pos',
    nome: 'Nome',
    idade: 'Idade',
    forca: 'Força',
    condicao: 'Físico',
    moral: 'Moral',
    jogos: 'J',
    gols: 'G',
    salario: 'Salário',
    contrato: 'Contrato',
    situacao: 'Situação',
    estrela: 'Destaque',
    lesionado: (n: number) => `Lesionado (${n} ${n === 1 ? 'rodada' : 'rodadas'})`,
    suspenso: 'Suspenso',
    pendurado: (n: number) => `${n} amarelos`,
    disponivel: 'Disponível',
    valor: 'Valor',
    ultimoAno: 'Último ano',
    detalhes: (nome: string) => `Ver contrato de ${nome}`,
    contratoAte: (ano: number) => `Contrato até ${ano}`,
    salarioAtual: (salario: string) => `Salário: ${salario} por mês`,
    valorDeMercado: (valor: string) => `Valor de mercado: ${valor}`,
    renovar: 'Renovar',
    pedido: (salario: string) => `Para renovar, pede ${salario} por mês.`,
    anos: 'Duração',
    opcaoAnos: (n: number) => `${n} ${n === 1 ? 'ano' : 'anos'}`,
    semRenovacao: 'Sem renovação, ele sai de graça no fim da temporada.',
    dispensar: 'Dispensar',
    custoDispensa: (valor: string) => `Dispensar custa ${valor} de rescisão (3 salários).`,
    confirmarDispensa: (nome: string) => `Confirmar dispensa de ${nome}`,
    cancelar: 'Cancelar',
    fechar: 'Fechar',
    verPropostas: 'Ver propostas',
    proposta: (clube: string, valor: string) => `O ${clube} oferece ${valor}.`,
    semProposta: 'Nenhum clube quer contratá-lo agora.',
    janelaFechada: 'A janela de transferências está fechada.',
    vender: (valor: string) => `Vender por ${valor}`,
  },
  mercado: {
    titulo: 'Mercado',
    janelaAberta: 'Janela aberta: compre de outros clubes ou contrate agentes livres.',
    janelaFechada: 'Janela fechada: só agentes livres podem ser contratados agora.',
    caixa: (valor: string) => `Caixa: ${valor}`,
    posicao: 'Posição',
    todas: 'Todas',
    forcaMinima: 'Força mínima',
    precoMaximo: 'Preço máximo (R$)',
    apenasLivres: 'Só agentes livres',
    buscar: 'Buscar',
    clube: 'Clube',
    livre: 'Livre',
    preco: 'Preço',
    salario: 'Salário',
    nenhum: 'Nenhum jogador encontrado com esses filtros.',
    negociar: (nome: string) => `Negociar ${nome}`,
    proposta: (nome: string, clube: string, preco: string) => `Comprar ${nome} do ${clube} por ${preco}.`,
    propostaLivre: (nome: string) => `Contratar ${nome}, agente livre, sem custo de transferência.`,
    contrato: (salario: string, anos: number) => `Salário de ${salario} por mês, contrato de ${anos} ${anos === 1 ? 'ano' : 'anos'}.`,
    confirmar: 'Confirmar contratação',
    cancelar: 'Cancelar',
    contratado: (nome: string) => `${nome} foi contratado!`,
  },
  tatica: {
    titulo: 'Tática',
    formacao: 'Formação',
    mentalidade: 'Mentalidade',
    titulares: 'Titulares',
    reservas: 'Reservas',
    automatica: 'Escalação automática',
    dicaReservas: (max: number) => `Marque até ${max} reservas.`,
    improvisado: 'Improvisado',
    forcaNaPosicao: 'Força na posição',
    mentalidades: {
      MuitoDefensiva: 'Muito defensiva',
      Defensiva: 'Defensiva',
      Equilibrada: 'Equilibrada',
      Ofensiva: 'Ofensiva',
      MuitoOfensiva: 'Muito ofensiva',
    } as Record<string, string>,
  },
  partida: {
    semPartida: 'Nenhuma partida jogada ainda. Avance a rodada para jogar.',
    rodada: (n: number) => `Rodada ${n}`,
    posse: 'Posse de bola',
    finalizacoes: 'Finalizações',
    noAlvo: 'No alvo',
    escanteios: 'Escanteios',
    faltas: 'Faltas',
    amarelos: 'Amarelos',
    vermelhos: 'Vermelhos',
    lances: 'Lances',
    todosOsLances: 'Mostrar todos os lances',
    outrosJogos: 'Outros jogos da rodada',
  },
  classificacao: {
    titulo: 'Classificação',
    clube: 'Clube',
    pontos: 'P',
    jogos: 'J',
    vitorias: 'V',
    empates: 'E',
    derrotas: 'D',
    golsPro: 'GP',
    golsContra: 'GC',
    saldo: 'SG',
    artilharia: 'Artilharia',
    gols: 'Gols',
    legendaAcesso: 'Acesso',
    legendaRebaixamento: 'Rebaixamento',
    legendaCampeao: 'Campeão',
  },
  fim: {
    titulo: (ano: number) => `Fim da temporada ${ano}`,
    suaCampanha: (posicao: number, divisao: string) => `Seu time terminou em ${posicao}º na ${divisao}.`,
    conquistouTitulo: '🏆 Campeão!',
    acesso: '⬆️ Acesso conquistado!',
    rebaixamento: '⬇️ Rebaixado.',
    campeao: 'Campeão',
    sobem: 'Sobem',
    descem: 'Descem',
    artilheiro: 'Artilheiro',
    resultadoFinanceiro: (valor: string) => `Resultado financeiro da temporada: ${valor}.`,
    viradaDeAno: (aposentados: number, revelacoes: number) =>
      `${aposentados} jogadores se aposentaram e ${revelacoes} jovens subiram da base.`,
    comecar: (ano: number) => `Começar a temporada ${ano}`,
  },
} as const

/** Descrição curta de um evento da partida (a narração completa chega na E7). */
export function descreverEvento(e: EventoPartida, nomeDoTime: (equipe: EventoPartida['equipe']) => string): string {
  const time = nomeDoTime(e.equipe)
  switch (e.tipo) {
    case 'InicioDeJogo':
      return 'Começa o jogo!'
    case 'Intervalo':
      return 'Fim do primeiro tempo.'
    case 'FimDeJogo':
      return 'Fim de jogo!'
    case 'Gol':
      return `⚽ GOL do ${time}! ${e.jogador ?? ''}${e.origem === 'Escanteio' ? ', de cabeça após escanteio' : e.origem === 'Falta' ? ', de falta' : ''}.`
    case 'FinalizacaoDefendida':
      return `${e.jogador} finaliza e ${e.outroJogador ?? 'o goleiro'} defende.`
    case 'FinalizacaoNaTrave':
      return `${e.jogador} acerta a trave!`
    case 'FinalizacaoNoTravessao':
      return `${e.jogador} carimba o travessão!`
    case 'FinalizacaoPorCima':
      return `${e.jogador} chuta por cima.`
    case 'FinalizacaoParaFora':
      return `${e.jogador} finaliza para fora.`
    case 'Escanteio':
      return `Escanteio para o ${time}.`
    case 'Falta':
      return `Falta de ${e.jogador}.`
    case 'CartaoAmarelo':
      return `🟨 Amarelo para ${e.jogador} (${time}).`
    case 'CartaoVermelho':
      return `🟥 Expulso! ${e.jogador} (${time}).`
    case 'Lesao':
      return `🚑 ${e.jogador} (${time}) se machuca e sai.`
    case 'Substituicao':
      return `🔁 ${time}: sai ${e.jogador}, entra ${e.outroJogador}.`
    case 'TrocaDeMentalidade':
      return `${time} muda a postura: ${textos.tatica.mentalidades[e.detalhe ?? ''] ?? e.detalhe}.`
    case 'TrocaDeFormacao':
      return `${time} muda para o ${e.detalhe}.`
    default:
      return e.tipo
  }
}

/** Eventos que aparecem no resumo da partida (os demais só com "todos os lances"). */
export const eventosImportantes = new Set([
  'InicioDeJogo',
  'Intervalo',
  'FimDeJogo',
  'Gol',
  'CartaoAmarelo',
  'CartaoVermelho',
  'Lesao',
  'Substituicao',
  'FinalizacaoNaTrave',
  'FinalizacaoNoTravessao',
])

export function minutoDoEvento(e: EventoPartida): string {
  return e.acrescimo > 0 ? `${e.minuto}+${e.acrescimo}'` : `${e.minuto}'`
}

export const formatoDinheiro = new Intl.NumberFormat('pt-BR', {
  style: 'currency',
  currency: 'BRL',
  maximumFractionDigits: 0,
})
