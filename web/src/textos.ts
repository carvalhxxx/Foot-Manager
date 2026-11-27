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
    rodada: 'Avançar rodada',
    atalho: 'Espaço',
    encerrarTemporada: 'Encerrar temporada',
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
