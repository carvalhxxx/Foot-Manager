import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import App from './App'
import type { ResultadoInicio } from './nucleo/iniciar'
import { armazenamentoEmMemoria } from './saves/armazenamento'
import { criarNucleoFalso, mundo } from './testes/nucleoFalso'
import { textos } from './textos'

const t = textos.mercado

async function comecar() {
  const nucleo = criarNucleoFalso()
  const resultado: ResultadoInicio = { ok: true, nucleo, mundo }
  render(<App iniciar={() => Promise.resolve(resultado)} gerarSeed={() => '1'} armazenamento={armazenamentoEmMemoria()} />)
  await userEvent.click(await screen.findByRole('button', { name: textos.escolher.comecar('Pampa Sport Club') }))
  return nucleo
}

describe('Mercado', () => {
  it('avisa no início que a janela está aberta', async () => {
    await comecar()
    expect(screen.getByText(textos.inicio.janelaAberta)).toBeInTheDocument()
  })

  it('filtra, mostra a proposta e compra com uma confirmação', async () => {
    const nucleo = await comecar()
    await userEvent.click(screen.getByRole('button', { name: textos.menu.mercado }))
    expect(screen.getByText(/Janela aberta/)).toBeInTheDocument()

    await userEvent.click(screen.getByLabelText(t.apenasLivres))
    await userEvent.click(screen.getByRole('button', { name: t.buscar }))
    expect(screen.queryByText('Reforço Lima')).not.toBeInTheDocument()
    await userEvent.click(screen.getByLabelText(t.apenasLivres))
    await userEvent.click(screen.getByRole('button', { name: t.buscar }))

    await userEvent.click(screen.getByRole('button', { name: t.negociar('Reforço Lima') }))
    expect(screen.getByText(/Comprar Reforço Lima do Cerrado FC/)).toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: t.confirmar }))

    expect(nucleo.comprar).toHaveBeenCalledWith(500)
    expect(screen.getByText(t.contratado('Reforço Lima'))).toBeInTheDocument()
  })

  it('não deixa confirmar quando há impedimento', async () => {
    await comecar()
    await userEvent.click(screen.getByRole('button', { name: textos.menu.mercado }))
    await userEvent.click(screen.getByRole('button', { name: t.negociar('Caro Demais') }))

    expect(screen.getByRole('button', { name: t.confirmar })).toBeDisabled()
    expect(screen.getByText(/Não há dinheiro/)).toBeInTheDocument()
  })

  it('vende pelo Elenco depois de ver a proposta', async () => {
    const nucleo = await comecar()
    await userEvent.click(screen.getByRole('button', { name: textos.menu.elenco }))
    await userEvent.click(screen.getByRole('button', { name: textos.elenco.detalhes('Titular 1') }))
    await userEvent.click(screen.getByRole('button', { name: textos.elenco.verPropostas }))
    expect(screen.getByText(/O Cerrado FC oferece/)).toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: /Vender por/ }))

    expect(nucleo.vender).toHaveBeenCalledWith(2)
  })
})
