// Apoio da tela Tática: onde desenhar cada posição no campo e como trocar jogadores de lugar.
// É só arrumação da interface (quem está em qual vaga); validar a escalação continua com o núcleo C#.

/** Uma vaga na escalação: titular (pela ordem da formação), banco ou fora do time. */
export type Local =
  | { tipo: 'titular'; indice: number }
  | { tipo: 'reserva'; indice: number }
  | { tipo: 'fora'; id?: number }

export interface Escalacao {
  titulares: number[]
  reservas: number[]
}

// Linha (altura no campo, 0 = ataque, 100 = gol) e lado preferido de cada posição.
const LINHA: Record<string, number> = {
  ATA: 12, SA: 24, PD: 22, PE: 22, MO: 36, MD: 49, ME: 49, MC: 50, VOL: 63, ALD: 66, ALE: 66, LD: 77, LE: 77, ZAG: 80, GOL: 93,
}
const LADO: Record<string, number> = { LE: 12, ALE: 10, ME: 12, PE: 16, LD: 88, ALD: 90, MD: 88, PD: 84 }

/** Posição (em %) de cada titular no campo, espalhando quem divide a mesma linha central. */
export function posicoesNoCampo(siglas: string[]): { x: number; y: number }[] {
  const resultado = siglas.map((s) => ({ x: LADO[s] ?? 50, y: LINHA[s] ?? 50 }))
  const centrais = new Map<number, number[]>()
  siglas.forEach((s, i) => {
    if (LADO[s] === undefined) {
      const y = resultado[i].y
      centrais.set(y, [...(centrais.get(y) ?? []), i])
    }
  })
  for (const indices of centrais.values()) {
    const espaco = indices.length > 3 ? 16 : 22
    indices.forEach((indice, k) => {
      resultado[indice].x = 50 + (k - (indices.length - 1) / 2) * espaco
    })
  }
  return resultado
}

export function mesmoLocal(a: Local, b: Local): boolean {
  if (a.tipo !== b.tipo) return false
  if (a.tipo === 'fora' && b.tipo === 'fora') return a.id === b.id
  return a.tipo !== 'fora' && b.tipo !== 'fora' && a.indice === b.indice
}

function idEm(escalacao: Escalacao, local: Local): number | undefined {
  if (local.tipo === 'titular') return escalacao.titulares[local.indice]
  if (local.tipo === 'reserva') return escalacao.reservas[local.indice]
  return local.id
}

/**
 * Troca o que está em <origem> com o que está em <destino>. Devolve a escalação nova, ou undefined se a
 * troca não fizer sentido (ex.: deixar uma vaga de titular vazia).
 */
export function trocar(escalacao: Escalacao, origem: Local, destino: Local, maximoReservas: number): Escalacao | undefined {
  if (mesmoLocal(origem, destino)) return undefined
  const a = idEm(escalacao, origem)
  if (a === undefined) return undefined
  const b = idEm(escalacao, destino)

  const titulares = [...escalacao.titulares]
  const reservas: (number | undefined)[] = [...escalacao.reservas]

  const colocar = (local: Local, id: number | undefined): boolean => {
    if (local.tipo === 'titular') {
      if (id === undefined) return false
      titulares[local.indice] = id
    } else if (local.tipo === 'reserva') {
      reservas[local.indice] = id
    }
    return true
  }

  if (!colocar(destino, a) || !colocar(origem, b)) return undefined
  const final = reservas.filter((id): id is number => id !== undefined && !titulares.includes(id))
  if (final.length > maximoReservas) return undefined
  return { titulares, reservas: final }
}

/** Lê o destino do atributo data-alvo ("titular:3", "reserva:7", "fora" ou "fora:42"). */
export function lerAlvo(texto: string | undefined): Local | undefined {
  if (!texto) return undefined
  const [tipo, valor] = texto.split(':')
  const numero = valor === undefined ? undefined : Number(valor)
  if (tipo === 'titular' && numero !== undefined) return { tipo, indice: numero }
  if (tipo === 'reserva' && numero !== undefined) return { tipo, indice: numero }
  if (tipo === 'fora') return { tipo, id: numero }
  return undefined
}

export function escreverAlvo(local: Local): string {
  if (local.tipo === 'fora') return local.id === undefined ? 'fora' : `fora:${local.id}`
  return `${local.tipo}:${local.indice}`
}
