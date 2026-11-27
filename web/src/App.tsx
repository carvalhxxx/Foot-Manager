import { useCallback, useEffect, useState } from 'react'
import { JogoEmAndamento } from './JogoEmAndamento'
import type { ResumoMundo } from './nucleo/contrato'
import { descreverErros, iniciarJogo, novaSeed, type ResultadoInicio } from './nucleo/iniciar'
import type { Nucleo } from './nucleo/nucleo'
import { armazenamentoPadrao, type ArmazenamentoSaves, type ResumoSave } from './saves/armazenamento'
import { Continuar } from './telas/Continuar'
import { EscolherClube } from './telas/EscolherClube'
import { useTema } from './tema'
import { textos } from './textos'

type Fase =
  | { tipo: 'carregando' }
  | { tipo: 'erro'; mensagens: string[] }
  | { tipo: 'saves'; saves: ResumoSave[]; erro?: string }
  | { tipo: 'escolhendo' }
  | { tipo: 'jogando'; slotId: string }

interface Props {
  /** Injetável para testes; em produção carrega o núcleo C# de verdade. */
  iniciar?: () => Promise<ResultadoInicio>
  /** Injetável para testes. */
  gerarSeed?: () => string
  /** Injetável para testes; em produção usa o IndexedDB do navegador. */
  armazenamento?: ArmazenamentoSaves
  /** Injetável para testes: identificador de um save novo. */
  gerarIdDeSave?: () => string
}

export default function App({
  iniciar = iniciarJogo,
  gerarSeed = novaSeed,
  armazenamento: armazenamentoInjetado,
  gerarIdDeSave = () => crypto.randomUUID(),
}: Props) {
  // Um armazenamento por instância do App (o do navegador é o mesmo IndexedDB de qualquer forma).
  const [armazenamento] = useState(() => armazenamentoInjetado ?? armazenamentoPadrao())
  const [tema, alternarTema] = useTema()
  const [fase, setFase] = useState<Fase>({ tipo: 'carregando' })
  const [nucleo, setNucleo] = useState<Nucleo | undefined>()
  const [mundo, setMundo] = useState<ResumoMundo | undefined>()

  /** Com saves guardados, mostra "Continuar"; sem nenhum, vai direto para a escolha de clube. */
  const irParaOMenu = useCallback(
    async (erro?: string) => {
      const saves = await armazenamento.listar().catch(() => [])
      setFase(saves.length > 0 || erro ? { tipo: 'saves', saves, erro } : { tipo: 'escolhendo' })
    },
    [armazenamento],
  )

  const buscar = useCallback(() => {
    iniciar()
      .then(async (r) => {
        if (!r.ok) {
          setFase({ tipo: 'erro', mensagens: descreverErros(r.erros) })
          return
        }
        setNucleo(r.nucleo)
        setMundo(r.mundo)
        await irParaOMenu()
      })
      .catch((erro: unknown) =>
        setFase({ tipo: 'erro', mensagens: [erro instanceof Error ? erro.message : textos.erro.inesperado] }),
      )
  }, [iniciar, irParaOMenu])

  useEffect(buscar, [buscar])

  const tentarDeNovo = () => {
    setFase({ tipo: 'carregando' })
    buscar()
  }

  const escolher = (clubeId: string) => {
    const r = nucleo!.novaCarreira(clubeId, gerarSeed())
    setFase(r.ok ? { tipo: 'jogando', slotId: gerarIdDeSave() } : { tipo: 'erro', mensagens: descreverErros(r.erros) })
  }

  const abrirSave = async (id: string) => {
    const conteudo = await armazenamento.lerConteudo(id)
    const r = conteudo ? nucleo!.carregarSave(conteudo) : undefined
    if (r?.ok) {
      setFase({ tipo: 'jogando', slotId: id })
    } else {
      await irParaOMenu(r ? descreverErros(r.erros).join(' ') : textos.saves.erroAoImportar)
    }
  }

  const importar = async (conteudo: string) => {
    const r = nucleo!.carregarSave(conteudo)
    if (r.ok) {
      // Um arquivo importado vira uma carreira nova na lista (não sobrescreve as outras).
      setFase({ tipo: 'jogando', slotId: gerarIdDeSave() })
    } else {
      await irParaOMenu(`${textos.saves.erroAoImportar} ${descreverErros(r.erros).join(' ')}`)
    }
  }

  const apagar = async (id: string) => {
    await armazenamento.apagar(id)
    await irParaOMenu()
  }

  const botaoTema = (
    <button type="button" className="menu-tema" onClick={alternarTema}>
      {tema === 'escuro' ? textos.tema.alternarParaClaro : textos.tema.alternarParaEscuro}
    </button>
  )

  if (fase.tipo === 'jogando' && nucleo && mundo) {
    return (
      <JogoEmAndamento
        key={fase.slotId}
        nucleo={nucleo}
        mundo={mundo}
        botaoTema={botaoTema}
        slotId={fase.slotId}
        armazenamento={armazenamento}
        aoSairParaOMenu={() => void irParaOMenu()}
      />
    )
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

        {fase.tipo === 'saves' && (
          <Continuar
            saves={fase.saves}
            erro={fase.erro}
            aoContinuar={(id) => void abrirSave(id)}
            aoApagar={(id) => void apagar(id)}
            aoNovoJogo={() => setFase({ tipo: 'escolhendo' })}
            aoImportar={(conteudo) => void importar(conteudo)}
          />
        )}

        {fase.tipo === 'escolhendo' && mundo && <EscolherClube mundo={mundo} aoEscolher={escolher} />}
      </main>
    </div>
  )
}
