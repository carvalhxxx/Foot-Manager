import { vi } from 'vitest'
import type {
  AoVivo as AoVivoDto,
  ClubeResumo,
  Decisao,
  EventoPartida,
  Financas,
  Classificacao,
  Estado,
  FimDeTemporada,
  Jogador,
  JogadorMercado,
  Mercado,
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
    valorDeMercado: 2000000,
    podeRenovar: false,
    salarioPedido: 55000,
    custoDaDispensa: 150000,
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
  jogador(20, 'Craque Silva', 'ATA', { forca: 88, estrela: true, podeRenovar: true, contratoAte: 2027 }),
  jogador(21, 'Machucado Souza', 'ZAG', { disponivel: false, rodadasLesionado: 3 }),
]

export const jogadoresDoMercado: JogadorMercado[] = [
  {
    id: 500, nome: 'Reforço Lima', idade: 24, posicao: 'ATA', forca: 78, estrela: false, clubeNome: 'Cerrado FC', clubeSigla: 'CER',
    livre: false, preco: 6000000, salarioPedido: 90000, valorDeMercado: 5000000, anosDeContrato: 4,
  },
  {
    id: 501, nome: 'Livre Costa', idade: 31, posicao: 'ZAG', forca: 66, estrela: false,
    livre: true, preco: 0, salarioPedido: 30000, valorDeMercado: 400000, anosDeContrato: 2,
  },
  {
    id: 502, nome: 'Caro Demais', idade: 27, posicao: 'MC', forca: 85, estrela: true, clubeNome: 'Cerrado FC', clubeSigla: 'CER',
    livre: false, preco: 50000000, salarioPedido: 400000, valorDeMercado: 40000000, anosDeContrato: 3,
    impedimento: 'Não há dinheiro em caixa para essa compra.',
  },
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
  meuResultadoFinanceiro: 4_500_000,
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

function financasFalsas(preco: number | undefined): Financas {
  return {
    caixa: 46_000_000,
    resultadoDaTemporada: 1_250_000,
    extrato: [
      { categoria: 'Bilheteria', valor: 2_000_000 },
      { categoria: 'Salarios', valor: -750_000 },
    ],
    folhaMensal: 2_900_000,
    cotaTvAnual: 4_400_000,
    patrocinioAnual: 12_000_000,
    precoIngresso: preco ?? 31,
    precoSugerido: 31,
    precoPersonalizado: preco !== undefined,
    capacidade: 53_000,
    ultimoPublico: 31_000,
    ultimaRenda: 961_000,
  }
}

/** Jogo ao vivo de mentira: gol aos 23', intervalo aos 45' e fim aos 93'. */
const FIM_DO_JOGO_FALSO = 93

/** Núcleo falso com estado mínimo: rodada atual, jogo ao vivo e fim de temporada. */
export function criarNucleoFalso(opcoes: { totalRodadas?: number } = {}) {
  const totalRodadas = opcoes.totalRodadas ?? 38
  let rodadaAtual = 0
  let taticaAtual = tatica
  let precoIngresso: number | undefined
  let aoVivo: { minuto: number; mentalidade: string; formacao: string; emCampo: number[]; reservas: number[]; trocas: number } | undefined

  const fotoAoVivo = (pausa?: AoVivoDto['pausa']): AoVivoDto => {
    const v = aoVivo!
    // Como no motor real: no intervalo já é 2º tempo, com o relógio parado em 45.
    const segundoTempo = v.minuto >= 45
    const eventos: EventoPartida[] = [{ minuto: 0, acrescimo: 0, tipo: 'InicioDeJogo' }]
    if (v.minuto >= 23) {
      eventos.push({ minuto: 23, acrescimo: 0, tipo: 'Gol', equipe: 'Mandante', jogador: 'Craque Silva', origem: 'JogadaTrabalhada' })
    }
    if (v.minuto >= 45) {
      eventos.push({ minuto: 45, acrescimo: 0, tipo: 'Intervalo' })
    }
    if (v.minuto >= FIM_DO_JOGO_FALSO) {
      eventos.push({ minuto: 90, acrescimo: 3, tipo: 'FimDeJogo' })
    }
    const encerrada = v.minuto >= FIM_DO_JOGO_FALSO
    const porId = new Map(elenco.map((j) => [j.id, j]))
    return {
      rodada: rodadaAtual + 1,
      rodadaEncerrada: encerrada,
      pausa,
      minhaPartida: {
        mandante: pampa,
        visitante: cerrado,
        souMandante: true,
        golsMandante: v.minuto >= 23 ? 1 : 0,
        golsVisitante: 0,
        tempo: segundoTempo ? 2 : 1,
        minuto: Math.min(v.minuto, 90),
        acrescimo: Math.max(0, v.minuto - 90),
        encerrada,
        posseMandante: 55,
        estatisticasMandante: rodada.minhaPartida!.estatisticasMandante,
        estatisticasVisitante: rodada.minhaPartida!.estatisticasVisitante,
        formacao: v.formacao,
        mentalidade: v.mentalidade,
        substituicoesRestantes: 5 - v.trocas,
        emCampo: v.emCampo.map((id) => ({ ...porId.get(id)!, condicao: 90, amarelos: 0, gols: 0 })),
        reservas: v.reservas.map((id) => ({ ...porId.get(id)!, condicao: 100 })),
        vagasAbertas: [],
        eventos,
      },
      placares: [
        {
          divisaoId: 'serie-a',
          divisaoNome: 'Série A',
          jogos: [{ mandanteId: 'pampa', mandanteSigla: 'PAM', golsMandante: 0, visitanteId: 'cerrado', visitanteSigla: 'CER', golsVisitante: 0, minuto: Math.min(v.minuto, 90), acrescimo: 0, encerrado: encerrada }],
        },
      ],
    }
  }

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
    rodadaAoVivo: aoVivo !== undefined,
    janelaAberta: rodadaAtual < 3,
  })

  const nucleo = {
    versao: vi.fn(() => '0.1.0'),
    carregarDefinicoes: vi.fn(() => ok(mundo)),
    novaCarreira: vi.fn(() => ok(estado())),
    estado: vi.fn(() => ok(estado())),
    elenco: vi.fn(() => ok(elenco)),
    renovarContrato: vi.fn((jogadorId: number, anos: number) =>
      ok(elenco.map((j) => (j.id === jogadorId ? { ...j, podeRenovar: false, contratoAte: 2027 + anos, salario: j.salarioPedido } : j))),
    ),
    dispensar: vi.fn((jogadorId: number) => ok(elenco.filter((j) => j.id !== jogadorId))),
    mercado: vi.fn((busca: { posicao?: string; forcaMinima: number; apenasLivres: boolean }) => {
      const resultado: Mercado = {
        janelaAberta: rodadaAtual < 3,
        caixa: 20000000,
        elencoAtual: elenco.length,
        posicoes: ['GOL', 'ZAG', 'MC', 'ATA'],
        jogadores: jogadoresDoMercado.filter(
          (j) => (!busca.posicao || j.posicao === busca.posicao) && j.forca >= busca.forcaMinima && (!busca.apenasLivres || j.livre),
        ),
      }
      return ok(resultado)
    }),
    comprar: vi.fn(() => ok(elenco)),
    ofertaPor: vi.fn((jogadorId: number) => ok({ jogadorId, clubeNome: 'Cerrado FC', valor: 3000000, janelaAberta: true })),
    vender: vi.fn((jogadorId: number) => ok(elenco.filter((j) => j.id !== jogadorId))),
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
    iniciarAoVivo: vi.fn(() => {
      aoVivo ??= {
        minuto: 0,
        mentalidade: 'Equilibrada',
        formacao: '4-4-2',
        emCampo: elenco.slice(0, 11).map((j) => j.id),
        reservas: [20, 21],
        trocas: 0,
      }
      return ok(fotoAoVivo())
    }),
    aoVivo: vi.fn(() => ok(fotoAoVivo())),
    avancarAoVivo: vi.fn((minutos: number) => {
      const v = aoVivo!
      let pausa: AoVivoDto['pausa']
      for (let i = 0; i < minutos && v.minuto < FIM_DO_JOGO_FALSO && !pausa; i++) {
        v.minuto++
        pausa = v.minuto === 45 ? 'intervalo' : v.minuto === FIM_DO_JOGO_FALSO ? 'fim' : undefined
      }
      return ok(fotoAoVivo(pausa))
    }),
    decidirAoVivo: vi.fn((d: Decisao) => {
      const v = aoVivo!
      if (d.tipo === 'mentalidade') {
        v.mentalidade = d.mentalidade
      } else if (d.tipo === 'formacao') {
        v.formacao = d.formacao
      } else {
        v.emCampo = v.emCampo.map((id) => (id === d.saiId ? d.entraId : id))
        v.reservas = v.reservas.filter((id) => id !== d.entraId)
        v.trocas++
      }
      return ok(fotoAoVivo())
    }),
    concluirAoVivo: vi.fn(() => {
      aoVivo = undefined
      rodadaAtual++
      return ok({ ...rodada, rodada: rodadaAtual })
    }),
    financas: vi.fn(() => ok(financasFalsas(precoIngresso))),
    definirPrecoIngresso: vi.fn((preco: number | undefined) => {
      if (preco !== undefined && (preco < 1 || preco > 1000)) {
        return { ok: false as const, erros: [{ arquivo: '', local: '', mensagem: 'O ingresso deve custar entre R$ 1 e R$ 1.000.' }] }
      }
      precoIngresso = preco
      return ok(financasFalsas(precoIngresso))
    }),
    // Save falso: o conteúdo só guarda a rodada.
    exportarSave: vi.fn(() =>
      aoVivo
        ? { ok: false as const, erros: [{ arquivo: '', local: '', mensagem: 'Não dá para salvar no meio de uma rodada ao vivo.' }] }
        : ok({
            conteudo: JSON.stringify({ falso: true, rodada: rodadaAtual }),
            clubeId: pampa.id,
            clubeNome: pampa.nome,
            corPrimaria: pampa.corPrimaria,
            corSecundaria: pampa.corSecundaria,
            ano: 2027,
            rodada: rodadaAtual,
            divisao: 'Série A',
          }),
    ),
    carregarSave: vi.fn((conteudo: string) => {
      try {
        const lido = JSON.parse(conteudo) as { falso?: boolean; rodada?: number }
        if (lido.falso) {
          rodadaAtual = lido.rodada ?? 0
          aoVivo = undefined
          return ok(estado())
        }
      } catch {
        // cai no erro abaixo
      }
      return { ok: false as const, erros: [{ arquivo: '', local: '', mensagem: 'O arquivo não é um save válido do Foot Manager BR.' }] }
    }),
  } satisfies Nucleo

  return nucleo
}
