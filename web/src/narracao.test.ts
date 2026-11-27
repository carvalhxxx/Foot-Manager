import { describe, expect, it } from 'vitest'
import type { EventoPartida } from './nucleo/contrato'
import { narrar } from './narracao'

const nomes = { mandante: 'Pampa Sport Club', visitante: 'Cerrado FC' }

describe('narrar', () => {
  it('o mesmo lance sempre recebe a mesma frase', () => {
    const gol: EventoPartida = { minuto: 23, acrescimo: 0, tipo: 'Gol', equipe: 'Mandante', jogador: 'Craque Silva' }

    expect(narrar(gol, nomes)).toBe(narrar({ ...gol }, nomes))
    expect(narrar(gol, nomes)).toContain('Craque Silva')
  })

  it('lances diferentes variam o texto', () => {
    const frases = new Set(
      Array.from({ length: 30 }, (_, i) =>
        narrar({ minuto: i + 1, acrescimo: 0, tipo: 'FinalizacaoParaFora', equipe: 'Visitante', jogador: 'Fulano' }, nomes),
      ),
    )

    expect(frases.size).toBeGreaterThan(1)
  })

  it('gol de escanteio e de falta têm narração própria', () => {
    const escanteio = narrar({ minuto: 10, acrescimo: 0, tipo: 'Gol', equipe: 'Visitante', jogador: 'Zagueirão', origem: 'Escanteio' }, nomes)
    const falta = narrar({ minuto: 10, acrescimo: 0, tipo: 'Gol', equipe: 'Visitante', jogador: 'Batedor', origem: 'Falta' }, nomes)

    expect(escanteio).toMatch(/escanteio|Cabeçada/i)
    expect(falta).toMatch(/falta/i)
  })

  it('usa o nome do time certo e o goleiro que defendeu', () => {
    const defesa = narrar(
      { minuto: 5, acrescimo: 0, tipo: 'FinalizacaoDefendida', equipe: 'Visitante', jogador: 'Atacante', outroJogador: 'Paredão' },
      nomes,
    )
    const cartao = narrar({ minuto: 5, acrescimo: 0, tipo: 'CartaoVermelho', equipe: 'Visitante', jogador: 'Brigão' }, nomes)

    expect(defesa).toContain('Paredão')
    expect(cartao).toContain('Brigão')
  })

  it('tipo desconhecido não quebra', () => {
    expect(narrar({ minuto: 1, acrescimo: 0, tipo: 'Novidade' }, nomes)).toBe('Novidade')
  })
})
