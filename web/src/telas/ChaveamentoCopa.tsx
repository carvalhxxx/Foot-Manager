import { useState } from 'react'
import type { ConfrontoCopa, Copa } from '../nucleo/contrato'
import { textos } from '../textos'

interface Props {
  copa: Copa
}

const placar = (a?: number, b?: number) => (a === undefined || b === undefined ? '– × –' : `${a} × ${b}`)

/** Chaveamento da Copa do Brasil: uma fase por vez (abre na mais recente). */
export function ChaveamentoCopa({ copa }: Props) {
  const t = textos.copa
  const ultima = copa.fases.length - 1
  const [indice, setIndice] = useState(ultima)
  const fase = copa.fases[Math.min(indice, ultima)]

  return (
    <div className="grade-classificacao">
      <section className="cartao" aria-label={t.titulo}>
        <div className="abas" role="tablist" aria-label={t.titulo}>
          {copa.fases.map((f, i) => (
            <button
              key={f.fase}
              type="button"
              role="tab"
              aria-selected={f === fase}
              className={`pilula ${f === fase ? 'pilula-ativa' : ''}`}
              onClick={() => setIndice(i)}
            >
              {t.fases[f.fase] ?? f.fase}
            </button>
          ))}
        </div>
        {fase && (
          <>
            <p className="secundario">{t.datas(fase.rodadaIda, fase.rodadaVolta)}</p>
            <ul className="lista-simples chaveamento">
              {fase.confrontos.map((c) => (
                <LinhaConfronto key={`${c.aSigla}-${c.bSigla}`} c={c} jogoUnico={fase.jogoUnico} />
              ))}
            </ul>
          </>
        )}
      </section>

      <section className="cartao regras-legenda">
        <h2>{t.titulo}</h2>
        {copa.campeaoNome && <p className="positivo">{t.campeao(copa.campeaoNome)}</p>}
        <p>{t.regra}</p>
        {copa.cabecas.length > 0 && <p className="secundario">{t.cabecas(copa.cabecas.join(', '))}</p>}
      </section>
    </div>
  )
}

function LinhaConfronto({ c, jogoUnico }: { c: ConfrontoCopa; jogoUnico: boolean }) {
  const t = textos.copa
  const nome = (sigla: string, n: string) => (
    <span className={c.vencedorSigla === sigla ? 'vencedor' : c.vencedorSigla ? 'secundario' : undefined}>{n}</span>
  )
  const temVolta = c.voltaGolsA !== undefined && c.voltaGolsB !== undefined
  return (
    <li className={c.meu ? 'meu-jogo' : undefined}>
      <strong>
        {nome(c.aSigla, c.aNome)} × {nome(c.bSigla, c.bNome)}
      </strong>
      <span className="secundario">
        {jogoUnico ? (
          placar(c.idaGolsA, c.idaGolsB)
        ) : (
          <>
            {textos.classificacao.ida}: {placar(c.idaGolsA, c.idaGolsB)} · {textos.classificacao.volta}: {placar(c.voltaGolsA, c.voltaGolsB)}
            {temVolta && ` · ${t.agregado}: ${placar((c.idaGolsA ?? 0) + (c.voltaGolsA ?? 0), (c.idaGolsB ?? 0) + (c.voltaGolsB ?? 0))}`}
          </>
        )}
        {c.penaltisA !== undefined && c.penaltisB !== undefined && ` (${t.penaltis(c.penaltisA, c.penaltisB)})`}
      </span>
    </li>
  )
}
