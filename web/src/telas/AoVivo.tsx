import { useCallback, useEffect, useRef, useState } from 'react'
import { Escudo } from '../componentes/Escudo'
import type { AoVivo as AoVivoDto, Decisao, Resposta } from '../nucleo/contrato'
import { descreverErros } from '../nucleo/iniciar'
import { narrar } from '../narracao'
import { minutoDoEvento, textos } from '../textos'

type Velocidade = 'normal' | 'rapida' | 'turbo'

/** Milissegundos entre um minuto de jogo e o seguinte. */
const INTERVALO_POR_VELOCIDADE: Record<Velocidade, number> = { normal: 700, rapida: 250, turbo: 60 }

interface Props {
  inicial: AoVivoDto
  formacoes: string[]
  mentalidades: string[]
  avancar: (minutos: number) => Resposta<AoVivoDto>
  decidir: (decisao: Decisao) => Resposta<AoVivoDto>
  aoConcluir: () => void
}

function elementoEditavel(alvo: EventTarget | null): boolean {
  return alvo instanceof HTMLElement && ['INPUT', 'SELECT', 'TEXTAREA', 'BUTTON'].includes(alvo.tagName)
}

export function AoVivo({ inicial, formacoes, mentalidades, avancar, decidir, aoConcluir }: Props) {
  const t = textos.aoVivo
  const [dados, setDados] = useState(inicial)
  const [rodando, setRodando] = useState(!inicial.rodadaEncerrada)
  const [velocidade, setVelocidade] = useState<Velocidade>('normal')
  const [aviso, setAviso] = useState<string | undefined>()
  const [erros, setErros] = useState<string[]>([])
  const [sai, setSai] = useState<number | undefined>()
  const [entra, setEntra] = useState<number | undefined>()

  // Trava síncrona: depois de uma pausa nenhum minuto extra escapa, mesmo que o relógio dispare
  // de novo antes de a tela atualizar (na velocidade 10× isso acontece).
  const pausadoRef = useRef(inicial.rodadaEncerrada)

  const pausar = useCallback(() => {
    pausadoRef.current = true
    setRodando(false)
  }, [])

  const aplicar = useCallback((r: Resposta<AoVivoDto>, aoPausar: boolean) => {
    if (!r.ok) {
      setErros(descreverErros(r.erros))
      pausar()
      return
    }
    setErros([])
    setDados(r.dados)
    if (aoPausar && r.dados.pausa) {
      pausar()
      setAviso(t.pausas[r.dados.pausa])
    }
  }, [pausar, t.pausas])

  // O relógio: um minuto de jogo a cada intervalo, enquanto estiver rodando.
  useEffect(() => {
    if (!rodando || dados.rodadaEncerrada) {
      return
    }
    pausadoRef.current = false
    const id = setInterval(() => {
      if (!pausadoRef.current) {
        aplicar(avancar(1), true)
      }
    }, INTERVALO_POR_VELOCIDADE[velocidade])
    return () => clearInterval(id)
  }, [rodando, velocidade, dados.rodadaEncerrada, avancar, aplicar])

  const alternar = useCallback(() => {
    if (dados.rodadaEncerrada) {
      return
    }
    setAviso(undefined)
    if (rodando) {
      pausar()
    } else {
      setRodando(true)
    }
  }, [dados.rodadaEncerrada, pausar, rodando])

  // Espaço pausa/continua.
  useEffect(() => {
    const aoTeclar = (e: KeyboardEvent) => {
      if (e.code === 'Space' && !elementoEditavel(e.target)) {
        e.preventDefault()
        alternar()
      }
    }
    window.addEventListener('keydown', aoTeclar)
    return () => window.removeEventListener('keydown', aoTeclar)
  }, [alternar])

  const pularParaOFim = () => {
    pausar()
    let r = avancar(300)
    // Pular ignora as pausas: o jogador escolheu não acompanhar.
    while (r.ok && !r.dados.rodadaEncerrada) {
      r = avancar(300)
    }
    aplicar(r, false)
    setAviso(undefined)
  }

  const enviar = (decisao: Decisao) => aplicar(decidir(decisao), false)

  const p = dados.minhaPartida
  if (!p) {
    return null
  }

  const nomes = { mandante: p.mandante.nome, visitante: p.visitante.nome }
  const relogio =
    p.encerrada ? t.encerrado
    : p.tempo === 2 && p.minuto === 45 && p.acrescimo === 0 ? t.intervalo
    : p.acrescimo > 0 ? `${p.minuto}+${p.acrescimo}'`
    : `${p.minuto}'`
  const opcoesSai = [
    ...p.vagasAbertas.map((v) => ({ id: v.jogadorId, rotulo: t.vaga(v.nome) })),
    ...p.emCampo.map((j) => ({ id: j.id, rotulo: `${j.posicao} · ${j.nome} (${j.condicao}%)` })),
  ]
  // Nada vem marcado (evita trocar alguém sem querer), exceto a vaga de um lesionado, que é a troca óbvia.
  const saiEscolhido = opcoesSai.some((o) => o.id === sai) ? sai : p.vagasAbertas[0]?.jogadorId
  const entraEscolhido = p.reservas.some((r) => r.id === entra) ? entra : undefined

  return (
    <>
      <section className="cartao placar placar-ao-vivo" aria-live="polite">
        <div className="placar-linha">
          <span className="placar-time">
            <Escudo corPrimaria={p.mandante.corPrimaria} corSecundaria={p.mandante.corSecundaria} tamanho="grande" />
            {p.mandante.nome}
          </span>
          <span className="placar-gols">
            {p.golsMandante} <span aria-hidden="true">×</span> {p.golsVisitante}
          </span>
          <span className="placar-time placar-time-visitante">
            {p.visitante.nome}
            <Escudo corPrimaria={p.visitante.corPrimaria} corSecundaria={p.visitante.corSecundaria} tamanho="grande" />
          </span>
        </div>
        <p className="relogio" data-testid="relogio">
          {relogio}
        </p>
      </section>

      <div className="controles-ao-vivo">
        {dados.rodadaEncerrada ? (
          <button type="button" className="botao botao-grande" onClick={aoConcluir}>
            {t.verResumo}
          </button>
        ) : (
          <>
            <button type="button" className="botao" onClick={alternar}>
              {rodando ? `⏸ ${t.pausar}` : `▶ ${t.continuar}`}
              <kbd>{textos.avancar.atalho}</kbd>
            </button>
            <div className="mentalidades" role="group" aria-label={t.velocidade}>
              {(Object.keys(INTERVALO_POR_VELOCIDADE) as Velocidade[]).map((v) => (
                <button
                  key={v}
                  type="button"
                  className={`pilula ${v === velocidade ? 'pilula-ativa' : ''}`}
                  aria-pressed={v === velocidade}
                  onClick={() => setVelocidade(v)}
                >
                  {t.velocidades[v]}
                </button>
              ))}
            </div>
            <button type="button" className="botao botao-secundario" onClick={pularParaOFim}>
              {t.pularParaOFim}
            </button>
          </>
        )}
      </div>

      {aviso && (
        <p className="aviso-destaque" role="status">
          {aviso}
        </p>
      )}
      {erros.length > 0 && (
        <ul className="erro-lista" role="alert">
          {erros.map((e) => (
            <li key={e}>{e}</li>
          ))}
        </ul>
      )}

      <div className="grade-partida">
        <section className="cartao">
          <h2>{t.narracao}</h2>
          <ol className="linha-do-tempo narracao" reversed>
            {[...p.eventos].reverse().map((e, i) => (
              <li
                key={`${e.minuto}-${e.acrescimo}-${p.eventos.length - i}`}
                className={`evento evento-${e.tipo} ${e.equipe ? `evento-${e.equipe.toLowerCase()}` : ''}`}
              >
                <span className="evento-minuto">{minutoDoEvento(e)}</span>
                <span>{narrar(e, nomes)}</span>
              </li>
            ))}
          </ol>
        </section>

        <section className="cartao">
          <h2>{t.meuTime}</h2>

          <div className="controles-tatica">
            <label>
              {textos.tatica.formacao}
              <select value={p.formacao} onChange={(e) => enviar({ tipo: 'formacao', formacao: e.target.value })}>
                {formacoes.map((f) => (
                  <option key={f} value={f}>
                    {f}
                  </option>
                ))}
              </select>
            </label>
          </div>

          <fieldset className="mentalidades">
            <legend>{textos.tatica.mentalidade}</legend>
            {mentalidades.map((m) => (
              <button
                key={m}
                type="button"
                className={`pilula ${m === p.mentalidade ? 'pilula-ativa' : ''}`}
                aria-pressed={m === p.mentalidade}
                onClick={() => enviar({ tipo: 'mentalidade', mentalidade: m })}
              >
                {textos.tatica.mentalidades[m] ?? m}
              </button>
            ))}
          </fieldset>

          <ul className="em-campo">
            {p.emCampo.map((j) => (
              <li key={j.id}>
                <span className="sigla-posicao">{j.posicao}</span>
                <span className="em-campo-nome">
                  {j.nome}
                  {j.gols > 0 && ` ${'⚽'.repeat(j.gols)}`}
                  {j.amarelos > 0 && ' 🟨'}
                </span>
                <span className="barra" aria-label={`${j.condicao}%`}>
                  <span className={`barra-preenchida ${j.condicao < 65 ? 'barra-baixa' : ''}`} style={{ width: `${j.condicao}%` }} />
                </span>
              </li>
            ))}
          </ul>

          <h3>{t.substituicao}</h3>
          {p.substituicoesRestantes === 0 || p.reservas.length === 0 ? (
            <p className="secundario">{t.semSubstituicoes}</p>
          ) : (
            <div className="form-substituicao">
              <label>
                {t.sai}
                <select value={saiEscolhido ?? ''} onChange={(e) => setSai(Number(e.target.value))}>
                  <option value="" disabled>
                    {t.escolha}
                  </option>
                  {opcoesSai.map((o) => (
                    <option key={o.id} value={o.id}>
                      {o.rotulo}
                    </option>
                  ))}
                </select>
              </label>
              <label>
                {t.entra}
                <select value={entraEscolhido ?? ''} onChange={(e) => setEntra(Number(e.target.value))}>
                  <option value="" disabled>
                    {t.escolha}
                  </option>
                  {p.reservas.map((r) => (
                    <option key={r.id} value={r.id}>
                      {r.posicao} · {r.nome} · {r.forca}
                    </option>
                  ))}
                </select>
              </label>
              <button
                type="button"
                className="botao"
                disabled={saiEscolhido === undefined || entraEscolhido === undefined}
                onClick={() => {
                  enviar({ tipo: 'substituicao', saiId: saiEscolhido!, entraId: entraEscolhido! })
                  setSai(undefined)
                  setEntra(undefined)
                }}
              >
                {t.substituir}
              </button>
              <p className="secundario">{t.restantes(p.substituicoesRestantes)}</p>
            </div>
          )}
        </section>
      </div>

      <section className="cartao">
        <h2>{t.placarDaRodada}</h2>
        <div className="outros-jogos placar-rodada-ao-vivo">
          {dados.placares.map((d) => (
            <div key={d.divisaoId}>
              <h3>{d.divisaoNome}</h3>
              <ul>
                {d.jogos.map((j) => (
                  <li
                    key={j.mandanteId}
                    className={j.mandanteId === p.mandante.id || j.visitanteId === p.mandante.id ? 'meu-jogo' : undefined}
                  >
                    <span>{j.mandanteSigla}</span>
                    <strong>
                      {j.golsMandante} × {j.golsVisitante}
                    </strong>
                    <span>{j.visitanteSigla}</span>
                    <span className="minuto-placar">
                      {j.encerrado ? t.encerrado : j.acrescimo > 0 ? `${j.minuto}+${j.acrescimo}'` : `${j.minuto}'`}
                    </span>
                  </li>
                ))}
              </ul>
            </div>
          ))}
        </div>
      </section>
    </>
  )
}
