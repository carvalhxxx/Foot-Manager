import { useState } from 'react'
import { Escudo } from '../componentes/Escudo'
import type { Estatisticas, EventoPartida, Rodada } from '../nucleo/contrato'
import { descreverEvento, eventosImportantes, formatoDinheiro, minutoDoEvento, textos } from '../textos'

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
  const tc = textos.copa
  const copa = rodada.minhaCopa

  // Meu jogo da Copa no meio de semana (simulado direto, sem ao vivo nesta versão).
  const cartaoCopa = copa && (
    <section className="cartao placar placar-copa" aria-label={tc.meioDeSemana}>
      <p className="secundario">
        {tc.meioDeSemana} · {tc.fases[copa.fase] ?? copa.fase}
        {copa.perna === 2 ? ` (${textos.classificacao.volta.toLowerCase()})` : ''}
      </p>
      <div className="placar-linha">
        <span className="placar-time">
          <Escudo corPrimaria={copa.partida.mandante.corPrimaria} corSecundaria={copa.partida.mandante.corSecundaria} />
          {copa.partida.mandante.nome}
        </span>
        <span className="placar-gols">
          {copa.partida.golsMandante} <span aria-hidden="true">×</span> {copa.partida.golsVisitante}
        </span>
        <span className="placar-time placar-time-visitante">
          {copa.partida.visitante.nome}
          <Escudo corPrimaria={copa.partida.visitante.corPrimaria} corSecundaria={copa.partida.visitante.corSecundaria} />
        </span>
      </div>
      {copa.penaltisMandante !== undefined && copa.penaltisVisitante !== undefined && (
        <p>
          <strong>{tc.penaltisDisputa(copa.penaltisMandante, copa.penaltisVisitante)}</strong>
        </p>
      )}
      {copa.confrontoEncerrado && (
        <p className={copa.classificado ? 'positivo' : 'negativo'}>
          {copa.classificado ? (copa.fase === 'Final' ? tc.venceuACopa : tc.classificou) : tc.caiu}
        </p>
      )}
      <ul className="lista-simples">
        {copa.partida.eventos
          .filter((e) => e.tipo === 'Gol' || e.tipo === 'CartaoVermelho')
          .map((e, i) => (
            <li key={`${e.minuto}-${i}`}>
              {minutoDoEvento(e)}{' '}
              {descreverEvento(e, (eq) => (eq === 'Mandante' ? copa.partida.mandante.nome : eq === 'Visitante' ? copa.partida.visitante.nome : ''))}
            </li>
          ))}
      </ul>
    </section>
  )

  // Outros jogos e mercado da rodada: aparecem mesmo quando o time do jogador não jogou (ex.: playoff da Série B).
  const restoDaRodada = (
    <>
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

        {rodada.jogosDaCopa && rodada.jogosDaCopa.length > 0 && (
          <section className="cartao">
            <h2>{tc.outrosJogos}</h2>
            <ul className="outros-jogos-copa">
              {rodada.jogosDaCopa.map((j) => (
                <li key={j.mandanteId} className={j.mandanteId === meuClubeId || j.visitanteId === meuClubeId ? 'meu-jogo' : undefined}>
                  <span title={j.mandanteNome}>{j.mandanteSigla}</span>
                  <strong>
                    {j.golsMandante} × {j.golsVisitante}
                  </strong>
                  <span title={j.visitanteNome}>{j.visitanteSigla}</span>
                </li>
              ))}
            </ul>
          </section>
        )}

        {rodada.transferencias.length > 0 && (
          <section className="cartao">
            <h2>{t.mercado}</h2>
            <ul className="lista-simples">
              {rodada.transferencias.map((tr) => (
                <li key={`${tr.jogador}-${tr.paraSigla}`}>
                  <span title={tr.paraNome}>{t.transferencia(tr.jogador, tr.deSigla ?? t.agenteLivre, tr.paraSigla)}</span>
                  {tr.valor > 0 && <span className="secundario"> · {formatoDinheiro.format(tr.valor)}</span>}
                </li>
              ))}
            </ul>
          </section>
        )}
    </>
  )

  if (!p) {
    return rodada.divisoes.length === 0 && !copa ? (
      <p className="aviso">{t.semPartida}</p>
    ) : (
      <>
        <header className="cabecalho">
          <h1>{t.rodada(rodada.rodada)}</h1>
          <p>{t.semJogoMeu}</p>
        </header>
        {cartaoCopa}
        {restoDaRodada}
      </>
    )
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

      {cartaoCopa}
      {restoDaRodada}
    </>
  )
}
