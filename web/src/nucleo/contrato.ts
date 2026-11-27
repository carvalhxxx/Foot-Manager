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
  rodadaAoVivo: boolean
  janelaAberta: boolean
  contratosAcabando: number
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
  valorDeMercado: number
  podeRenovar: boolean
  salarioPedido: number
  custoDaDispensa: number
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
  marcacao: string
  concentracao: string
  marcacoes: string[]
  concentracoes: string[]
  /** Capitão escolhido (ausente = automático). */
  capitaoId?: number
  /** Cobrador de faltas escolhido (ausente = sem cobrador fixo). */
  cobradorId?: number
  /** Quem vai ser o capitão (o escolhido ou o automático). */
  capitaoEfetivo?: number
  /** Os 3 titulares melhores em Finalização + Passe. */
  sugestaoDeCobradores: number[]
}

export interface DefinicaoTatica {
  formacao: string
  mentalidade: string
  titulares: number[]
  reservas: number[]
  marcacao?: string
  concentracao?: string
  capitaoId?: number
  cobradorId?: number
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
  transferencias: Transferencia[]
}

export interface Transferencia {
  jogador: string
  deSigla?: string
  paraSigla: string
  paraNome: string
  valor: number
}

// ---------------------------------------------------------------- ao vivo

export interface JogadorEmCampo {
  id: number
  nome: string
  posicao: string
  forca: number
  condicao: number
  amarelos: number
  gols: number
}

export interface Reserva {
  id: number
  nome: string
  posicao: string
  forca: number
  condicao: number
}

export interface Vaga {
  jogadorId: number
  nome: string
  posicao: string
}

export interface MinhaPartidaAoVivo {
  mandante: ClubeResumo
  visitante: ClubeResumo
  souMandante: boolean
  golsMandante: number
  golsVisitante: number
  tempo: number
  minuto: number
  acrescimo: number
  encerrada: boolean
  posseMandante: number
  estatisticasMandante: Estatisticas
  estatisticasVisitante: Estatisticas
  formacao: string
  mentalidade: string
  marcacao: string
  concentracao: string
  substituicoesRestantes: number
  emCampo: JogadorEmCampo[]
  reservas: Reserva[]
  vagasAbertas: Vaga[]
  eventos: EventoPartida[]
}

export interface PlacarAoVivo {
  mandanteId: string
  mandanteSigla: string
  golsMandante: number
  visitanteId: string
  visitanteSigla: string
  golsVisitante: number
  minuto: number
  acrescimo: number
  encerrado: boolean
}

export interface RodadaDivisaoAoVivo {
  divisaoId: string
  divisaoNome: string
  jogos: PlacarAoVivo[]
}

export type MotivoPausa = 'intervalo' | 'lesao' | 'expulsao' | 'fim'

export interface AoVivo {
  rodada: number
  rodadaEncerrada: boolean
  pausa?: MotivoPausa
  minhaPartida?: MinhaPartidaAoVivo
  placares: RodadaDivisaoAoVivo[]
}

export type Decisao =
  | { tipo: 'substituicao'; saiId: number; entraId: number }
  | { tipo: 'mentalidade'; mentalidade: string }
  | { tipo: 'formacao'; formacao: string }
  | { tipo: 'estilo'; marcacao: string; concentracao: string }

// ---------------------------------------------------------------- finanças

export interface Lancamento {
  categoria: string
  valor: number
}

export interface Financas {
  caixa: number
  resultadoDaTemporada: number
  extrato: Lancamento[]
  folhaMensal: number
  cotaTvAnual: number
  patrocinioAnual: number
  precoIngresso: number
  precoSugerido: number
  precoPersonalizado: boolean
  capacidade: number
  ultimoPublico?: number
  ultimaRenda?: number
}

// ---------------------------------------------------------------- saves

/** Save pronto para guardar: o conteúdo é texto opaco (só o C# lê) + um resumo para a lista. */
export interface SaveExportado {
  conteudo: string
  clubeId: string
  clubeNome: string
  corPrimaria: string
  corSecundaria: string
  ano: number
  rodada: number
  divisao: string
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
  meuResultadoFinanceiro: number
}

export interface BuscaMercado {
  posicao?: string
  forcaMinima: number
  precoMaximo?: number
  apenasLivres: boolean
}

export interface JogadorMercado {
  id: number
  nome: string
  idade: number
  posicao: string
  forca: number
  estrela: boolean
  clubeNome?: string
  clubeSigla?: string
  livre: boolean
  preco: number
  salarioPedido: number
  valorDeMercado: number
  anosDeContrato: number
  impedimento?: string
}

export interface Mercado {
  janelaAberta: boolean
  caixa: number
  elencoAtual: number
  posicoes: string[]
  jogadores: JogadorMercado[]
}

export interface Oferta {
  jogadorId: number
  clubeNome?: string
  valor: number
  janelaAberta: boolean
}
