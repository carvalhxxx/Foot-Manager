import { describe, expect, it } from 'vitest'
import { arquivosDaBase } from './definicoes'

describe('arquivosDaBase', () => {
  it('traz manifesto, divisões e os 40 clubes com caminhos relativos', () => {
    const arquivos = arquivosDaBase()

    expect(arquivos['manifesto.json']).toContain('"versaoFormato": 1')
    expect(arquivos['divisoes.json']).toContain('"serie-a"')
    expect(Object.keys(arquivos).filter((c) => c.startsWith('clubes/'))).toHaveLength(40)
    expect(arquivos['clubes/pampa.json']).toContain('Pampa Sport Club')
  })
})
