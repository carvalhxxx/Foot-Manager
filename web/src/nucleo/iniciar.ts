import type { ErroNucleo, ResumoMundo } from './contrato'
import { carregarNucleo, type Nucleo } from './nucleo'

export type ResultadoInicio =
  | { ok: true; nucleo: Nucleo; mundo: ResumoMundo }
  | { ok: false; erros: ErroNucleo[] }

/** Carrega o núcleo e as definições do jogo base (os dois downloads correm em paralelo). */
export async function iniciarJogo(): Promise<ResultadoInicio> {
  const [nucleo, { arquivosDaBase }] = await Promise.all([carregarNucleo(), import('./definicoes')])
  const resposta = nucleo.carregarDefinicoes(arquivosDaBase())
  return resposta.ok ? { ok: true, nucleo, mundo: resposta.dados } : resposta
}

/** Seed aleatória para uma carreira nova (como texto: o C# usa inteiro de 64 bits). */
export function novaSeed(): string {
  const partes = crypto.getRandomValues(new Uint32Array(2))
  return ((BigInt(partes[0]) << 32n) | BigInt(partes[1])).toString()
}

/** Junta as mensagens de erro do núcleo num texto legível. */
export function descreverErros(erros: ErroNucleo[]): string[] {
  return erros.map((e) => [e.arquivo, e.local].filter(Boolean).join(' → ') + (e.arquivo ? ': ' : '') + e.mensagem)
}
