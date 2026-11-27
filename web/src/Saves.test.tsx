import { act, fireEvent, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import App from './App'
import type { ResultadoInicio } from './nucleo/iniciar'
import { armazenamentoEmMemoria, type ArmazenamentoSaves } from './saves/armazenamento'
import { criarNucleoFalso, mundo } from './testes/nucleoFalso'
import { textos } from './textos'

const t = textos.saves

function montar(armazenamento: ArmazenamentoSaves = armazenamentoEmMemoria(), nucleo = criarNucleoFalso()) {
  let id = 0
  const resultado: ResultadoInicio = { ok: true, nucleo, mundo }
  render(
    <App
      iniciar={() => Promise.resolve(resultado)}
      gerarSeed={() => '1'}
      armazenamento={armazenamento}
      gerarIdDeSave={() => `save-${++id}`}
    />,
  )
  return { nucleo, armazenamento }
}

async function simularRodada() {
  vi.useFakeTimers({ shouldAdvanceTime: true })
  await userEvent.click(screen.getByRole('button', { name: textos.avancar.simularRapido }))
  await act(() => vi.runAllTimersAsync())
  vi.useRealTimers()
}

describe('Saves', () => {
  it('sem saves, vai direto para a escolha de clube', async () => {
    montar()

    expect(await screen.findByRole('heading', { name: textos.escolher.titulo })).toBeInTheDocument()
  })

  it('salva sozinho ao começar e depois de cada rodada', async () => {
    const { armazenamento } = montar()
    await userEvent.click(await screen.findByRole('button', { name: textos.escolher.comecar('Pampa Sport Club') }))

    await waitFor(async () => expect(await armazenamento.listar()).toHaveLength(1))
    expect(screen.getByText(/Salvo/)).toBeInTheDocument()

    await simularRodada()

    await waitFor(async () => expect((await armazenamento.listar())[0].rodada).toBe(1))
    expect(JSON.parse((await armazenamento.lerConteudo('save-1'))!)).toEqual({ falso: true, rodada: 1 })
  })

  it('com saves, mostra "Continuar" e retoma a carreira escolhida', async () => {
    const armazenamento = armazenamentoEmMemoria()
    await armazenamento.guardar(
      { id: 'antigo', clubeId: 'pampa', clubeNome: 'Pampa Sport Club', corPrimaria: '#000', corSecundaria: '#fff', ano: 2027, rodada: 7, divisao: 'Série A', atualizadoEm: '2026-11-20T10:00:00.000Z' },
      JSON.stringify({ falso: true, rodada: 7 }),
    )
    const { nucleo } = montar(armazenamento)

    expect(await screen.findByText(t.resumo(2027, 'Série A', 7))).toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: `${t.continuar}: Pampa Sport Club` }))

    expect(nucleo.carregarSave).toHaveBeenCalledWith(JSON.stringify({ falso: true, rodada: 7 }))
    expect(screen.getAllByText(new RegExp(textos.inicio.rodada(7, 38))).length).toBeGreaterThan(0)
  })

  it('apagar pede confirmação', async () => {
    const armazenamento = armazenamentoEmMemoria()
    await armazenamento.guardar(
      { id: 'a', clubeId: 'pampa', clubeNome: 'Pampa Sport Club', corPrimaria: '#000', corSecundaria: '#fff', ano: 2027, rodada: 3, divisao: 'Série A', atualizadoEm: '2026-11-20T10:00:00.000Z' },
      '{}',
    )
    montar(armazenamento)

    await userEvent.click(await screen.findByRole('button', { name: t.apagar }))
    expect(await armazenamento.listar()).toHaveLength(1)

    await userEvent.click(screen.getByRole('button', { name: t.confirmarApagar }))
    await waitFor(async () => expect(await armazenamento.listar()).toHaveLength(0))
    expect(await screen.findByRole('heading', { name: textos.escolher.titulo })).toBeInTheDocument()
  })

  it('importar arquivo inválido mostra o erro e não estraga nada', async () => {
    const armazenamento = armazenamentoEmMemoria()
    await armazenamento.guardar(
      { id: 'a', clubeId: 'pampa', clubeNome: 'Pampa Sport Club', corPrimaria: '#000', corSecundaria: '#fff', ano: 2027, rodada: 3, divisao: 'Série A', atualizadoEm: '2026-11-20T10:00:00.000Z' },
      '{}',
    )
    montar(armazenamento)
    const entrada = (await screen.findByLabelText(t.importar)) as HTMLInputElement

    await userEvent.upload(entrada, new File(['lixo'], 'save.json', { type: 'application/json' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('save válido')
    expect(await armazenamento.listar()).toHaveLength(1)
  })

  it('importar arquivo válido abre a carreira e guarda como save novo', async () => {
    const armazenamento = armazenamentoEmMemoria()
    await armazenamento.guardar(
      { id: 'a', clubeId: 'pampa', clubeNome: 'Pampa Sport Club', corPrimaria: '#000', corSecundaria: '#fff', ano: 2027, rodada: 3, divisao: 'Série A', atualizadoEm: '2026-11-20T10:00:00.000Z' },
      '{}',
    )
    montar(armazenamento)
    const entrada = (await screen.findByLabelText(t.importar)) as HTMLInputElement

    await userEvent.upload(entrada, new File([JSON.stringify({ falso: true, rodada: 12 })], 'save.json'))

    expect((await screen.findAllByText(new RegExp(textos.inicio.rodada(12, 38)))).length).toBeGreaterThan(0)
    await waitFor(async () => expect(await armazenamento.listar()).toHaveLength(2))
  })

  it('exportar baixa um arquivo com o save', async () => {
    const criarUrl = vi.fn(() => 'blob:teste')
    URL.createObjectURL = criarUrl
    URL.revokeObjectURL = vi.fn()
    const clique = vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(() => {})
    montar()
    await userEvent.click(await screen.findByRole('button', { name: textos.escolher.comecar('Pampa Sport Club') }))

    fireEvent.click(screen.getByRole('button', { name: t.exportar }))

    expect(criarUrl).toHaveBeenCalledTimes(1)
    expect(clique).toHaveBeenCalledTimes(1)
    clique.mockRestore()
  })

  it('"Menu inicial" volta para a lista de carreiras', async () => {
    const { armazenamento } = montar()
    await userEvent.click(await screen.findByRole('button', { name: textos.escolher.comecar('Pampa Sport Club') }))
    await waitFor(async () => expect(await armazenamento.listar()).toHaveLength(1))

    await userEvent.click(screen.getByRole('button', { name: t.menuInicial }))

    expect(await screen.findByRole('heading', { name: t.titulo })).toBeInTheDocument()
  })
})
