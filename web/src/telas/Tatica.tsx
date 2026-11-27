import { type DragEvent, type PointerEvent, useEffect, useRef, useState } from 'react'
import type { DefinicaoTatica, Jogador, Tatica as TaticaDto } from '../nucleo/contrato'
import { textos } from '../textos'
import { escreverAlvo, lerAlvo, type Local, mesmoLocal, posicoesNoCampo, trocar } from './campoTatico'

interface Props {
  tatica: TaticaDto
  jogadores: Jogador[]
  erros: string[]
  /** Envia a tática nova; o núcleo valida e devolve as forças na posição. */
  aoMudar: (definicao: DefinicaoTatica) => void
  aoUsarAutomatica: () => void
}

const MAXIMO_RESERVAS = 7

interface Arrasto {
  nome: string
  x: number
  y: number
}

interface InicioArrasto {
  local: Local
  x: number
  y: number
  nome: string
  moveu: boolean
}

/** Nome curto para caber embaixo da camisa: o último sobrenome (ou o nome único). */
function apelido(nome: string): string {
  const partes = nome.split(' ')
  return partes.length > 1 ? partes[partes.length - 1] : nome
}

export function Tatica({ tatica, jogadores, erros, aoMudar, aoUsarAutomatica }: Props) {
  const t = textos.tatica
  const porId = new Map(jogadores.map((j) => [j.id, j]))
  const titulares = tatica.titulares.map((x) => x.jogadorId)
  const [selecionado, setSelecionado] = useState<Local>()
  const [arrasto, setArrasto] = useState<Arrasto>()
  const inicioArrasto = useRef<InicioArrasto | undefined>(undefined)
  const acabouDeArrastar = useRef(false)

  const enviar = (mudanca: Partial<DefinicaoTatica>) =>
    aoMudar({
      formacao: tatica.formacao,
      mentalidade: tatica.mentalidade,
      titulares,
      reservas: tatica.reservas,
      ...mudanca,
    })

  const aplicar = (origem: Local, destino: Local) => {
    setSelecionado(undefined)
    const nova = trocar({ titulares, reservas: tatica.reservas }, origem, destino, MAXIMO_RESERVAS)
    if (nova) {
      enviar(nova)
    }
  }

  // O arrasto termina num listener da janela: ele precisa da versão mais nova de "aplicar".
  const aplicarRef = useRef(aplicar)
  useEffect(() => {
    aplicarRef.current = aplicar
  })

  // Tocar/clicar: o primeiro toque escolhe, o segundo troca.
  const tocar = (local: Local) => {
    if (acabouDeArrastar.current) return
    if (!selecionado) {
      if (local.tipo === 'reserva' && tatica.reservas[local.indice] === undefined) return
      setSelecionado(local)
    } else if (mesmoLocal(selecionado, local)) {
      setSelecionado(undefined)
    } else {
      aplicar(selecionado, local)
    }
  }

  // Arrastar (mouse e toque) com pointer events; ao soltar, procura a vaga debaixo do ponteiro.
  useEffect(() => {
    const mover = (e: globalThis.PointerEvent) => {
      const inicio = inicioArrasto.current
      if (!inicio) return
      if (!inicio.moveu && Math.hypot(e.clientX - inicio.x, e.clientY - inicio.y) < 8) return
      inicio.moveu = true
      setArrasto({ nome: inicio.nome, x: e.clientX, y: e.clientY })
    }
    const soltar = (e: globalThis.PointerEvent) => {
      const inicio = inicioArrasto.current
      // Termina o arrasto já: um "pointermove" que chegue depois não pode redesenhar o nome flutuando.
      inicioArrasto.current = undefined
      setArrasto(undefined)
      if (!inicio?.moveu) return

      const alvo = document.elementFromPoint?.(e.clientX, e.clientY)?.closest<HTMLElement>('[data-alvo]')
      const destino = lerAlvo(alvo?.dataset.alvo)
      if (destino) {
        aplicarRef.current(inicio.local, destino)
      }
      // O clique que o navegador dispara logo depois do arrasto não deve selecionar nada.
      acabouDeArrastar.current = true
      setTimeout(() => {
        acabouDeArrastar.current = false
      }, 0)
    }
    // O navegador cancelou o gesto (ex.: virou rolagem): some com o nome e não troca nada.
    const cancelar = () => {
      inicioArrasto.current = undefined
      setArrasto(undefined)
    }
    window.addEventListener('pointermove', mover)
    window.addEventListener('pointerup', soltar)
    window.addEventListener('pointercancel', cancelar)
    window.addEventListener('blur', cancelar)
    return () => {
      window.removeEventListener('pointermove', mover)
      window.removeEventListener('pointerup', soltar)
      window.removeEventListener('pointercancel', cancelar)
      window.removeEventListener('blur', cancelar)
    }
  }, [])

  const propsDe = (local: Local, nome: string | undefined) => ({
    'data-alvo': escreverAlvo(local),
    'aria-pressed': selecionado !== undefined && mesmoLocal(selecionado, local),
    onClick: () => tocar(local),
    // Sem o arrastar nativo do navegador (ele engoliria o pointerup).
    onDragStart: (e: DragEvent<HTMLElement>) => e.preventDefault(),
    onPointerDown: (e: PointerEvent<HTMLElement>) => {
      if (nome !== undefined) {
        inicioArrasto.current = { local, x: e.clientX, y: e.clientY, nome, moveu: false }
      }
    },
  })

  const posicoes = posicoesNoCampo(tatica.titulares.map((x) => x.posicao))
  const foraDoTime = jogadores.filter((j) => !titulares.includes(j.id) && !tatica.reservas.includes(j.id))
  const vagasNoBanco = Array.from({ length: MAXIMO_RESERVAS }, (_, i) => tatica.reservas[i])

  return (
    <>
      <header className="cabecalho cabecalho-acoes">
        <h1>{t.titulo}</h1>
        <button type="button" className="botao botao-secundario" onClick={aoUsarAutomatica}>
          {t.automatica}
        </button>
      </header>

      {erros.length > 0 && (
        <ul className="erro-lista" role="alert">
          {erros.map((e) => (
            <li key={e}>{e}</li>
          ))}
        </ul>
      )}

      <div className="controles-tatica">
        <label>
          {t.formacao}
          <select value={tatica.formacao} onChange={(e) => enviar({ formacao: e.target.value })}>
            {tatica.formacoes.map((f) => (
              <option key={f.nome} value={f.nome}>
                {f.nome}
              </option>
            ))}
          </select>
        </label>

        <fieldset className="mentalidades">
          <legend>{t.mentalidade}</legend>
          {tatica.mentalidades.map((m) => (
            <button
              key={m}
              type="button"
              className={`pilula ${m === tatica.mentalidade ? 'pilula-ativa' : ''}`}
              aria-pressed={m === tatica.mentalidade}
              onClick={() => enviar({ mentalidade: m })}
            >
              {t.mentalidades[m] ?? m}
            </button>
          ))}
        </fieldset>
      </div>

      <p className="secundario dica" aria-live="polite">
        {selecionado ? t.escolhaDestino : t.dicaArrastar}
      </p>

      <div className="grade-tatica">
        <section className="campo" aria-label={t.titulares}>
          <div className="campo-linhas" aria-hidden="true">
            <span className="campo-meio" />
            <span className="campo-circulo" />
            <span className="campo-area campo-area-cima" />
            <span className="campo-area campo-area-baixo" />
          </div>
          {tatica.titulares.map((titular, i) => {
            const jogador = porId.get(titular.jogadorId)
            const nome = jogador?.nome ?? '?'
            const classes = ['peca', titular.improvisado ? 'peca-improvisada' : '', jogador && !jogador.disponivel ? 'peca-fora' : '']
            return (
              <button
                key={`${titular.posicao}-${i}`}
                type="button"
                className={classes.join(' ')}
                style={{ left: `${posicoes[i].x}%`, top: `${posicoes[i].y}%` }}
                aria-label={t.titularNaPosicao(nome, titular.posicao)}
                {...propsDe({ tipo: 'titular', indice: i }, nome)}
              >
                <span className="peca-camisa" title={t.forcaNaPosicao}>
                  {titular.forcaNaPosicao}
                </span>
                <span className="peca-nome">{apelido(nome)}</span>
                <span className="peca-posicao">
                  {titular.posicao}
                  {titular.improvisado && ` · ${t.improvisado}`}
                  {jogador && !jogador.disponivel && ' ✖'}
                </span>
              </button>
            )
          })}
        </section>

        <div className="lado-tatica">
          <section className="cartao">
            <h2>{t.reservas}</h2>
            <ul className="lista-banco">
              {vagasNoBanco.map((id, i) => {
                const jogador = id === undefined ? undefined : porId.get(id)
                const local: Local = { tipo: 'reserva', indice: i }
                return (
                  <li key={i}>
                    {jogador ? (
                      <button
                        type="button"
                        className={`linha-jogador ${jogador.disponivel ? '' : 'linha-indisponivel'}`}
                        aria-label={t.noBanco(jogador.nome)}
                        {...propsDe(local, jogador.nome)}
                      >
                        <span className="sigla-posicao">{jogador.posicao}</span>
                        <span className="linha-jogador-nome">{jogador.nome}</span>
                        <strong>{jogador.forca}</strong>
                      </button>
                    ) : (
                      <button type="button" className="linha-jogador vaga-vazia" aria-label={t.vagaNoBanco(i + 1)} {...propsDe(local, undefined)}>
                        {t.vagaNoBanco(i + 1)}
                      </button>
                    )}
                  </li>
                )
              })}
            </ul>
          </section>

          <section className="cartao" data-alvo="fora">
            <h2>{t.foraDoTime}</h2>
            <ul className="lista-banco">
              {foraDoTime.map((j) => (
                <li key={j.id}>
                  <button
                    type="button"
                    className={`linha-jogador ${j.disponivel ? '' : 'linha-indisponivel'}`}
                    aria-label={t.foraDoTimeJogador(j.nome)}
                    {...propsDe({ tipo: 'fora', id: j.id }, j.nome)}
                  >
                    <span className="sigla-posicao">{j.posicao}</span>
                    <span className="linha-jogador-nome">
                      {j.nome}
                      {!j.disponivel && ' ✖'}
                    </span>
                    <strong>{j.forca}</strong>
                  </button>
                </li>
              ))}
            </ul>
          </section>
        </div>
      </div>

      {arrasto && (
        <div className="peca-arrastada" style={{ left: arrasto.x, top: arrasto.y }} aria-hidden="true">
          {arrasto.nome}
        </div>
      )}
    </>
  )
}
