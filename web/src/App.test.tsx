import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import App from './App'
import type { ResultadoInicio } from './nucleo/iniciar'
import { textos } from './textos'

const mundo: ResultadoInicio = {
  ok: true,
  versao: '0.1.0+abc123',
  mundo: {
    nome: 'Base',
    temporada: 2027,
    totalJogadores: 1103,
    divisoes: [
      {
        id: 'serie-a',
        nome: 'Série A',
        clubes: [
          {
            id: 'pampa',
            nome: 'Pampa Sport Club',
            sigla: 'PAM',
            cidade: 'Porto Alegre',
            uf: 'RS',
            corPrimaria: '#C8102E',
            corSecundaria: '#FFFFFF',
            forcaTop11: 75.4,
            jogadores: 28,
            torcida: 88,
          },
        ],
      },
    ],
  },
}

describe('App', () => {
  it('mostra carregando e depois o mundo vindo do núcleo', async () => {
    render(<App iniciar={() => Promise.resolve(mundo)} />)

    expect(screen.getByRole('status')).toHaveTextContent(textos.carregando)
    expect(await screen.findByRole('heading', { name: textos.inicio.titulo })).toBeInTheDocument()
    expect(screen.getByText('Pampa Sport Club')).toBeInTheDocument()
    expect(screen.getByText('Porto Alegre/RS')).toBeInTheDocument()
    expect(screen.getByText('75,4')).toBeInTheDocument()
    expect(screen.getByText('1.103')).toBeInTheDocument()
    expect(screen.getByText('0.1.0')).toBeInTheDocument()
  })

  it('lista os erros de definições com arquivo e local', async () => {
    const falha: ResultadoInicio = {
      ok: false,
      erros: [{ arquivo: 'clubes/pampa.json', local: 'elenco[1]', mensagem: 'idade deve estar entre 15 e 45.' }],
    }

    render(<App iniciar={() => Promise.resolve(falha)} />)

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'clubes/pampa.json → elenco[1]: idade deve estar entre 15 e 45.',
    )
  })

  it('permite tentar de novo depois de uma falha', async () => {
    const iniciar = vi.fn<() => Promise<ResultadoInicio>>()
    iniciar.mockRejectedValueOnce(new Error('rede caiu')).mockResolvedValueOnce(mundo)

    render(<App iniciar={iniciar} />)
    await userEvent.click(await screen.findByRole('button', { name: textos.erro.tentarDeNovo }))

    expect(await screen.findByText('Pampa Sport Club')).toBeInTheDocument()
    expect(iniciar).toHaveBeenCalledTimes(2)
  })

  it('alterna o tema e lembra a escolha', async () => {
    render(<App iniciar={() => Promise.resolve(mundo)} />)
    const inicial = document.documentElement.dataset.tema

    await userEvent.click(screen.getByRole('button', { name: /Tema/ }))

    const novo = document.documentElement.dataset.tema
    expect(novo).not.toBe(inicial)
    expect(localStorage.getItem('foot-manager:tema')).toBe(novo)
  })
})
