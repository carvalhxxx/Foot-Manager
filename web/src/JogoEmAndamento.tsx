import { type ReactNode, useCallback, useEffect, useState } from 'react'
import { Escudo } from './componentes/Escudo'
import type { DefinicaoTatica, FimDeTemporada as FimDto, Resposta, ResumoMundo, Rodada } from './nucleo/contrato'
import { descreverErros } from './nucleo/iniciar'
import type { Nucleo } from './nucleo/nucleo'
import type { ArmazenamentoSaves } from './saves/armazenamento'
import { baixarSave, formatoHora, salvarCarreira } from './saves/salvar'
import { AoVivo } from './telas/AoVivo'
import { Classificacao } from './telas/Classificacao'
import { Elenco } from './telas/Elenco'
import { Financas } from './telas/Financas'
import { FimDeTemporada } from './telas/FimDeTemporada'
import { Inicio } from './telas/Inicio'
import { Mercado } from './telas/Mercado'
import { Partida } from './telas/Partida'
import { Tatica } from './telas/Tatica'
import { textos } from './textos'

type Tela = 'inicio' | 'elenco' | 'tatica' | 'partida' | 'classificacao' | 'mercado' | 'financas' | 'aoVivo' | 'fim'

const MENU: { tela: Tela; rotulo: string }[] = [
  { tela: 'inicio', rotulo: textos.menu.inicio },
  { tela: 'elenco', rotulo: textos.menu.elenco },
  { tela: 'tatica', rotulo: textos.menu.tatica },
  { tela: 'partida', rotulo: textos.menu.partida },
  { tela: 'classificacao', rotulo: textos.menu.classificacao },
  { tela: 'mercado', rotulo: textos.menu.mercado },
  { tela: 'financas', rotulo: textos.menu.financas },
]

interface Props {
  nucleo: Nucleo
  mundo: ResumoMundo
  botaoTema: ReactNode
  /** Onde esta carreira é salva automaticamente. */
  slotId: string
  armazenamento: ArmazenamentoSaves
  aoSairParaOMenu: () => void
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

type SituacaoSave = { tipo: 'salvo'; em: Date } | { tipo: 'erro'; mensagem: string }

export function JogoEmAndamento({ nucleo, mundo, botaoTema, slotId, armazenamento, aoSairParaOMenu }: Props) {
  const [tela, setTela] = useState<Tela>('inicio')
  // O estado mora no núcleo C#: cada mudança só pede uma nova renderização, e as telas releem os dados.
  const [, setVersao] = useState(0)
  const [simulando, setSimulando] = useState(false)
  const [rodada, setRodada] = useState<Rodada | undefined>()
  const [fim, setFim] = useState<FimDto | undefined>()
  const [errosTatica, setErrosTatica] = useState<string[]>([])
  const [errosFinancas, setErrosFinancas] = useState<string[]>([])
  const [errosElenco, setErrosElenco] = useState<string[]>([])
  const [errosMercado, setErrosMercado] = useState<string[]>([])
  const [mensagemMercado, setMensagemMercado] = useState<string>()
  const [divisaoVista, setDivisaoVista] = useState<string | undefined>()
  const [save, setSave] = useState<SituacaoSave | undefined>()

  const estado = dados(nucleo.estado())
  const divisoes = mundo.divisoes.map((d) => ({ id: d.id, nome: d.nome }))

  const atualizar = () => setVersao((v) => v + 1)

  // O estado só muda quando o salvamento termina (assíncrono), nunca durante a renderização.
  const salvar = useCallback(() => {
    const agora = new Date()
    void salvarCarreira(nucleo, armazenamento, slotId, agora).then((erro) =>
      setSave(erro ? { tipo: 'erro', mensagem: erro } : { tipo: 'salvo', em: agora }),
    )
  }, [armazenamento, nucleo, slotId])

  // Salva sozinho ao entrar e sempre que uma rodada (ou temporada) termina. No meio do jogo ao vivo não dá.
  useEffect(() => {
    if (!estado.rodadaAoVivo) {
      salvar()
    }
  }, [estado.ano, estado.rodadaAtual, estado.rodadaAoVivo, salvar])

  const exportar = () => {
    const r = nucleo.exportarSave()
    if (r.ok) {
      baixarSave(r.dados)
    } else {
      setSave({ tipo: 'erro', mensagem: descreverErros(r.erros).join(' ') })
    }
  }

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
    if (r.ok) {
      salvar()
    }
    atualizar()
  }

