import { useState } from 'react'
import type { Jogador, Oferta } from '../nucleo/contrato'
import { formatoDinheiro, textos } from '../textos'

interface Props {
  jogadores: Jogador[]
  erros: string[]
  aoRenovar: (jogadorId: number, anos: number) => void
  aoDispensar: (jogadorId: number) => void
  consultarOferta: (jogadorId: number) => Oferta | undefined
  aoVender: (jogadorId: number) => void
}

const dinheiro = (v: number) => formatoDinheiro.format(v)

interface PropsContrato {
  jogador: Jogador
  erros: string[]
  aoRenovar: (anos: number) => void
  aoDispensar: () => void
  consultarOferta: () => Oferta | undefined
  aoVender: () => void
  aoFechar: () => void
}

function CartaoContrato({ jogador, erros, aoRenovar, aoDispensar, consultarOferta, aoVender, aoFechar }: PropsContrato) {
  const t = textos.elenco
  const [anos, setAnos] = useState(2)
  const [confirmando, setConfirmando] = useState(false)
  const [oferta, setOferta] = useState<Oferta>()

  return (
    <section className="cartao cartao-contrato" aria-label={jogador.nome}>
      <h2>{jogador.nome}</h2>
      <p className="secundario">
        {t.contratoAte(jogador.contratoAte)} · {t.salarioAtual(dinheiro(jogador.salario))} ·{' '}
        {t.valorDeMercado(dinheiro(jogador.valorDeMercado))}
      </p>
      {jogador.podeRenovar && (
        <form
          className="form-ingresso"
          onSubmit={(e) => {
            e.preventDefault()
            aoRenovar(anos)
          }}
        >
          <span>{t.pedido(dinheiro(jogador.salarioPedido))}</span>
          <label>
            {t.anos}{' '}
            <select value={anos} onChange={(e) => setAnos(Number(e.target.value))}>
              {[1, 2, 3, 4].map((n) => (
                <option key={n} value={n}>
                  {t.opcaoAnos(n)}
                </option>
              ))}
            </select>
          </label>
          <button type="submit" className="botao">
            {t.renovar}
          </button>
        </form>
      )}
      {jogador.podeRenovar && <p className="secundario dica">{t.semRenovacao}</p>}
      <p className="secundario">{t.custoDispensa(dinheiro(jogador.custoDaDispensa))}</p>
      {oferta && (
        <p className="secundario">
          {!oferta.janelaAberta
            ? t.janelaFechada
            : oferta.clubeNome
              ? t.proposta(oferta.clubeNome, dinheiro(oferta.valor))
              : t.semProposta}
        </p>
      )}
      <div className="acoes">
        {oferta?.janelaAberta && oferta.clubeNome ? (
          <button type="button" className="botao" onClick={aoVender}>
            {t.vender(dinheiro(oferta.valor))}
          </button>
        ) : (
          <button type="button" className="botao botao-secundario" onClick={() => setOferta(consultarOferta())}>
            {t.verPropostas}
          </button>
        )}
        {confirmando ? (
          <>
            <button type="button" className="botao botao-perigo" onClick={aoDispensar}>
              {t.confirmarDispensa(jogador.nome)}
            </button>
            <button type="button" className="botao botao-secundario" onClick={() => setConfirmando(false)}>
              {t.cancelar}
            </button>
          </>
        ) : (
          <button type="button" className="botao botao-secundario" onClick={() => setConfirmando(true)}>
            {t.dispensar}
          </button>
        )}
        <button type="button" className="botao botao-secundario" onClick={aoFechar}>
          {t.fechar}
        </button>
      </div>
      {erros.length > 0 && (
        <ul className="erro-lista" role="alert">
          {erros.map((e) => (
            <li key={e}>{e}</li>
          ))}
        </ul>
      )}
    </section>
  )
}

