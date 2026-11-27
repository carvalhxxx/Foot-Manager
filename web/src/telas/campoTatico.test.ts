import { describe, expect, it } from 'vitest'
import { lerAlvo, posicoesNoCampo, trocar } from './campoTatico'

const base = { titulares: [1, 2, 3], reservas: [10, 11] }

describe('campo tático', () => {
  it('troca dois titulares de vaga', () => {
    expect(trocar(base, { tipo: 'titular', indice: 0 }, { tipo: 'titular', indice: 2 }, 7)).toEqual({
      titulares: [3, 2, 1],
      reservas: [10, 11],
    })
  })

  it('titular e reserva trocam de lugar', () => {
    expect(trocar(base, { tipo: 'reserva', indice: 1 }, { tipo: 'titular', indice: 0 }, 7)).toEqual({
      titulares: [11, 2, 3],
      reservas: [10, 1],
    })
  })

  it('quem vem de fora entra e o titular sai do time', () => {
    expect(trocar(base, { tipo: 'fora', id: 99 }, { tipo: 'titular', indice: 1 }, 7)).toEqual({
      titulares: [1, 99, 3],
      reservas: [10, 11],
    })
  })

  it('reserva arrastado para fora sai do banco; de fora para vaga vazia entra', () => {
    expect(trocar(base, { tipo: 'reserva', indice: 0 }, { tipo: 'fora' }, 7)).toEqual({ titulares: [1, 2, 3], reservas: [11] })
    expect(trocar(base, { tipo: 'fora', id: 50 }, { tipo: 'reserva', indice: 2 }, 7)).toEqual({ titulares: [1, 2, 3], reservas: [10, 11, 50] })
  })

  it('não deixa vaga de titular vazia nem passa do limite do banco', () => {
    expect(trocar(base, { tipo: 'titular', indice: 0 }, { tipo: 'reserva', indice: 2 }, 7)).toBeUndefined()
    expect(trocar(base, { tipo: 'titular', indice: 0 }, { tipo: 'fora' }, 7)).toBeUndefined()
    expect(trocar(base, { tipo: 'fora', id: 50 }, { tipo: 'reserva', indice: 2 }, 2)).toBeUndefined()
  })

  it('espalha os zagueiros e põe laterais nos lados', () => {
    const [gol, ld, zag1, zag2, le] = posicoesNoCampo(['GOL', 'LD', 'ZAG', 'ZAG', 'LE'])
    expect(gol.y).toBeGreaterThan(zag1.y)
    expect(ld.x).toBeGreaterThan(zag2.x)
    expect(le.x).toBeLessThan(zag1.x)
    expect(zag1.x).not.toBe(zag2.x)
  })

  it('lê os alvos do data-alvo', () => {
    expect(lerAlvo('titular:3')).toEqual({ tipo: 'titular', indice: 3 })
    expect(lerAlvo('fora')).toEqual({ tipo: 'fora', id: undefined })
    expect(lerAlvo('outra')).toBeUndefined()
  })
})