  const usarAutomatica = () => {
    dados(nucleo.escalacaoAutomatica())
    setErrosTatica([])
    salvar()
    atualizar()
  }

  const conteudo = () => {
    switch (tela) {
      case 'inicio':
        return <Inicio estado={estado} />
      case 'elenco':
        return (
          <Elenco
            jogadores={dados(nucleo.elenco())}
            erros={errosElenco}
            aoRenovar={(id, anos) => {
              const r = nucleo.renovarContrato(id, anos)
              setErrosElenco(r.ok ? [] : descreverErros(r.erros))
              if (r.ok) {
                salvar()
              }
              atualizar()
            }}
            aoDispensar={(id) => {
              const r = nucleo.dispensar(id)
              setErrosElenco(r.ok ? [] : descreverErros(r.erros))
              if (r.ok) {
                salvar()
              }
              atualizar()
            }}
            consultarOferta={(id) => {
              const r = nucleo.ofertaPor(id)
              setErrosElenco(r.ok ? [] : descreverErros(r.erros))
              return r.ok ? r.dados : undefined
            }}
            aoVender={(id) => {
              const r = nucleo.vender(id)
              setErrosElenco(r.ok ? [] : descreverErros(r.erros))
              if (r.ok) {
                salvar()
              }
              atualizar()
            }}
          />
        )
      case 'mercado':
        return (
          <Mercado
            buscar={(busca) => dados(nucleo.mercado(busca))}
            erros={errosMercado}
            mensagem={mensagemMercado}
            aoComprar={(jogador) => {
              const r = nucleo.comprar(jogador.id)
              setErrosMercado(r.ok ? [] : descreverErros(r.erros))
              setMensagemMercado(r.ok ? textos.mercado.contratado(jogador.nome) : undefined)
              if (r.ok) {
                salvar()
              }
              atualizar()
            }}
          />
        )
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
      case 'financas':
        return (
          <Financas
            financas={dados(nucleo.financas())}
            erros={errosFinancas}
            aoDefinirPreco={(preco) => {
              const r = nucleo.definirPrecoIngresso(preco)
              setErrosFinancas(r.ok ? [] : descreverErros(r.erros))
              if (r.ok) {
                salvar()
              }
              atualizar()
            }}
          />
        )
      case 'aoVivo': {
        const tatica = dados(nucleo.tatica())
        return (
          <AoVivo
            inicial={dados(nucleo.aoVivo())}
            formacoes={tatica.formacoes.map((f) => f.nome)}
            mentalidades={tatica.mentalidades}
            marcacoes={tatica.marcacoes}
            concentracoes={tatica.concentracoes}
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
        <div className="menu-rodape">
          <p className={`situacao-save ${save?.tipo === 'erro' ? 'situacao-save-erro' : ''}`} aria-live="polite">
            {save?.tipo === 'salvo' && textos.saves.salvoEm(formatoHora.format(save.em))}
            {save?.tipo === 'erro' && save.mensagem}
          </p>
          <button type="button" className="menu-tema" onClick={exportar} disabled={estado.rodadaAoVivo}>
            {textos.saves.exportar}
          </button>
          <button type="button" className="menu-tema" onClick={aoSairParaOMenu} disabled={estado.rodadaAoVivo}>
            {textos.saves.menuInicial}
          </button>
          {botaoTema}
        </div>
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
