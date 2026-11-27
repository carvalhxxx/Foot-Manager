import { type ReactNode, useCallback, useEffect, useState } from 'react'
import { Escudo } from './componentes/Escudo'
import type { DefinicaoTatica, FimDeTemporada as FimDto, Resposta, ResumoMundo, Rodada } from './nucleo/contrato'
import { descreverErros } from './nucleo/iniciar'
import type { Nucleo } from './nucleo/nucleo'
import { AoVivo } from './telas/AoVivo'
import { Classificacao } from './telas/Classificacao'
import { Elenco } from './telas/Elenco'
import { FimDeTemporada } from './telas/FimDeTemporada'
import { Inicio } from './telas/Inicio'
import { Partida } from './telas/Partida'
import { Tatica } from './telas/Tatica'
import { textos } from './textos'

type Tela = 'inicio' | 'elenco' | 'tatica' | 'partida' | 'classificacao' | 'aoVivo' | 'fim'

const MENU: { tela: Tela; rotulo: string }[] = [
  { tela: 'inicio', rotulo: textos.menu.inicio },
  { tela: 'elenco', rotulo: textos.menu.elenco },
  { tela: 'tatica', rotulo: textos.menu.tatica },
  { tela: 'partida', rotulo: textos.menu.partida },
  { tela: 'classificacao', rotulo: textos.menu.classificacao },
]

interface Props {
  nucleo: Nucleo
  mundo: ResumoMundo
  botaoTema: ReactNode
}

/** Lança um erro com as mensagens do núcleo (só para respostas que nunca deveriam falhar). */
function dados<T>(r: Resposta<T>): T {
  if (!r.ok) {
    throw new Error(descreverErros(r.erros).join('\n'))
  }
  return r.dados
}

function elementoEditavel(alvo: EventTarget | null): boolean {
  return alvo instanceof HTMLElement && ['INPUT', 'SELECT', 'TEXTAREA', 'BUTTON'].includes(alvo.tagName)
}

