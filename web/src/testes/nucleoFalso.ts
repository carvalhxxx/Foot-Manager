import { vi } from 'vitest'
import type {
  ClubeResumo,
  Classificacao,
  Estado,
  FimDeTemporada,
  Jogador,
  Resposta,
  ResumoMundo,
  Rodada,
  Tatica,
} from '../nucleo/contrato'
import type { Nucleo } from '../nucleo/nucleo'

const ok = <T,>(dados: T): Resposta<T> => ({ ok: true, dados })

export const pampa: ClubeResumo = {
  id: 'pampa',
  nome: 'Pampa Sport Club',
  sigla: 'PAM',
  cidade: 'Porto Alegre',
  uf: 'RS',
  corPrimaria: '#C8102E',
  corSecundaria: '#FFFFFF',
  forcaTop11: 75.4,
  jogadores: 28,
  torcida: 88,
}

export const cerrado: ClubeResumo = {
  ...pampa,
  id: 'cerrado',
  nome: 'Cerrado FC',
  sigla: 'CER',
  cidade: 'Goiânia',
  uf: 'GO',
  forcaTop11: 67.1,
}

export const mundo: ResumoMundo = {
  nome: 'Base',
  temporada: 2027,
  totalJogadores: 1103,
  divisoes: [
    { id: 'serie-a', nome: 'Série A', clubes: [pampa, cerrado] },
    { id: 'serie-b', nome: 'Série B', clubes: [] },
  ],
}

function jogador(id: number, nome: string, posicao: string, extra: Partial<Jogador> = {}): Jogador {
  return {
    id,
    nome,
    idade: 25,
    posicao,
    posicoesSecundarias: [],
    forca: 70,
    estrela: false,
    condicao: 100,
    moral: 50,
    disponivel: true,
    rodadasLesionado: 0,
    rodadasSuspenso: 0,
    amarelos: 0,
    salario: 50000,
    contratoAte: 2029,
    jogos: 0,
    gols: 0,
    atributos: {
      finalizacao: 10, passe: 10, desarme: 10, drible: 10, velocidade: 10,
      resistencia: 10, decisao: 10, determinacao: 10, reflexos: 2, posicionamento: 2,
    },
    ...extra,
  }
}

export const elenco: Jogador[] = [
  jogador(1, 'Zé Goleiro', 'GOL'),
  ...Array.from({ length: 10 }, (_, i) => jogador(2 + i, `Titular ${i + 1}`, 'MC')),
  jogador(20, 'Craque Silva', 'ATA', { forca: 88, estrela: true }),
  jogador(21, 'Machucado Souza', 'ZAG', { disponivel: false, rodadasLesionado: 3 }),
]

const tatica: Tatica = {
  formacao: '4-4-2',
  mentalidade: 'Equilibrada',
  titulares: elenco.slice(0, 11).map((j, i) => ({
    posicao: i === 0 ? 'GOL' : 'MC',
    jogadorId: j.id,
    forcaNaPosicao: 70,
    improvisado: false,
  })),
  reservas: [20],
  formacoes: [
    { nome: '4-4-2', posicoes: [] },
    { nome: '4-3-3', posicoes: [] },
  ],
  mentalidades: ['MuitoDefensiva', 'Defensiva', 'Equilibrada', 'Ofensiva', 'MuitoOfensiva'],
}

export const rodada: Rodada = {
  rodada: 1,
  minhaPartida: {
    rodada: 1,
    mandante: pampa,
    visitante: cerrado,
    golsMandante: 2,
    golsVisitante: 1,
    posseMandante: 56,
    estatisticasMandante: { finalizacoes: 14, noAlvo: 6, escanteios: 5, faltas: 11, amarelos: 2, vermelhos: 0 },
    estatisticasVisitante: { finalizacoes: 9, noAlvo: 3, escanteios: 3, faltas: 13, amarelos: 3, vermelhos: 1 },
    eventos: [
      { minuto: 0, acrescimo: 0, tipo: 'InicioDeJogo' },
      { minuto: 23, acrescimo: 0, tipo: 'Gol', equipe: 'Mandante', jogador: 'Craque Silva', origem: 'JogadaTrabalhada' },
      { minuto: 30, acrescimo: 0, tipo: 'FinalizacaoParaFora', equipe: 'Visitante', jogador: 'Fulano' },
      { minuto: 45, acrescimo: 2, tipo: 'Intervalo' },
      { minuto: 90, acrescimo: 3, tipo: 'FimDeJogo' },
    ],
  },
  divisoes: [
    {
      divisaoId: 'serie-a',
      divisaoNome: 'Série A',
      jogos: [
        { mandanteId: 'pampa', mandanteSigla: 'PAM', mandanteNome: 'Pampa', golsMandante: 2, visitanteId: 'cerrado', visitanteSigla: 'CER', visitanteNome: 'Cerrado', golsVisitante: 1 },
        { mandanteId: 'x', mandanteSigla: 'XXX', mandanteNome: 'X', golsMandante: 0, visitanteId: 'y', visitanteSigla: 'YYY', visitanteNome: 'Y', golsVisitante: 0 },
      ],
    },
  ],
}

