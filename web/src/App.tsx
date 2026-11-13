import { useCallback, useEffect, useState } from 'react'
import { iniciarJogo, type ResultadoInicio } from './nucleo/iniciar'
import { Inicio } from './telas/Inicio'
import { useTema } from './tema'
import { textos } from './textos'

type Estado =
  | { fase: 'carregando' }
  | { fase: 'pronto'; resultado: Extract<ResultadoInicio, { ok: true }> }
  | { fase: 'erro'; mensagens: string[] }

interface Props {
  /** Injetável para testes; em produção carrega o núcleo C# de verdade. */
  iniciar?: () => Promise<ResultadoInicio>
}

export default function App({ iniciar = iniciarJogo }: Props) {
  const [tema, alternarTema] = useTema()
  const [estado, setEstado] = useState<Estado>({ fase: 'carregando' })

  const buscar = useCallback(() => {
    iniciar()
      .then((resultado) =>
        setEstado(
          resultado.ok
            ? { fase: 'pronto', resultado }
            : {
                fase: 'erro',
                mensagens: resultado.erros.map((e) =>
                  [e.arquivo, e.local].filter(Boolean).join(' → ') + (e.arquivo ? ': ' : '') + e.mensagem,
                ),
              },
        ),
      )
      .catch((erro: unknown) =>
        setEstado({ fase: 'erro', mensagens: [erro instanceof Error ? erro.message : textos.erro.inesperado] }),
      )
  }, [iniciar])

  useEffect(buscar, [buscar])

  const tentarDeNovo = () => {
    setEstado({ fase: 'carregando' })
    buscar()
  }

  return (
    <div className="casca">
      <aside className="menu">
        <div className="marca">
          <span className="marca-bola" aria-hidden="true">
            ⚽
          </span>
          {textos.nomeDoJogo}
        </div>
        <nav>
          <button type="button" className="menu-item ativo" aria-current="page">
            {textos.menu.inicio}
          </button>
        </nav>
        <button type="button" className="menu-tema" onClick={alternarTema}>
          {tema === 'escuro' ? textos.tema.alternarParaClaro : textos.tema.alternarParaEscuro}
        </button>
      </aside>

      <main className="conteudo">
        {estado.fase === 'carregando' && (
          <p className="aviso" role="status">
            {textos.carregando}
          </p>
        )}

        {estado.fase === 'erro' && (
          <section className="erro" role="alert">
            <h1>{textos.erro.titulo}</h1>
            <ul>
              {estado.mensagens.map((m) => (
                <li key={m}>{m}</li>
              ))}
            </ul>
            <button type="button" className="botao" onClick={tentarDeNovo}>
              {textos.erro.tentarDeNovo}
            </button>
          </section>
        )}

        {estado.fase === 'pronto' && <Inicio mundo={estado.resultado.mundo} versao={estado.resultado.versao} />}
      </main>
    </div>
  )
}
