import { Escudo } from '../componentes/Escudo'
import type { ResumoMundo } from '../nucleo/contrato'
import { textos } from '../textos'

interface Props {
  mundo: ResumoMundo
  aoEscolher: (clubeId: string) => void
}

const formatoForca = new Intl.NumberFormat('pt-BR', { minimumFractionDigits: 1, maximumFractionDigits: 1 })

export function EscolherClube({ mundo, aoEscolher }: Props) {
  const t = textos.escolher
  return (
    <div className="escolher">
      <header className="cabecalho">
        <h1>{t.titulo}</h1>
        <p>{t.subtitulo}</p>
      </header>

      {mundo.divisoes.map((divisao) => (
        <section key={divisao.id} className="escolher-divisao">
          <h2>{divisao.nome}</h2>
          <div className="grade-clubes">
            {divisao.clubes.map((clube) => (
              <button
                key={clube.id}
                type="button"
                className="cartao-clube"
                onClick={() => aoEscolher(clube.id)}
                aria-label={t.comecar(clube.nome)}
              >
                <Escudo corPrimaria={clube.corPrimaria} corSecundaria={clube.corSecundaria} tamanho="grande" />
                <span className="cartao-clube-nome">{clube.nome}</span>
                <span className="secundario">
                  {clube.cidade}/{clube.uf}
                </span>
                <span className="cartao-clube-numeros">
                  {t.forca} <strong>{formatoForca.format(clube.forcaTop11)}</strong> · {t.torcida}{' '}
                  <strong>{clube.torcida}</strong>
                </span>
              </button>
            ))}
          </div>
        </section>
      ))}
    </div>
  )
}
