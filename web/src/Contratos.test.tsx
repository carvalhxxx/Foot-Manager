import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import App from './App'
import type { ResultadoInicio } from './nucleo/iniciar'
import { armazenamentoEmMemoria } from './saves/armazenamento'
import { criarNucleoFalso, mundo } from './testes/nucleoFalso'
import { textos } from './textos'

const t = textos.elenco

async function abrirElenco() {
  const nucleo = criarNucleoFalso()
  const resultado: ResultadoInicio = { ok: true, nucleo, mundo }
  render(<App iniciar={() => Promise.resolve(resultado)} gerarSeed={() => '1'} armazenamento={armazenamentoEmMemoria()} />)
  await userEvent.click(await screen.findByRole('button', { name: textos.escolher.comecar('Pampa Sport Club') }))
  await userEvent.click(screen.getByRole('button', { name: textos.menu.elenco }))
  return nucleo
}

describe('Contratos no elenco', () => {
  it('renova quem está no último ano pelo prazo escolhido', async () => {
    const nucleo = await abrirElenco()
    expect(screen.getByText(/Último ano/)).toBeInTheDocument()

    await userEvent.click(screen.getByRole('button', { name: t.detalhes('Craque Silva') }))
    expect(screen.getByText(/pede R\$\s?55\.000/)).toBeInTheDocument()
    await userEvent.selectOptions(screen.getByLabelText(t.anos), '3')
    await userEvent.click(screen.getByRole('button', { name: t.renovar }))

    expect(nucleo.renovarContrato).toHaveBeenCalledWith(20, 3)
  })

  it('pede confirmação antes de dispensar', async () => {
    const nucleo = await abrirElenco()

    await userEvent.click(screen.getByRole('button', { name: t.detalhes('Titular 1') }))
    expect(screen.queryByRole('button', { name: t.renovar })).not.toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: t.dispensar }))
    expect(nucleo.dispensar).not.toHaveBeenCalled()
    await userEvent.click(screen.getByRole('button', { name: t.confirmarDispensa('Titular 1') }))

    expect(nucleo.dispensar).toHaveBeenCalledWith(2)
  })
})
