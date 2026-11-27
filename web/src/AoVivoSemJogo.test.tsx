import { render, screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import type { AoVivo as AoVivoDto } from './nucleo/contrato'
import { AoVivo } from './telas/AoVivo'
import { textos } from './textos'

// Rodada do playoff da Série B vista por um clube da Série A: ele não joga, mas a rodada acontece.
const semMeuJogo: AoVivoDto = {
  rodada: 39,
  rodadaEncerrada: false,
  placares: [
    {
      divisaoId: 'serie-b',
      divisaoNome: 'Série B',
      jogos: [
        { mandanteId: 'jan', mandanteSigla: 'JAN', golsMandante: 1, visitanteId: 'dun', visitanteSigla: 'DUN', golsVisitante: 0, minuto: 12, acrescimo: 0, encerrado: false },
      ],
    },
  ],
}

describe('Ao vivo sem jogo do meu clube', () => {
  it('mostra os outros jogos e os controles em vez de uma tela vazia', () => {
    render(
      <AoVivo
        inicial={semMeuJogo}
        formacoes={['4-4-2']}
        mentalidades={['Equilibrada']}
        marcacoes={['Normal']}
        concentracoes={['Variado']}
        avancar={vi.fn(() => ({ ok: true as const, dados: semMeuJogo }))}
        decidir={vi.fn(() => ({ ok: true as const, dados: semMeuJogo }))}
        aoConcluir={vi.fn()}
      />,
    )

    expect(screen.getByText(textos.aoVivo.semJogoMeu)).toBeInTheDocument()
    expect(screen.getByText('Série B')).toBeInTheDocument()
    expect(screen.getByText('JAN')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: textos.aoVivo.pularParaOFim })).toBeInTheDocument()
  })
})
