import { useState } from 'react'
import { Escudo } from '../componentes/Escudo'
import type { Estatisticas, EventoPartida, Rodada } from '../nucleo/contrato'
import { descreverEvento, eventosImportantes, minutoDoEvento, textos } from '../textos'

interface Props {
  rodada: Rodada
  meuClubeId: string
}

const LINHAS_ESTATISTICAS: [keyof Estatisticas, string][] = [
  ['finalizacoes', textos.partida.finalizacoes],
  ['noAlvo', textos.partida.noAlvo],
  ['escanteios', textos.partida.escanteios],
  ['faltas', textos.partida.faltas],
  ['amarelos', textos.partida.amarelos],
  ['vermelhos', textos.partida.vermelhos],
]

export function Partida({ rodada, meuClubeId }: Props) {
  const t = textos.partida
  const [todos, setTodos] = useState(false)
  const p = rodada.minhaPartida

  if (!p) {
    return <p className="aviso">{t.semPartida}</p>
  }

  const nomeDoTime = (equipe: EventoPartida['equipe']) =>
    equipe === 'Mandante' ? p.mandante.nome : equipe === 'Visitante' ? p.visitante.nome : ''
  const eventos = p.eventos.filter((e) => todos || eventosImportantes.has(e.tipo))

  return (
    <>
      <section className="cartao placar" aria-label={t.rodada(p.rodada)}>
        <p className="secundario">{t.rodada(p.rodada)}</p>
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
      </section>

      <div className="grade-partida">
        <section className="cartao">
          <h2>{t.lances}</h2>
          <label className="alternador">
            <input type="checkbox" checked={todos} onChange={(e) => setTodos(e.target.checked)} />
            {t.todosOsLances}
          </label>
          <ol className="linha-do-tempo">
            {eventos.map((e, i) => (
              <li
                key={`${e.minuto}-${e.acrescimo}-${i}`}
                className={`evento evento-${e.tipo} ${e.equipe ? `evento-${e.equipe.toLowerCase()}` : ''}`}
              >
                <span className="evento-minuto">{minutoDoEvento(e)}</span>
                <span>{descreverEvento(e, nomeDoTime)}</span>
              </li>
            ))}
          </ol>
        </section>

        <section className="cartao">
          <h2>{t.posse}</h2>
          <div className="barra-posse" aria-label={`${t.posse}: ${p.posseMandante}% × ${100 - p.posseMandante}%`}>
            <span style={{ width: `${p.posseMandante}%`, background: p.mandante.corPrimaria }}>{p.posseMandante}%</span>
            <span style={{ width: `${100 - p.posseMandante}%`, background: p.visitante.corPrimaria }}>
              {100 - p.posseMandante}%
            </span>
          </div>
          <table className="tabela tabela-estatisticas">
            <tbody>
              {LINHAS_ESTATISTICAS.map(([chave, rotulo]) => (
                <tr key={chave}>
                  <td className="numero">{p.estatisticasMandante[chave]}</td>
                  <th scope="row">{rotulo}</th>
                  <td className="numero">{p.estatisticasVisitante[chave]}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </section>
      </div>

      <section className="cartao">
        <h2>{t.outrosJogos}</h2>
        <div className="outros-jogos">
          {rodada.divisoes.map((d) => (
            <div key={d.divisaoId}>
              <h3>{d.divisaoNome}</h3>
              <ul>
                {d.jogos.map((j) => (
                  <li
                    key={j.mandanteId}
                    className={j.mandanteId === meuClubeId || j.visitanteId === meuClubeId ? 'meu-jogo' : undefined}
                  >
                    <span title={j.mandanteNome}>{j.mandanteSigla}</span>
                    <strong>
                      {j.golsMandante} × {j.golsVisitante}
                    </strong>
                    <span title={j.visitanteNome}>{j.visitanteSigla}</span>
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
