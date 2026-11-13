import type { ErroNucleo, ResumoMundo } from './contrato'
import { carregarNucleo } from './nucleo'

export type ResultadoInicio =
  | { ok: true; versao: string; mundo: ResumoMundo }
  | { ok: false; erros: ErroNucleo[] }

/** Carrega o núcleo e o mundo do jogo base (os dois downloads correm em paralelo). */
export async function iniciarJogo(): Promise<ResultadoInicio> {
  const [nucleo, { arquivosDaBase }] = await Promise.all([carregarNucleo(), import('./definicoes')])
  const resposta = nucleo.carregarDefinicoes(arquivosDaBase())
  return resposta.ok ? { ok: true, versao: nucleo.versao(), mundo: resposta.dados } : resposta
}
