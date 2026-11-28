import { act, fireEvent, render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import App from './App'
import type { ResultadoInicio } from './nucleo/iniciar'
import { criarNucleoFalso, mundo } from './testes/nucleoFalso'
import { textos } from './textos'

function iniciarCom(nucleo = criarNucleoFalso()) {
  const resultado: ResultadoInicio = { ok: true, nucleo, mundo }
  return { nucleo, iniciar: () => Promise.resolve(resultado) }
}

async function entrarNoJogo(nucleo = criarNucleoFalso()) {
  const { iniciar } = iniciarCom(nucleo)
  render(<App iniciar={iniciar} gerarSeed={() => '123'} />)
  await userEvent.click(await screen.findByRole('button', { name: textos.escolher.comecar('Pampa Sport Club') }))
  return nucleo
}

/** Simula uma rodada no modo rápido e espera o setTimeout que libera a tela antes de simular. */
async function avancar() {
  vi.useFakeTimers({ shouldAdvanceTime: true })
  await userEvent.click(screen.getByRole('button', { name: textos.avancar.simularRapido }))
  await act(() => vi.runAllTimersAsync())
  vi.useRealTimers()
}

describe('App', () => {
  it('carrega, mostra os clubes e começa a carreira no clube escolhido', async () => {
    const nucleo = await entrarNoJogo()

    expect(nucleo.novaCarreira).toHaveBeenCalledWith('pampa', '123')
    // A tela inicial é o elenco; a coluna ao lado mostra o próximo jogo.
    expect(screen.getByRole('heading', { name: textos.elenco.titulo })).toBeInTheDocument()
    expect(screen.getByText('Craque Silva')).toBeInTheDocument()
    expect(screen.getByRole('region', { name: textos.inicio.proximoJogo })).toHaveTextContent('Cerrado FC')
    expect(screen.getByText(/Pampa Sport Club/)).toBeInTheDocument()
    expect(screen.getByText('Cerrado FC')).toBeInTheDocument()
    expect(screen.getByRole('note')).toHaveTextContent('Machucado Souza (lesionado)')
  })

  it('lista os erros de definições com arquivo e local', async () => {
    const falha: ResultadoInicio = {
      ok: false,
      erros: [{ arquivo: 'clubes/pampa.json', local: 'elenco[1]', mensagem: 'idade deve estar entre 15 e 45.' }],
    }

    render(<App iniciar={() => Promise.resolve(falha)} />)

    expect(await screen.findByRole('alert')).toHaveTextContent('clubes/pampa.json → elenco[1]: idade deve estar entre 15 e 45.')
  })

  it('simular rápido mostra a partida com placar, lances e outros jogos', async () => {
    const nucleo = await entrarNoJogo()

    await avancar()

    expect(nucleo.avancarRodada).toHaveBeenCalledTimes(1)
    expect(screen.getByText(/GOL do Pampa Sport Club! Craque Silva/)).toBeInTheDocument()
    expect(screen.getByText("45+2'")).toBeInTheDocument()
    expect(screen.queryByText(/finaliza para fora/)).not.toBeInTheDocument()
    expect(screen.getByText(textos.partida.outrosJogos)).toBeInTheDocument()

    await userEvent.click(screen.getByLabelText(textos.partida.todosOsLances))
    expect(screen.getByText(/Fulano finaliza para fora/)).toBeInTheDocument()
  })

  it('a partida mostra as transferências da IA na rodada', async () => {
    await entrarNoJogo()
    await avancar()

    expect(screen.getByText(textos.partida.mercado)).toBeInTheDocument()
    expect(screen.getByText(textos.partida.transferencia('Vendido Alves', 'CER', 'PAM'))).toBeInTheDocument()
    expect(screen.getByText(textos.partida.transferencia('Livre Nunes', textos.partida.agenteLivre, 'CER'))).toBeInTheDocument()
  })

  it('a escolha de clube mostra as dicas de como jogar', async () => {
    const { iniciar } = iniciarCom()
    render(<App iniciar={iniciar} gerarSeed={() => '123'} />)
    const dicas = await screen.findByRole('region', { name: textos.escolher.comoJogar })
    expect(within(dicas).getAllByRole('listitem')).toHaveLength(textos.escolher.dicas.length)
  })

  it('avisa na reta final quem está no último ano de contrato', async () => {
    await entrarNoJogo(criarNucleoFalso({ totalRodadas: 5 }))
    expect(screen.getByText(textos.inicio.contratosAcabando(2))).toBeInTheDocument()
  })

  it('a barra de espaço abre a rodada ao vivo', async () => {
    const nucleo = await entrarNoJogo()

    await userEvent.keyboard(' ')

    expect(nucleo.iniciarAoVivo).toHaveBeenCalledTimes(1)
    expect(screen.getByTestId('relogio')).toHaveTextContent("0'")
  })

  it('elenco mostra destaque e jogador lesionado', async () => {
    await entrarNoJogo()

    await userEvent.click(screen.getByRole('button', { name: textos.menu.elenco }))

    expect(screen.getByLabelText(textos.elenco.estrela)).toBeInTheDocument()
    expect(screen.getByText(textos.elenco.lesionado(3))).toBeInTheDocument()
  })

  it('tática: trocar a mentalidade envia ao núcleo', async () => {
    const nucleo = await entrarNoJogo()
    await userEvent.click(screen.getByRole('button', { name: textos.menu.tatica }))

    await userEvent.click(screen.getByRole('button', { name: textos.tatica.mentalidades.Ofensiva }))

    expect(nucleo.definirTatica).toHaveBeenLastCalledWith(expect.objectContaining({ mentalidade: 'Ofensiva' }))
    expect(screen.getByRole('button', { name: textos.tatica.mentalidades.Ofensiva })).toHaveAttribute('aria-pressed', 'true')
  })

  it('tática: tocar num titular e depois em outro jogador troca os dois', async () => {
    const nucleo = await entrarNoJogo()
    await userEvent.click(screen.getByRole('button', { name: textos.menu.tatica }))
    const t = textos.tatica

    const titular = screen.getByRole('button', { name: new RegExp(`^${'Titular 1'}, titular`) })
    await userEvent.click(titular)
    expect(titular).toHaveAttribute('aria-pressed', 'true')
    expect(screen.getByText(t.escolhaDestino)).toBeInTheDocument()

    const craque = screen.queryByRole('button', { name: t.foraDoTimeJogador('Craque Silva') }) ?? screen.getByRole('button', { name: t.noBanco('Craque Silva') })
    await userEvent.click(craque)

    const enviada = nucleo.definirTatica.mock.lastCall?.[0]
    expect(enviada?.titulares).toContain(20)
    expect(enviada?.titulares).not.toContain(2)
  })

  it('tática: depois de soltar o arrasto, o nome não fica flutuando', async () => {
    await entrarNoJogo()
    await userEvent.click(screen.getByRole('button', { name: textos.menu.tatica }))
    const peca = screen.getByRole('button', { name: /^Titular 1, titular/ })

    fireEvent.pointerDown(peca, { clientX: 10, clientY: 10 })
    fireEvent.pointerMove(window, { clientX: 60, clientY: 60 })
    expect(document.querySelector('.peca-arrastada')).not.toBeNull()

    fireEvent.pointerUp(window, { clientX: 60, clientY: 60 })
    fireEvent.pointerMove(window, { clientX: 65, clientY: 65 })
    expect(document.querySelector('.peca-arrastada')).toBeNull()
  })

  it('tática: marcação, concentração, capitão e cobrador vão para o núcleo', async () => {
    const nucleo = await entrarNoJogo()
    await userEvent.click(screen.getByRole('button', { name: textos.menu.tatica }))
    const t = textos.tatica

    await userEvent.click(screen.getByRole('button', { name: t.marcacoes.Forte }))
    expect(nucleo.definirTatica).toHaveBeenLastCalledWith(expect.objectContaining({ marcacao: 'Forte' }))
    await userEvent.click(screen.getByRole('button', { name: t.concentracoes.Esquerda }))
    expect(nucleo.definirTatica).toHaveBeenLastCalledWith(expect.objectContaining({ marcacao: 'Forte', concentracao: 'Esquerda' }))

    expect(screen.getByText(t.semCobrador)).toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: /^Titular 3, titular/ }))
    await userEvent.click(screen.getByRole('button', { name: t.fazerCobrador('Titular 3') }))
    expect(nucleo.definirTatica).toHaveBeenLastCalledWith(expect.objectContaining({ cobradorId: 4 }))
    expect(screen.getByTitle(t.seloCobrador)).toBeInTheDocument()

    await userEvent.click(screen.getByRole('button', { name: /^Titular 5, titular/ }))
    await userEvent.click(screen.getByRole('button', { name: t.fazerCapitao('Titular 5') }))
    expect(nucleo.definirTatica).toHaveBeenLastCalledWith(expect.objectContaining({ capitaoId: 6, cobradorId: 4 }))

    await userEvent.click(screen.getByRole('button', { name: t.tirarCobrador }))
    expect(nucleo.definirTatica).toHaveBeenLastCalledWith(expect.objectContaining({ cobradorId: undefined, capitaoId: 6 }))
  })

  it('tática: erro do núcleo aparece na tela', async () => {
    const nucleo = await entrarNoJogo()
    nucleo.definirTatica.mockReturnValueOnce({ ok: false, erros: [{ arquivo: '', local: '', mensagem: 'Escale exatamente 11 titulares.' }] })
    await userEvent.click(screen.getByRole('button', { name: textos.menu.tatica }))

    await userEvent.selectOptions(screen.getByLabelText(textos.tatica.formacao), '4-3-3')

    expect(screen.getByRole('alert')).toHaveTextContent('Escale exatamente 11 titulares.')
  })

  it('classificação da Série A mostra as vagas continentais e os critérios de desempate', async () => {
    await entrarNoJogo()
    await userEvent.click(screen.getByRole('button', { name: textos.menu.classificacao }))

    const regras = screen.getByRole('region', { name: textos.classificacao.regrasELegenda })
    const t = textos.classificacao
    expect(within(regras).getAllByRole('listitem').map((li) => li.textContent)).toEqual([
      `${t.zonas.Libertadores} (1º ao 4º)`,
      `${t.zonas.LibertadoresQualificacao} (5º)`,
      `${t.zonas.SulAmericana} (6º ao 11º)`,
      `${t.zonas.Rebaixamento} (17º ao 20º)`,
      t.criterios.Vitorias, t.criterios.Saldo, t.criterios.GolsPro, t.criterios.ConfrontoDireto,
      t.criterios.Vermelhos, t.criterios.Amarelos, t.criterios.Sorteio,
    ])
    expect(document.querySelector('tbody tr')).toHaveClass('zona-Libertadores')
  })

  it('classificação da Série B mostra o playoff de acesso', async () => {
    await entrarNoJogo()
    await userEvent.click(screen.getByRole('button', { name: textos.menu.classificacao }))
    await userEvent.click(screen.getByRole('tab', { name: 'Série B' }))

    const playoff = screen.getByRole('region', { name: textos.classificacao.playoff })
    expect(playoff).toHaveTextContent('3º Dunas FC × 6º Jangadeiro EC')
    expect(playoff).toHaveTextContent(textos.classificacao.classificado('Dunas FC'))
    const regras = screen.getByRole('region', { name: textos.classificacao.regrasELegenda })
    expect(regras).toHaveTextContent(`${textos.classificacao.zonas.Playoff} (3º ao 6º)`)
    expect(regras).toHaveTextContent(textos.classificacao.criterios.Saldo)
  })

  it('Copa do Brasil: aba com o chaveamento e próximo jogo no painel', async () => {
    const nucleo = await entrarNoJogo()
    const painel = screen.getByRole('region', { name: textos.copa.titulo })
    expect(painel).toHaveTextContent('Dunas FC')
    expect(painel).toHaveTextContent(textos.copa.proximo(textos.copa.fases.SegundaFase, 1, 7))

    await userEvent.click(screen.getByRole('button', { name: textos.menu.classificacao }))
    await userEvent.click(screen.getByRole('tab', { name: textos.copa.aba }))
    expect(nucleo.copa).toHaveBeenCalled()
    expect(screen.getByRole('tab', { name: textos.copa.fases.SegundaFase })).toHaveAttribute('aria-selected', 'true')

    await userEvent.click(screen.getByRole('tab', { name: textos.copa.fases.PrimeiraFase }))
    expect(screen.getByText(/pên\. 3 × 4/)).toBeInTheDocument()
  })

  it('classificação troca de divisão e destaca o clube do jogador', async () => {
    const nucleo = await entrarNoJogo()
    await userEvent.click(screen.getByRole('button', { name: textos.menu.classificacao }))

    expect(screen.getByRole('row', { name: /Pampa Sport Club/ })).toHaveClass('meu-clube')

    await userEvent.click(screen.getByRole('tab', { name: 'Série B' }))
    expect(nucleo.classificacao).toHaveBeenLastCalledWith('serie-b')
    expect(screen.getByRole('row', { name: /Dunas FC/ })).toBeInTheDocument()
  })

  it('fim de temporada: encerra, mostra o resumo e começa o ano seguinte', async () => {
    const nucleo = await entrarNoJogo(criarNucleoFalso({ totalRodadas: 1 }))
    await avancar()

    await userEvent.click(screen.getByRole('button', { name: new RegExp(textos.avancar.encerrarTemporada) }))

    expect(nucleo.encerrarTemporada).toHaveBeenCalled()
    expect(screen.getByRole('heading', { name: textos.fim.titulo(2027) })).toBeInTheDocument()
    expect(screen.getByText(textos.fim.conquistouTitulo)).toBeInTheDocument()

    await userEvent.click(screen.getByRole('button', { name: textos.fim.comecar(2028) }))
    expect(screen.getByText(textos.inicio.proximoJogo)).toBeInTheDocument()
  })

  it('alterna o tema e lembra a escolha', async () => {
    await entrarNoJogo()
    const inicial = document.documentElement.dataset.tema

    await userEvent.click(screen.getByRole('button', { name: /Tema/ }))

    const novo = document.documentElement.dataset.tema
    expect(novo).not.toBe(inicial)
    expect(localStorage.getItem('foot-manager:tema')).toBe(novo)
    expect(within(document.body).getByRole('navigation')).toBeInTheDocument()
  })
})