export function JogoEmAndamento({ nucleo, mundo, botaoTema }: Props) {
  const [tela, setTela] = useState<Tela>('inicio')
  // O estado mora no núcleo C#: cada mudança só pede uma nova renderização, e as telas releem os dados.
  const [, setVersao] = useState(0)
  const [simulando, setSimulando] = useState(false)
  const [rodada, setRodada] = useState<Rodada | undefined>()
  const [fim, setFim] = useState<FimDto | undefined>()
  const [errosTatica, setErrosTatica] = useState<string[]>([])
  const [divisaoVista, setDivisaoVista] = useState<string | undefined>()

  const estado = dados(nucleo.estado())
  const divisoes = mundo.divisoes.map((d) => ({ id: d.id, nome: d.nome }))

  const atualizar = () => setVersao((v) => v + 1)

  /** Ação principal: jogar a rodada ao vivo (ou voltar a ela), ou encerrar a temporada. */
  const acaoPrincipal = useCallback(() => {
    if (simulando) {
      return
    }

    if (estado.temporadaEncerrada) {
      setFim(dados(nucleo.encerrarTemporada()))
      setTela('fim')
      setVersao((v) => v + 1)
      return
    }

    dados(nucleo.iniciarAoVivo())
    setTela('aoVivo')
    setVersao((v) => v + 1)
  }, [estado.temporadaEncerrada, nucleo, simulando])

  const simularRapido = () => {
    // Deixa a tela mostrar "Simulando…" antes de o WebAssembly ocupar a thread.
    setSimulando(true)
    setTimeout(() => {
      const resultado = dados(nucleo.avancarRodada())
      setRodada(resultado)
      setSimulando(false)
      setTela('partida')
      setVersao((v) => v + 1)
    }, 0)
  }

  const concluirAoVivo = () => {
    setRodada(dados(nucleo.concluirAoVivo()))
    setTela('partida')
    atualizar()
  }

  // Atalho: Espaço joga a rodada (a tela ao vivo usa o Espaço para pausar).
  useEffect(() => {
    const aoTeclar = (e: KeyboardEvent) => {
      if (e.code === 'Space' && !elementoEditavel(e.target) && tela !== 'fim' && tela !== 'aoVivo') {
        e.preventDefault()
        acaoPrincipal()
      }
    }
    window.addEventListener('keydown', aoTeclar)
    return () => window.removeEventListener('keydown', aoTeclar)
  }, [acaoPrincipal, tela])

  const mudarTatica = (definicao: DefinicaoTatica) => {
    const r = nucleo.definirTatica(definicao)
    setErrosTatica(r.ok ? [] : descreverErros(r.erros))
    atualizar()
  }

  const usarAutomatica = () => {
    dados(nucleo.escalacaoAutomatica())
    setErrosTatica([])
    atualizar()
  }

  const conteudo = () => {
    switch (tela) {
      case 'inicio':
        return <Inicio estado={estado} />
      case 'elenco':
        return <Elenco jogadores={dados(nucleo.elenco())} />
      case 'tatica':
        return (
          <Tatica
            tatica={dados(nucleo.tatica())}
            jogadores={dados(nucleo.elenco())}
            erros={errosTatica}
            aoMudar={mudarTatica}
            aoUsarAutomatica={usarAutomatica}
          />
        )
      case 'partida':
        return <Partida rodada={rodada ?? dados(nucleo.ultimaRodada())} meuClubeId={estado.clube.id} />
      case 'classificacao': {
        const selecionada = divisaoVista ?? estado.divisaoId
        return (
          <Classificacao
            divisoes={divisoes}
            selecionada={selecionada}
            aoSelecionar={setDivisaoVista}
            classificacao={dados(nucleo.classificacao(selecionada))}
            meuClubeId={estado.clube.id}
          />
        )
      }
      case 'aoVivo': {
        const tatica = dados(nucleo.tatica())
        return (
          <AoVivo
            inicial={dados(nucleo.aoVivo())}
            formacoes={tatica.formacoes.map((f) => f.nome)}
            mentalidades={tatica.mentalidades}
            avancar={(minutos) => nucleo.avancarAoVivo(minutos)}
            decidir={(decisao) => nucleo.decidirAoVivo(decisao)}
            aoConcluir={concluirAoVivo}
          />
        )
      }
      case 'fim':
        return fim ? (
          <FimDeTemporada
            fim={fim}
            aoContinuar={() => {
              setFim(undefined)
              setRodada(undefined)
              setTela('inicio')
              atualizar()
            }}
          />
        ) : null
    }
  }

  const rotuloPrincipal = simulando
    ? textos.simulando
    : estado.temporadaEncerrada
      ? textos.avancar.encerrarTemporada
      : estado.rodadaAoVivo
        ? textos.avancar.voltarAoJogo
        : textos.avancar.rodada

  return (
    <div className="casca">
      <aside className="menu">
        <div className="marca">
          <Escudo corPrimaria={estado.clube.corPrimaria} corSecundaria={estado.clube.corSecundaria} />
          <span>{estado.clube.nome}</span>
        </div>
        <nav>
          {MENU.map((item) => (
            <button
              key={item.tela}
              type="button"
              className={`menu-item ${tela === item.tela ? 'ativo' : ''}`}
              aria-current={tela === item.tela ? 'page' : undefined}
              onClick={() => setTela(item.tela)}
            >
              {item.rotulo}
            </button>
          ))}
          {estado.rodadaAoVivo && (
            <button
              type="button"
              className={`menu-item menu-ao-vivo ${tela === 'aoVivo' ? 'ativo' : ''}`}
              aria-current={tela === 'aoVivo' ? 'page' : undefined}
              onClick={() => setTela('aoVivo')}
            >
              ● {textos.aoVivo.menu}
            </button>
          )}
        </nav>
        {botaoTema}
      </aside>

      <main className="conteudo">
        {tela !== 'fim' && tela !== 'aoVivo' && (
          <div className="barra-avancar">
            <span className="secundario">
              {textos.inicio.temporada(estado.ano)} · {textos.inicio.rodada(estado.rodadaAtual, estado.totalRodadas)}
            </span>
            <div className="acoes-rodada">
              {!estado.temporadaEncerrada && !estado.rodadaAoVivo && (
                <button type="button" className="botao botao-secundario" onClick={simularRapido} disabled={simulando}>
                  {textos.avancar.simularRapido}
                </button>
              )}
              <button type="button" className="botao botao-grande" onClick={acaoPrincipal} disabled={simulando}>
                {rotuloPrincipal}
                {!simulando && <kbd>{textos.avancar.atalho}</kbd>}
              </button>
            </div>
          </div>
        )}
        {conteudo()}
      </main>
    </div>
  )
}
