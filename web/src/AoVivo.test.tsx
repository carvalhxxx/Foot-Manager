import { act, fireEvent, render, screen, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import App from './App'
import type { ResultadoInicio } from './nucleo/iniciar'
import { criarNucleoFalso, mundo } from './testes/nucleoFalso'
import { textos } from './textos'

const t = textos.aoVivo

/** Entra no jogo e abre a rodada ao vivo (com relógio falso, para controlar os minutos). */
async function abrirAoVivo() {
  const nucleo = criarNucleoFalso()
  const resultado: ResultadoInicio = { ok: true, nucleo, mundo }
  render(<App iniciar={() => Promise.resolve(resultado)} gerarSeed={() => '1'} />)
  await act(async () => {})
  fireEvent.click(screen.getByRole('button', { name: textos.escolher.comecar('Pampa Sport Club') }))
  fireEvent.click(screen.getByRole('button', { name: new RegExp(textos.avancar.rodada) }))
  return nucleo
}

/** Passa N minutos de jogo na velocidade normal (700 ms por minuto). */
function passarMinutos(n: number) {
  act(() => {
    vi.advanceTimersByTime(700 * n)
  })
}

describe('Rodada ao vivo', () => {
  beforeEach(() => {
    vi.useFakeTimers()
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it('o relógio anda sozinho e a narração conta o gol', async () => {
    const nucleo = await abrirAoVivo()

    passarMinutos(25)

    expect(screen.getByTestId('relogio')).toHaveTextContent("25'")
    expect(nucleo.avancarAoVivo).toHaveBeenCalledTimes(25)
    const narracao = screen.getByRole('heading', { name: t.narracao }).parentElement!
    expect(within(narracao).getByText(/Craque Silva/)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: textos.menu.inicio })).toBeInTheDocument()
  })

  it('pausa sozinho no intervalo, avisa e continua quando o jogador quiser', async () => {
    const nucleo = await abrirAoVivo()

    passarMinutos(60)

    expect(screen.getByTestId('relogio')).toHaveTextContent(t.intervalo)
    expect(screen.getByRole('status')).toHaveTextContent(t.pausas.intervalo)
    expect(nucleo.avancarAoVivo).toHaveBeenCalledTimes(45)

    fireEvent.click(screen.getByRole('button', { name: new RegExp(t.continuar) }))
    passarMinutos(2)
    expect(screen.getByTestId('relogio')).toHaveTextContent("47'")
  })

  it('a barra de espaço pausa e continua', async () => {
    const nucleo = await abrirAoVivo()
    passarMinutos(3)

    fireEvent.keyDown(window, { code: 'Space' })
    passarMinutos(10)
    expect(nucleo.avancarAoVivo).toHaveBeenCalledTimes(3)

    fireEvent.keyDown(window, { code: 'Space' })
    passarMinutos(2)
    expect(nucleo.avancarAoVivo).toHaveBeenCalledTimes(5)
  })

  it('substituição, mentalidade e formação vão para o núcleo', async () => {
    const nucleo = await abrirAoVivo()
    fireEvent.click(screen.getByRole('button', { name: new RegExp(t.pausar) }))

    // Nada vem marcado: o botão só libera depois de escolher quem sai e quem entra.
    expect(screen.getByRole('button', { name: t.substituir })).toBeDisabled()
    fireEvent.change(screen.getByLabelText(t.sai), { target: { value: '2' } })
    expect(screen.getByRole('button', { name: t.substituir })).toBeDisabled()
    fireEvent.change(screen.getByLabelText(t.entra), { target: { value: '20' } })
    fireEvent.click(screen.getByRole('button', { name: t.substituir }))
    fireEvent.click(screen.getByRole('button', { name: textos.tatica.mentalidades.MuitoOfensiva }))
    fireEvent.change(screen.getByLabelText(textos.tatica.formacao), { target: { value: '4-3-3' } })

    expect(nucleo.decidirAoVivo).toHaveBeenCalledWith({ tipo: 'substituicao', saiId: 2, entraId: 20 })
    expect(nucleo.decidirAoVivo).toHaveBeenCalledWith({ tipo: 'mentalidade', mentalidade: 'MuitoOfensiva' })
    expect(nucleo.decidirAoVivo).toHaveBeenCalledWith({ tipo: 'formacao', formacao: '4-3-3' })
    expect(screen.getByText(t.restantes(4))).toBeInTheDocument()
    expect(screen.getByRole('button', { name: textos.tatica.mentalidades.MuitoOfensiva })).toHaveAttribute('aria-pressed', 'true')

    fireEvent.click(screen.getByRole('button', { name: textos.tatica.marcacoes.Forte }))
    expect(nucleo.decidirAoVivo).toHaveBeenLastCalledWith({ tipo: 'estilo', marcacao: 'Forte', concentracao: 'Variado' })
    fireEvent.click(screen.getByRole('button', { name: textos.tatica.concentracoes.Direita }))
    expect(nucleo.decidirAoVivo).toHaveBeenLastCalledWith({ tipo: 'estilo', marcacao: 'Forte', concentracao: 'Direita' })
    expect(screen.getByRole('button', { name: textos.tatica.concentracoes.Direita })).toHaveAttribute('aria-pressed', 'true')
  })

  it('pular para o fim, ver o resumo e voltar à tela da partida', async () => {
    const nucleo = await abrirAoVivo()

    fireEvent.click(screen.getByRole('button', { name: t.pularParaOFim }))
    expect(screen.getByTestId('relogio')).toHaveTextContent(t.encerrado)

    fireEvent.click(screen.getByRole('button', { name: t.verResumo }))
    expect(nucleo.concluirAoVivo).toHaveBeenCalledTimes(1)
    expect(screen.getByText(textos.partida.outrosJogos)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: textos.avancar.simularRapido })).toBeInTheDocument()
  })

  it('sair da tela no meio do jogo deixa voltar pelo menu "Ao vivo"', async () => {
    await abrirAoVivo()
    passarMinutos(5)

    fireEvent.click(screen.getByRole('button', { name: textos.menu.elenco }))
    expect(screen.getByRole('button', { name: new RegExp(textos.avancar.voltarAoJogo) })).toBeInTheDocument()

    fireEvent.click(screen.getByRole('button', { name: new RegExp(t.menu) }))
    expect(screen.getByTestId('relogio')).toHaveTextContent("5'")
  })
})
