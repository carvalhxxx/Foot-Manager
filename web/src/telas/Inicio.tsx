import type { ResumoMundo } from '../nucleo/contrato'
import { textos } from '../textos'

interface Props {
  mundo: ResumoMundo
  versao: string
}

const formatoForca = new Intl.NumberFormat('pt-BR', { minimumFractionDigits: 1, maximumFractionDigits: 1 })
const formatoInteiro = new Intl.NumberFormat('pt-BR')

export function Inicio({ mundo, versao }: Props) {
  const t = textos.inicio
  const totalClubes = mundo.divisoes.reduce((soma, d) => soma + d.clubes.length, 0)

  return (
    <>
      <header className="cabecalho">
        <h1>{t.titulo}</h1>
        <p>{t.subtitulo}</p>
      </header>

      <dl className="numeros">
        <Numero rotulo={t.temporada} valor={String(mundo.temporada)} />
        <Numero rotulo={t.clubes} valor={formatoInteiro.format(totalClubes)} />
        <Numero rotulo={t.jogadores} valor={formatoInteiro.format(mundo.totalJogadores)} />
        <Numero rotulo={t.versaoNucleo} valor={versao.split('+')[0]} />
      </dl>

      <div className="divisoes">
        {mundo.divisoes.map((divisao) => (
          <section key={divisao.id} className="cartao">
            <h2>{divisao.nome}</h2>
            <table className="tabela">
              <thead>
                <tr>
                  <th scope="col">{t.colunaClube}</th>
                  <th scope="col">{t.colunaCidade}</th>
                  <th scope="col" className="numero" title={t.dicaForca}>
                    {t.colunaForca}
                  </th>
                </tr>
              </thead>
              <tbody>
                {divisao.clubes.map((clube) => (
                  <tr key={clube.id}>
                    <td>
                      <span
                        className="escudo"
                        style={{ background: clube.corPrimaria, borderColor: clube.corSecundaria }}
                        aria-hidden="true"
                      />
                      {clube.nome}
                    </td>
                    <td className="secundario">
                      {clube.cidade}/{clube.uf}
                    </td>
                    <td className="numero">{formatoForca.format(clube.forcaTop11)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </section>
        ))}
      </div>
    </>
  )
}

function Numero({ rotulo, valor }: { rotulo: string; valor: string }) {
  return (
    <div className="numero-destaque">
      <dt>{rotulo}</dt>
      <dd>{valor}</dd>
    </div>
  )
}
