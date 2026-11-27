// Contrato de dados com o núcleo C#. Espelha src/Jogo.Aplicacao/Contrato.cs:
// qualquer mudança lá precisa ser refletida aqui.

export interface ErroNucleo {
  arquivo: string
  local: string
  mensagem: string
}

export type Resposta<T> = { ok: true; dados: T } | { ok: false; erros: ErroNucleo[] }

export interface ClubeResumo {
  id: string
  nome: string
  sigla: string
  cidade: string
  uf: string
  corPrimaria: string
  corSecundaria: string
  forcaTop11: number
  jogadores: number
  torcida: number
}

export interface DivisaoResumo {
  id: string
  nome: string
  clubes: ClubeResumo[]
}

export interface ResumoMundo {
  nome: string
  temporada: number
  totalJogadores: number
  divisoes: DivisaoResumo[]
}

// ---------------------------------------------------------------- carreira

export interface ProximoJogo {
  rodada: number
  adversario: ClubeResumo
  emCasa: boolean
}

export interface ResultadoRecente {
  rodada: number
  adversarioSigla: string
  emCasa: boolean
  golsPro: number
  golsContra: number
}

export interface Estado {
  ano: number
  rodadaAtual: number
  totalRodadas: number
  temporadaEncerrada: boolean
  clube: ClubeResumo
  divisaoId: string
  divisaoNome: string
  posicao: number
  pontos: number
  proximoJogo?: ProximoJogo
  ultimosResultados: ResultadoRecente[]
  ajustesNaEscalacao: string[]
}

export interface Atributos {
  finalizacao: number
  passe: number
  desarme: number
  drible: number
  velocidade: number
  resistencia: number
  decisao: number
  determinacao: number
  reflexos: number
  posicionamento: number
}

export interface Jogador {
  id: number
  nome: string
  idade: number
  posicao: string
  posicoesSecundarias: string[]
  forca: number
  estrela: boolean
  condicao: number
  moral: number
  disponivel: boolean
  rodadasLesionado: number
  rodadasSuspenso: number
  amarelos: number
  salario: number
  contratoAte: number
  jogos: number
  gols: number
  atributos: Atributos
}

export interface Formacao {
  nome: string
  posicoes: string[]
}

export interface Titular {
  posicao: string
  jogadorId: number
  forcaNaPosicao: number
  improvisado: boolean
}

export interface Tatica {
  formacao: string
  mentalidade: string
  titulares: Titular[]
  reservas: number[]
  formacoes: Formacao[]
  mentalidades: string[]
}

export interface DefinicaoTatica {
  formacao: string
  mentalidade: string
  titulares: number[]
  reservas: number[]
}

// ---------------------------------------------------------------- partidas

export interface Estatisticas {
  finalizacoes: number
  noAlvo: number
  escanteios: number
  faltas: number
  amarelos: number
  vermelhos: number
}

export interface EventoPartida {
  minuto: number
  acrescimo: number
  tipo: string
  equipe?: 'Mandante' | 'Visitante'
  jogador?: string
  outroJogador?: string
  origem?: string
  detalhe?: string
}

export interface Partida {
  rodada: number
  mandante: ClubeResumo
  visitante: ClubeResumo
  golsMandante: number
  golsVisitante: number
  posseMandante: number
  estatisticasMandante: Estatisticas
  estatisticasVisitante: Estatisticas
  eventos: EventoPartida[]
}

export interface Placar {
  mandanteId: string
  mandanteSigla: string
  mandanteNome: string
  golsMandante: number
  visitanteId: string
  visitanteSigla: string
  visitanteNome: string
  golsVisitante: number
}

export interface RodadaDivisao {
  divisaoId: string
  divisaoNome: string
  jogos: Placar[]
}

export interface Rodada {
  rodada: number
  minhaPartida?: Partida
  divisoes: RodadaDivisao[]
}

// ---------------------------------------------------------------- tabelas

export interface LinhaTabela {
  posicao: number
  clubeId: string
  nome: string
  sigla: string
  corPrimaria: string
  corSecundaria: string
  jogos: number
  vitorias: number
  empates: number
  derrotas: number
  golsPro: number
  golsContra: number
  saldo: number
  pontos: number
}

export interface Artilheiro {
  jogador: string
  clubeSigla: string
  gols: number
  jogos: number
}

export interface Classificacao {
  divisaoId: string
  nome: string
  vagasAcesso: number
  vagasRebaixamento: number
  linhas: LinhaTabela[]
  artilharia: Artilheiro[]
}

export interface ResumoDivisao {
  nome: string
  campeao: string
  promovidos: string[]
  rebaixados: string[]
  artilheiro?: Artilheiro
}

export interface FimDeTemporada {
  ano: number
  divisoes: ResumoDivisao[]
  minhaPosicao: number
  minhaDivisao: string
  movimento?: 'acesso' | 'rebaixamento' | 'titulo'
  aposentados: number
  revelacoes: number
  proximoAno: number
}
