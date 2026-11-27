import { Escudo } from '../componentes/Escudo'
import type { Classificacao as ClassificacaoDto } from '../nucleo/contrato'
import { textos } from '../textos'

interface Props {
  divisoes: { id: string; nome: string }[]
  selecionada: string
  aoSelecionar: (divisaoId: string) => void
  classificacao: ClassificacaoDto
  meuClubeId: string
}

function zona(posicao: number, total: number, c: ClassificacaoDto): string | undefined {
  if (c.vagasAcesso === 0 && posicao === 1) {
    return 'zona-campeao'
  }
  if (posicao <= c.vagasDiretas) {
    return 'zona-acesso'
  }
  if (posicao <= c.vagasDiretas + 2 * (c.vagasAcesso - c.vagasDiretas)) {
    return 'zona-playoff'
  }
  if (posicao > total - c.vagasRebaixamento) {
    return 'zona-rebaixamento'
  }
  return undefined
}

export function Classificacao({ divisoes, selecionada, aoSelecionar, classificacao, meuClubeId }: Props) {
  const t = textos.classificacao
  const total = classificacao.linhas.length

  return (
    <>
      <header className="cabecalho cabecalho-acoes">
        <h1>{t.titulo}</h1>
        <div className="abas" role="tablist">
          {divisoes.map((d) => (
            <button
              key={d.id}
              type="button"
              role="tab"
              aria-selected={d.id === selecionada}
              className={`pilula ${d.id === selecionada ? 'pilula-ativa' : ''}`}
              onClick={() => aoSelecionar(d.id)}
            >
              {d.nome}
            </button>
          ))}
        </div>
      </header>

      <div className="grade-classificacao">
        <section className="cartao rolagem">
          <table className="tabela tabela-classificacao">
            <thead>
              <tr>
                <th scope="col" className="numero">#</th>
                <th scope="col">{t.clube}</th>
                <th scope="col" className="numero">{t.pontos}</th>
                <th scope="col" className="numero">{t.jogos}</th>
                <th scope="col" className="numero">{t.vitorias}</th>
                <th scope="col" className="numero">{t.empates}</th>
                <th scope="col" className="numero">{t.derrotas}</th>
                <th scope="col" className="numero ocultar-celular">{t.golsPro}</th>
                <th scope="col" className="numero ocultar-celular">{t.golsContra}</th>
                <th scope="col" className="numero">{t.saldo}</th>
              </tr>
            </thead>
            <tbody>
              {classificacao.linhas.map((l) => (
                <tr
                  key={l.clubeId}
                  className={[zona(l.posicao, total, classificacao), l.clubeId === meuClubeId ? 'meu-clube' : '']
                    .filter(Boolean)
                    .join(' ')}
                >
                  <td className="numero">{l.posicao}</td>
                  <td>
                    <Escudo corPrimaria={l.corPrimaria} corSecundaria={l.corSecundaria} />
                    <span className="nome-completo">{l.nome}</span>
                    <span className="nome-curto">{l.sigla}</span>
                  </td>
                  <td className="numero">
                    <strong>{l.pontos}</strong>
                  </td>
                  <td className="numero">{l.jogos}</td>
                  <td className="numero">{l.vitorias}</td>
                  <td className="numero">{l.empates}</td>
                  <td className="numero">{l.derrotas}</td>
                  <td className="numero ocultar-celular">{l.golsPro}</td>
                  <td className="numero ocultar-celular">{l.golsContra}</td>
                  <td className="numero">{l.saldo > 0 ? `+${l.saldo}` : l.saldo}</td>
                </tr>
              ))}
            </tbody>
          </table>
          <p className="legenda">
            {classificacao.vagasAcesso === 0 ? (
              <span className="legenda-item zona-campeao">{t.legendaCampeao}</span>
            ) : classificacao.vagasDiretas < classificacao.vagasAcesso ? (
              <>
                <span className="legenda-item zona-acesso">{t.legendaAcessoDireto}</span>
                <span className="legenda-item zona-playoff">{t.legendaPlayoff}</span>
              </>
            ) : (
              <span className="legenda-item zona-acesso">{t.legendaAcesso}</span>
            )}
            {classificacao.vagasRebaixamento > 0 && (
              <span className="legenda-item zona-rebaixamento">{t.legendaRebaixamento}</span>
            )}
          </p>
        </section>

        {classificacao.vagasDiretas < classificacao.vagasAcesso && (
          <section className="cartao" aria-label={t.playoff}>
            <h2>{t.playoff}</h2>
            <p className="secundario dica">
              {t.regraPlayoff(
                classificacao.vagasDiretas,
                classificacao.vagasDiretas + 1,
                classificacao.vagasDiretas + 2 * (classificacao.vagasAcesso - classificacao.vagasDiretas),
              )}
            </p>
            {classificacao.playoff.length === 0 ? (
              <p className="secundario">{t.aguardando}</p>
            ) : (
              <ul className="lista-simples playoff">
                {classificacao.playoff.map((c) => (
                  <li key={c.melhorSigla}>
                    <strong>
                      {c.melhorPosicao}º {c.melhorNome} × {c.piorPosicao}º {c.piorNome}
                    </strong>
                    <span className="secundario">
                      {t.ida}: {c.idaGolsPior ?? '–'} × {c.idaGolsMelhor ?? '–'} ({c.piorSigla} em casa) · {t.volta}: {c.voltaGolsMelhor ?? '–'} ×{' '}
                      {c.voltaGolsPior ?? '–'} ({c.melhorSigla} em casa)
                    </span>
                    {c.vencedorNome && <span className="positivo">{t.classificado(c.vencedorNome)}</span>}
                  </li>
                ))}
              </ul>
            )}
          </section>
        )}

        <section className="cartao">
          <h2>{t.artilharia}</h2>
          <ol className="artilharia">
            {classificacao.artilharia.map((a) => (
              <li key={`${a.jogador}-${a.clubeSigla}`}>
                <span className="artilheiro-linha">
                  <span>
                    {a.jogador} <span className="secundario">{a.clubeSigla}</span>
                  </span>
                  <strong>{a.gols}</strong>
                </span>
              </li>
            ))}
          </ol>
        </section>
      </div>
    </>
  )
}
