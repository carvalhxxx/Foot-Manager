import { useCallback, useEffect, useState } from 'react'
import { JogoEmAndamento } from './JogoEmAndamento'
import type { ResumoMundo } from './nucleo/contrato'
import { descreverErros, iniciarJogo, novaSeed, type ResultadoInicio } from './nucleo/iniciar'
import type { Nucleo } from './nucleo/nucleo'
import { EscolherClube } from './telas/EscolherClube'
import { useTema } from './tema'
import { textos } from './textos'

type Fase =
  | { tipo: 'carregando' }
  | { tipo: 'erro'; mensagens: string[] }
  | { tipo: 'escolhendo'; nucleo: Nucleo; mundo: ResumoMundo }
  | { tipo: 'jogando'; nucleo: Nucleo; mundo: ResumoMundo }

interface Props {
  /** Injetável para testes; em produção carrega o núcleo C# de verdade. */
  iniciar?: () => Promise<ResultadoInicio>
  /** Injetável para testes. */
  gerarSeed?: () => string
}

export default function App({ iniciar = iniciarJogo, gerarSeed = novaSeed }: Props) {
  const [tema, alternarTema] = useTema()
  const [fase, setFase] = useState<Fase>({ tipo: 'carregando' })

  const buscar = useCallback(() => {
    iniciar()
      .then((r) =>
        setFase(r.ok ? { tipo: 'escolhendo', nucleo: r.nucleo, mundo: r.mundo } : { tipo: 'erro', mensagens: descreverErros(r.erros) }),
      )
      .catch((erro: unknown) =>
        setFase({ tipo: 'erro', mensagens: [erro instanceof Error ? erro.message : textos.erro.inesperado] }),
      )
  }, [iniciar])

  useEffect(buscar, [buscar])

  const tentarDeNovo = () => {
    setFase({ tipo: 'carregando' })
    buscar()
  }

  const escolher = (clubeId: string) => {
    if (fase.tipo !== 'escolhendo') {
      return
    }
    const r = fase.nucleo.novaCarreira(clubeId, gerarSeed())
    setFase(r.ok ? { ...fase, tipo: 'jogando' } : { tipo: 'erro', mensagens: descreverErros(r.erros) })
  }

  const botaoTema = (
    <button type="button" className="menu-tema" onClick={alternarTema}>
      {tema === 'escuro' ? textos.tema.alternarParaClaro : textos.tema.alternarParaEscuro}
    </button>
  )

  if (fase.tipo === 'jogando') {
    return <JogoEmAndamento nucleo={fase.nucleo} mundo={fase.mundo} botaoTema={botaoTema} />
  }

  return (
    <div className="casca casca-simples">
      <header className="barra-topo">
        <span className="marca">
          <span aria-hidden="true">⚽</span> {textos.nomeDoJogo}
        </span>
        {botaoTema}
      </header>

      <main className="conteudo">
        {fase.tipo === 'carregando' && (
          <p className="aviso" role="status">
            {textos.carregando}
          </p>
        )}

        {fase.tipo === 'erro' && (
          <section className="erro" role="alert">
            <h1>{textos.erro.titulo}</h1>
            <ul>
              {fase.mensagens.map((m) => (
                <li key={m}>{m}</li>
              ))}
            </ul>
            <button type="button" className="botao" onClick={tentarDeNovo}>
              {textos.erro.tentarDeNovo}
            </button>
          </section>
        )}

        {fase.tipo === 'escolhendo' && <EscolherClube mundo={fase.mundo} aoEscolher={escolher} />}
      </main>
    </div>
  )
}
