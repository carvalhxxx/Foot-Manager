import type { FimDeTemporada as FimDto } from '../nucleo/contrato'
import { textos } from '../textos'

interface Props {
  fim: FimDto
  aoContinuar: () => void
}

export function FimDeTemporada({ fim, aoContinuar }: Props) {
  const t = textos.fim
  const destaque =
    fim.movimento === 'titulo' ? t.conquistouTitulo
    : fim.movimento === 'acesso' ? t.acesso
    : fim.movimento === 'rebaixamento' ? t.rebaixamento
    : undefined

  return (
    <>
      <header className="cabecalho">
        <h1>{t.titulo(fim.ano)}</h1>
        <p>{t.suaCampanha(fim.minhaPosicao, fim.minhaDivisao)}</p>
        {destaque && <p className={`destaque-fim destaque-${fim.movimento}`}>{destaque}</p>}
      </header>

      <div className="painel">
        {fim.divisoes.map((d) => (
          <section key={d.nome} className="cartao">
            <h2>{d.nome}</h2>
            <dl className="resumo-fim">
              <dt>{t.campeao}</dt>
              <dd>{d.campeao}</dd>
              {d.promovidos.length > 0 && (
                <>
                  <dt>{t.sobem}</dt>
                  <dd>{d.promovidos.join(', ')}</dd>
                </>
              )}
              {d.rebaixados.length > 0 && (
                <>
                  <dt>{t.descem}</dt>
                  <dd>{d.rebaixados.join(', ')}</dd>
                </>
              )}
              {d.artilheiro && (
                <>
                  <dt>{t.artilheiro}</dt>
                  <dd>
                    {d.artilheiro.jogador} ({d.artilheiro.clubeSigla}) · {d.artilheiro.gols}
                  </dd>
                </>
              )}
            </dl>
          </section>
        ))}
      </div>

      <p className="secundario">{t.viradaDeAno(fim.aposentados, fim.revelacoes)}</p>
      <button type="button" className="botao botao-grande" onClick={aoContinuar}>
        {t.comecar(fim.proximoAno)}
      </button>
    </>
  )
}
