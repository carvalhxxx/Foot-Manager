// Contrato de dados com o núcleo C#. Espelha src/Jogo.Wasm/ContratoJson.cs:
// qualquer mudança lá precisa ser refletida aqui.

export interface ErroNucleo {
  arquivo: string
  local: string
  mensagem: string
}

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

export interface Divisao {
  id: string
  nome: string
  clubes: ClubeResumo[]
}

export interface ResumoMundo {
  nome: string
  temporada: number
  totalJogadores: number
  divisoes: Divisao[]
}

export type Resposta<T> = { ok: true; dados: T } | { ok: false; erros: ErroNucleo[] }
