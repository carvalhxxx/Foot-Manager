import { useState } from 'react'
import type { BuscaMercado, JogadorMercado, Mercado as MercadoDto } from '../nucleo/contrato'
import { formatoDinheiro, textos } from '../textos'

interface Props {
  buscar: (busca: BuscaMercado) => MercadoDto
  erros: string[]
  mensagem?: string
  aoComprar: (jogador: JogadorMercado) => void
}

const dinheiro = (v: number) => formatoDinheiro.format(v)

interface PropsNegociacao {
  jogador: JogadorMercado
  aoConfirmar: () => void
  aoCancelar: () => void
}

function Negociacao({ jogador, aoConfirmar, aoCancelar }: PropsNegociacao) {
  const t = textos.mercado
  return (
    <section className="cartao cartao-contrato" aria-label={t.negociar(jogador.nome)}>
      <h2>{jogador.nome}</h2>
      <p>
        {jogador.livre
          ? t.propostaLivre(jogador.nome)
          : t.proposta(jogador.nome, jogador.clubeNome ?? '', dinheiro(jogador.preco))}
      </p>
      <p className="secundario">{t.contrato(dinheiro(jogador.salarioPedido), jogador.anosDeContrato)}</p>
      {jogador.impedimento && (
        <p className="erro-lista" role="alert">
          {jogador.impedimento}
        </p>
      )}
      <div className="acoes">
        <button type="button" className="botao" disabled={jogador.impedimento !== undefined} onClick={aoConfirmar}>
          {t.confirmar}
        </button>
        <button type="button" className="botao botao-secundario" onClick={aoCancelar}>
          {t.cancelar}
        </button>
      </div>
    </section>
  )
}

export function Mercado({ buscar, erros, mensagem, aoComprar }: Props) {
  const t = textos.mercado
  const [posicao, setPosicao] = useState('')
  const [forcaMinima, setForcaMinima] = useState('')
  const [precoMaximo, setPrecoMaximo] = useState('')
  const [apenasLivres, setApenasLivres] = useState(false)
  const [busca, setBusca] = useState<BuscaMercado>({ forcaMinima: 0, apenasLivres: false })
  const [selecionado, setSelecionado] = useState<number>()

  const mercado = buscar(busca)
  const escolhido = mercado.jogadores.find((j) => j.id === selecionado)

  return (
    <>
      <header className="cabecalho">
        <h1>{t.titulo}</h1>
        <p>
          {mercado.janelaAberta ? t.janelaAberta : t.janelaFechada} · {t.caixa(dinheiro(mercado.caixa))}
        </p>
      </header>

      {mensagem && (
        <p className="aviso-destaque" aria-live="polite">
          {mensagem}
        </p>
      )}
      {erros.length > 0 && (
        <ul className="erro-lista" role="alert">
          {erros.map((e) => (
            <li key={e}>{e}</li>
          ))}
        </ul>
      )}

      {escolhido && (
        <Negociacao
          key={escolhido.id}
          jogador={escolhido}
          aoConfirmar={() => {
            setSelecionado(undefined)
            aoComprar(escolhido)
          }}
          aoCancelar={() => setSelecionado(undefined)}
        />
      )}

      <form
        className="cartao form-busca"
        onSubmit={(e) => {
          e.preventDefault()
          const preco = Number(precoMaximo.replace(',', '.'))
          setBusca({
            posicao: posicao || undefined,
            forcaMinima: Number(forcaMinima) || 0,
            precoMaximo: precoMaximo && preco > 0 ? preco : undefined,
            apenasLivres,
          })
        }}
      >
        <label>
          {t.posicao}{' '}
          <select value={posicao} onChange={(e) => setPosicao(e.target.value)}>
            <option value="">{t.todas}</option>
            {mercado.posicoes.map((p) => (
              <option key={p} value={p}>
                {p}
              </option>
            ))}
          </select>
        </label>
        <label>
          {t.forcaMinima}{' '}
          <input type="number" inputMode="numeric" value={forcaMinima} onChange={(e) => setForcaMinima(e.target.value)} />
        </label>
        <label>
          {t.precoMaximo}{' '}
          <input type="number" inputMode="decimal" value={precoMaximo} onChange={(e) => setPrecoMaximo(e.target.value)} />
        </label>
        <label>
          <input type="checkbox" checked={apenasLivres} onChange={(e) => setApenasLivres(e.target.checked)} /> {t.apenasLivres}
        </label>
        <button type="submit" className="botao">
          {t.buscar}
        </button>
      </form>

      <div className="cartao rolagem">
        {mercado.jogadores.length === 0 ? (
          <p className="secundario">{t.nenhum}</p>
        ) : (
          <table className="tabela">
            <thead>
              <tr>
                <th scope="col">{textos.elenco.pos}</th>
                <th scope="col">{textos.elenco.nome}</th>
                <th scope="col" className="numero">{textos.elenco.idade}</th>
                <th scope="col" className="numero">{textos.elenco.forca}</th>
                <th scope="col">{t.clube}</th>
                <th scope="col" className="numero">{t.preco}</th>
                <th scope="col" className="numero ocultar-celular">{t.salario}</th>
              </tr>
            </thead>
            <tbody>
              {mercado.jogadores.map((j) => (
                <tr key={j.id}>
                  <td>
                    <span className="sigla-posicao">{j.posicao}</span>
                  </td>
                  <td>
                    <button type="button" className="link-jogador" aria-label={t.negociar(j.nome)} onClick={() => setSelecionado(j.id)}>
                      {j.nome}
                    </button>
                    {j.estrela && <span className="estrela">★</span>}
                  </td>
                  <td className="numero">{j.idade}</td>
                  <td className="numero">
                    <strong>{j.forca}</strong>
                  </td>
                  <td>{j.livre ? <span className="situacao-ok">{t.livre}</span> : j.clubeSigla}</td>
                  <td className={`numero ${j.impedimento ? 'secundario' : ''}`}>{dinheiro(j.preco)}</td>
                  <td className="numero ocultar-celular">{dinheiro(j.salarioPedido)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </>
  )
}
