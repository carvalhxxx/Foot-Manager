import { useState } from 'react'
import type { Financas as FinancasDto } from '../nucleo/contrato'
import { formatoDinheiro, textos } from '../textos'

interface Props {
  financas: FinancasDto
  erros: string[]
  aoDefinirPreco: (preco: number | undefined) => void
}

const formatoNumero = new Intl.NumberFormat('pt-BR')

export function Financas({ financas, erros, aoDefinirPreco }: Props) {
  const t = textos.financas
  const [preco, setPreco] = useState(String(financas.precoIngresso))
  const dinheiro = (v: number) => formatoDinheiro.format(v)

  return (
    <>
      <header className="cabecalho">
        <h1>{t.titulo}</h1>
      </header>

      <div className="painel">
        <section className="cartao">
          <h2>{t.caixa}</h2>
          <p className={`numero-grande ${financas.caixa < 0 ? 'negativo' : ''}`}>{dinheiro(financas.caixa)}</p>
          <p className="secundario">
            {t.resultado}:{' '}
            <strong className={financas.resultadoDaTemporada < 0 ? 'negativo' : 'positivo'}>
              {dinheiro(financas.resultadoDaTemporada)}
            </strong>
          </p>
        </section>

        <section className="cartao">
          <h2>{t.folha}</h2>
          <p className="numero-grande">{dinheiro(financas.folhaMensal)}</p>
          <p className="secundario">
            {t.receitasFixas}: {t.tv} {dinheiro(financas.cotaTvAnual)} · {t.patrocinio} {dinheiro(financas.patrocinioAnual)}
          </p>
        </section>

        <section className="cartao">
          <h2>{t.ingresso}</h2>
          <form
            className="form-ingresso"
            onSubmit={(e) => {
              e.preventDefault()
              aoDefinirPreco(Number(preco.replace(',', '.')))
            }}
          >
            <label className="campo-dinheiro">
              R$
              <input
                type="number"
                inputMode="decimal"
                value={preco}
                aria-label={t.ingresso}
                onChange={(e) => setPreco(e.target.value)}
              />
            </label>
            <button type="submit" className="botao">
              {t.aplicar}
            </button>
            {financas.precoPersonalizado && (
              <button
                type="button"
                className="botao botao-secundario"
                onClick={() => {
                  setPreco(String(financas.precoSugerido))
                  aoDefinirPreco(undefined)
                }}
              >
                {t.usarSugerido}
              </button>
            )}
          </form>
          <p className="secundario">{t.sugerido(dinheiro(financas.precoSugerido))}</p>
          {financas.ultimoPublico !== undefined && financas.ultimaRenda !== undefined && (
            <p className="secundario">
              {t.ultimoJogo(formatoNumero.format(financas.ultimoPublico), formatoNumero.format(financas.capacidade), dinheiro(financas.ultimaRenda))}
            </p>
          )}
          <p className="secundario dica">{t.dicaIngresso}</p>
          {erros.length > 0 && (
            <ul className="erro-lista" role="alert">
              {erros.map((e) => (
                <li key={e}>{e}</li>
              ))}
            </ul>
          )}
        </section>
      </div>

      <section className="cartao">
        <h2>{t.extrato}</h2>
        {financas.extrato.length === 0 ? (
          <p className="secundario">{t.semLancamentos}</p>
        ) : (
          <table className="tabela">
            <tbody>
              {financas.extrato.map((l) => (
                <tr key={l.categoria}>
                  <th scope="row">{t.categorias[l.categoria] ?? l.categoria}</th>
                  <td className={`numero ${l.valor < 0 ? 'negativo' : 'positivo'}`}>{dinheiro(l.valor)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </section>
    </>
  )
}