export const fimDeTemporada: FimDeTemporada = {
  ano: 2027,
  divisoes: [{ nome: 'Série A', campeao: 'Pampa Sport Club', promovidos: [], rebaixados: ['A', 'B', 'C', 'D'], artilheiro: { jogador: 'Craque Silva', clubeSigla: 'PAM', gols: 25, jogos: 38 } }],
  minhaPosicao: 1,
  minhaDivisao: 'Série A',
  movimento: 'titulo',
  aposentados: 20,
  revelacoes: 40,
  proximoAno: 2028,
}

function classificacao(divisaoId: string): Classificacao {
  return {
    divisaoId,
    nome: divisaoId === 'serie-a' ? 'Série A' : 'Série B',
    vagasAcesso: divisaoId === 'serie-a' ? 0 : 4,
    vagasRebaixamento: divisaoId === 'serie-a' ? 4 : 0,
    linhas: [
      { posicao: 1, clubeId: divisaoId === 'serie-a' ? 'pampa' : 'dunas', nome: divisaoId === 'serie-a' ? 'Pampa Sport Club' : 'Dunas FC', sigla: 'PAM', corPrimaria: '#000', corSecundaria: '#fff', jogos: 1, vitorias: 1, empates: 0, derrotas: 0, golsPro: 2, golsContra: 1, saldo: 1, pontos: 3 },
    ],
    artilharia: [{ jogador: 'Craque Silva', clubeSigla: 'PAM', gols: 1, jogos: 1 }],
  }
}

/** Núcleo falso com estado mínimo: rodada atual e fim de temporada. */
export function criarNucleoFalso(opcoes: { totalRodadas?: number } = {}) {
  const totalRodadas = opcoes.totalRodadas ?? 38
  let rodadaAtual = 0
  let taticaAtual = tatica

  const estado = (): Estado => ({
    ano: 2027,
    rodadaAtual,
    totalRodadas,
    temporadaEncerrada: rodadaAtual >= totalRodadas,
    clube: pampa,
    divisaoId: 'serie-a',
    divisaoNome: 'Série A',
    posicao: 3,
    pontos: rodadaAtual * 2,
    proximoJogo: rodadaAtual < totalRodadas ? { rodada: rodadaAtual + 1, adversario: cerrado, emCasa: true } : undefined,
    ultimosResultados: rodadaAtual > 0 ? [{ rodada: 1, adversarioSigla: 'CER', emCasa: true, golsPro: 2, golsContra: 1 }] : [],
    ajustesNaEscalacao: ['Fulano entra no lugar de Machucado Souza (lesionado).'],
  })

  const nucleo = {
    versao: vi.fn(() => '0.1.0'),
    carregarDefinicoes: vi.fn(() => ok(mundo)),
    novaCarreira: vi.fn(() => ok(estado())),
    estado: vi.fn(() => ok(estado())),
    elenco: vi.fn(() => ok(elenco)),
    tatica: vi.fn(() => ok(taticaAtual)),
    definirTatica: vi.fn((d) => {
      if (d.titulares.length !== 11) {
        return { ok: false as const, erros: [{ arquivo: '', local: '', mensagem: 'Escale exatamente 11 titulares.' }] }
      }
      taticaAtual = { ...taticaAtual, formacao: d.formacao, mentalidade: d.mentalidade, reservas: d.reservas }
      return ok(taticaAtual)
    }),
    escalacaoAutomatica: vi.fn(() => ok(taticaAtual)),
    avancarRodada: vi.fn(() => {
      rodadaAtual++
      return ok({ ...rodada, rodada: rodadaAtual })
    }),
    ultimaRodada: vi.fn(() => ok(rodadaAtual > 0 ? rodada : { rodada: 0, divisoes: [] })),
    classificacao: vi.fn((id: string) => ok(classificacao(id))),
    encerrarTemporada: vi.fn(() => {
      rodadaAtual = 0
      return ok(fimDeTemporada)
    }),
  } satisfies Nucleo

  return nucleo
}
