import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import App from './App'
import type { ResultadoInicio } from './nucleo/iniciar'
import { armazenamentoEmMemoria } from './saves/armazenamento'
import { criarNucleoFalso, mundo } from './testes/nucleoFalso'
import { textos } from './textos'

const t = textos.financas

async function abrirFinancas() {
  const nucleo = criarNucleoFalso()
  const resultado: ResultadoInicio = { ok: true, nucleo, mundo }
  render(<App iniciar={() => Promise.resolve(resultado)} gerarSeed={() => '1'} armazenamento={armazenamentoEmMemoria()} />)
  await userEvent.click(await screen.findByRole('button', { name: textos.escolher.comecar('Pampa Sport Club') }))
  await userEvent.click(screen.getByRole('button', { name: textos.menu.financas }))
  return nucleo
}

describe('Finanças', () => {
  it('mostra caixa, folha, extrato e o último jogo em casa', async () => {
    await abrirFinancas()

    expect(screen.getByRole('heading', { name: t.titulo })).toBeInTheDocument()
    expect(screen.getByText(t.categorias.Bilheteria)).toBeInTheDocument()
    expect(screen.getByText(t.categorias.Salarios)).toBeInTheDocument()
    expect(screen.getByText(/31\.000 de 53\.000 lugares/)).toBeInTheDocument()
  })

  it('define e volta ao preço sugerido do ingresso', async () => {
    const nucleo = await abrirFinancas()
    const campo = screen.getByLabelText(t.ingresso)

    await userEvent.clear(campo)
    await userEvent.type(campo, '50')
    await userEvent.click(screen.getByRole('button', { name: t.aplicar }))
    expect(nucleo.definirPrecoIngresso).toHaveBeenLastCalledWith(50)

    await userEvent.click(screen.getByRole('button', { name: t.usarSugerido }))
    expect(nucleo.definirPrecoIngresso).toHaveBeenLastCalledWith(undefined)
  })

  it('preço inválido mostra o erro do núcleo', async () => {
    await abrirFinancas()
    const campo = screen.getByLabelText(t.ingresso)

    await userEvent.clear(campo)
    await userEvent.type(campo, '5000')
    await userEvent.click(screen.getByRole('button', { name: t.aplicar }))

    expect(screen.getByRole('alert')).toHaveTextContent('entre R$ 1 e R$ 1.000')
  })
})
