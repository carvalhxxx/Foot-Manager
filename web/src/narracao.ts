// Narração do jogo ao vivo: vários textos por tipo de lance, para não ficar repetitivo.
// É texto de interface (como textos.ts), por isso mora no TypeScript; o núcleo C# só gera os eventos.
// A escolha do texto é determinística: o mesmo lance sempre recebe a mesma frase.

import type { EventoPartida } from './nucleo/contrato'
import { descreverEstilo, textos } from './textos'

type Modelo = (d: { jogador: string; outro: string; time: string; adversario: string; detalhe: string }) => string

const MODELOS: Record<string, Modelo[]> = {
  InicioDeJogo: [
    ({ time, adversario }) => `Rola a bola! ${time} e ${adversario} começam o jogo.`,
    () => 'Apita o árbitro, começa a partida!',
  ],
  Intervalo: [() => 'Fim do primeiro tempo. Hora de ajustar o time no vestiário.', () => 'Intervalo! Os times descem para o vestiário.'],
  FimDeJogo: [() => 'Fim de papo! O árbitro encerra a partida.', () => 'Apita o árbitro: fim de jogo!'],
  Gol: [
    ({ jogador, time }) => `GOOOOL! ${jogador} balança a rede! Gol do ${time}!`,
    ({ jogador, time }) => `É GOL DO ${time.toUpperCase()}! ${jogador} não perdoa!`,
    ({ jogador }) => `${jogador} bate firme e manda para o fundo do gol! GOOOL!`,
    ({ jogador, time }) => `Que golaço! ${jogador} marca para o ${time}!`,
  ],
  GolDeEscanteio: [
    ({ jogador, time }) => `GOL! Escanteio na área e ${jogador} sobe mais que todo mundo! ${time} marca!`,
    ({ jogador }) => `Cabeçada certeira de ${jogador} depois do escanteio! GOOOL!`,
  ],
  GolDeFalta: [
    ({ jogador }) => `GOL DE FALTA! ${jogador} acerta o ângulo, sem chance para o goleiro!`,
    ({ jogador, time }) => `${jogador} cobra a falta com perfeição! Gol do ${time}!`,
  ],
  FinalizacaoDefendida: [
    ({ jogador, outro }) => `${jogador} finaliza e ${outro || 'o goleiro'} faz grande defesa!`,
    ({ jogador, outro }) => `Chute de ${jogador}... ${outro || 'o goleiro'} espalma!`,
    ({ jogador, outro }) => `${outro || 'O goleiro'} segura firme o chute de ${jogador}.`,
  ],
  FinalizacaoNaTrave: [
    ({ jogador }) => `NA TRAVE! ${jogador} quase abre o placar!`,
    ({ jogador }) => `Uhhh! ${jogador} acerta a trave!`,
  ],
  FinalizacaoNoTravessao: [({ jogador }) => `No travessão! A bomba de ${jogador} explode no ferro!`],
  FinalizacaoPorCima: [
    ({ jogador }) => `${jogador} arrisca e manda por cima do gol.`,
    ({ jogador }) => `Chute alto de ${jogador}, a bola vai para a arquibancada.`,
  ],
  FinalizacaoParaFora: [
    ({ jogador }) => `${jogador} finaliza, mas a bola sai à direita do gol.`,
    ({ jogador }) => `Tentativa de ${jogador}, para fora.`,
    ({ jogador }) => `${jogador} chuta cruzado e erra o alvo.`,
  ],
  Escanteio: [({ time }) => `Escanteio para o ${time}.`, ({ time }) => `A bola desvia na zaga: escanteio para o ${time}.`],
  Falta: [({ jogador }) => `Falta de ${jogador}.`, ({ jogador }) => `${jogador} chega atrasado e o árbitro marca falta.`],
  CartaoAmarelo: [
    ({ jogador, time }) => `🟨 Cartão amarelo para ${jogador} (${time}).`,
    ({ jogador }) => `🟨 ${jogador} é advertido com o amarelo.`,
  ],
  CartaoVermelho: [
    ({ jogador, time }) => `🟥 EXPULSO! ${jogador} recebe o vermelho e o ${time} fica com um a menos!`,
    ({ jogador }) => `🟥 Vermelho para ${jogador}! Vai mais cedo para o chuveiro.`,
  ],
  Lesao: [
    ({ jogador, time }) => `🚑 ${jogador} (${time}) sente a lesão e não tem condições de continuar.`,
    ({ jogador }) => `🚑 ${jogador} cai no gramado e pede substituição.`,
  ],
  Substituicao: [({ jogador, outro, time }) => `🔁 Substituição no ${time}: sai ${jogador}, entra ${outro}.`],
  TrocaDeMentalidade: [
    ({ time, detalhe }) => `📋 O ${time} muda a postura: ${textos.tatica.mentalidades[detalhe] ?? detalhe}.`,
  ],
  TrocaDeFormacao: [({ time, detalhe }) => `📋 O ${time} muda o esquema para o ${detalhe}.`],
  TrocaDeEstilo: [({ time, detalhe }) => `📋 O ${time} ajusta o estilo: ${descreverEstilo(detalhe)}.`],
}

/** Hash simples e estável para escolher o modelo (não é regra de jogo, só variedade de texto). */
function indice(e: EventoPartida, quantidade: number): number {
  const chave = `${e.minuto}|${e.acrescimo}|${e.tipo}|${e.jogador ?? ''}|${e.outroJogador ?? ''}`
  let h = 2166136261
  for (let i = 0; i < chave.length; i++) {
    h = Math.imul(h ^ chave.charCodeAt(i), 16777619)
  }
  return (h >>> 0) % quantidade
}

export function narrar(e: EventoPartida, nomes: { mandante: string; visitante: string }): string {
  const time = e.equipe === 'Mandante' ? nomes.mandante : e.equipe === 'Visitante' ? nomes.visitante : nomes.mandante
  const adversario = e.equipe === 'Visitante' ? nomes.mandante : nomes.visitante
  const tipo =
    e.tipo === 'Gol' && e.origem === 'Escanteio' ? 'GolDeEscanteio'
    : e.tipo === 'Gol' && e.origem === 'Falta' ? 'GolDeFalta'
    : e.tipo
  const modelos = MODELOS[tipo]
  if (!modelos) {
    return e.tipo
  }

  return modelos[indice(e, modelos.length)]({
    jogador: e.jogador ?? '',
    outro: e.outroJogador ?? '',
    time,
    adversario,
    detalhe: e.detalhe ?? '',
  })
}