function situacao(j: Jogador): { texto: string; classe: string } {
  const t = textos.elenco
  if (j.rodadasLesionado > 0) {
    return { texto: t.lesionado(j.rodadasLesionado), classe: 'situacao-fora' }
  }
  if (j.rodadasSuspenso > 0) {
    return { texto: t.suspenso, classe: 'situacao-fora' }
  }
  if (j.amarelos >= 2) {
    return { texto: t.pendurado(j.amarelos), classe: 'situacao-alerta' }
  }
  return { texto: t.disponivel, classe: 'situacao-ok' }
}

export function Elenco({ jogadores, erros, aoRenovar, aoDispensar, consultarOferta, aoVender }: Props) {
  const t = textos.elenco
  const [selecionado, setSelecionado] = useState<number>()
  const escolhido = jogadores.find((j) => j.id === selecionado)
  return (
    <>
      <header className="cabecalho">
        <h1>{t.titulo}</h1>
      </header>
      {escolhido && (
        <CartaoContrato
          key={escolhido.id}
          jogador={escolhido}
          erros={erros}
          aoRenovar={(anos) => aoRenovar(escolhido.id, anos)}
          aoDispensar={() => aoDispensar(escolhido.id)}
          consultarOferta={() => consultarOferta(escolhido.id)}
          aoVender={() => aoVender(escolhido.id)}
          aoFechar={() => setSelecionado(undefined)}
        />
      )}
      <div className="cartao rolagem">
        <table className="tabela">
          <thead>
            <tr>
              <th scope="col">{t.pos}</th>
              <th scope="col">{t.nome}</th>
              <th scope="col" className="numero">{t.idade}</th>
              <th scope="col" className="numero">{t.forca}</th>
              <th scope="col">{t.condicao}</th>
              <th scope="col" className="numero">{t.moral}</th>
              <th scope="col" className="numero">{t.jogos}</th>
              <th scope="col" className="numero">{t.gols}</th>
              <th scope="col">{t.situacao}</th>
              <th scope="col" className="numero ocultar-celular">{t.salario}</th>
              <th scope="col" className="numero ocultar-celular">{t.contrato}</th>
              <th scope="col" className="numero ocultar-celular">{t.valor}</th>
            </tr>
          </thead>
          <tbody>
            {jogadores.map((j) => {
              const s = situacao(j)
              return (
                <tr key={j.id} className={j.disponivel ? undefined : 'linha-indisponivel'}>
                  <td>
                    <span className="sigla-posicao">{j.posicao}</span>
                  </td>
                  <td>
                    <button type="button" className="link-jogador" aria-label={t.detalhes(j.nome)} onClick={() => setSelecionado(j.id)}>
                      {j.nome}
                    </button>
                    {j.estrela && (
                      <span className="estrela" title={t.estrela} aria-label={t.estrela}>
                        ★
                      </span>
                    )}
                    {j.posicoesSecundarias.length > 0 && (
                      <span className="secundario"> · {j.posicoesSecundarias.join(', ')}</span>
                    )}
                  </td>
                  <td className="numero">{j.idade}</td>
                  <td className="numero">
                    <strong>{j.forca}</strong>
                  </td>
                  <td>
                    <span className="barra" aria-label={`${j.condicao}%`}>
                      <span
                        className={`barra-preenchida ${j.condicao < 70 ? 'barra-baixa' : ''}`}
                        style={{ width: `${j.condicao}%` }}
                      />
                    </span>
                  </td>
                  <td className="numero">{j.moral}</td>
                  <td className="numero">{j.jogos}</td>
                  <td className="numero">{j.gols}</td>
                  <td className={s.classe}>{s.texto}</td>
                  <td className="numero ocultar-celular">{dinheiro(j.salario)}</td>
                  <td className="numero ocultar-celular">
                    {j.contratoAte}
                    {j.podeRenovar && <span className="situacao-alerta"> · {t.ultimoAno}</span>}
                  </td>
                  <td className="numero ocultar-celular">{dinheiro(j.valorDeMercado)}</td>
                </tr>
              )
            })}
          </tbody>
        </table>
      </div>
    </>
  )
}
