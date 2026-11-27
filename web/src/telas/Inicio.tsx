import { Escudo } from '../componentes/Escudo'
import type { Estado } from '../nucleo/contrato'
import { textos } from '../textos'

interface Props {
  estado: Estado
}

export function Inicio({ estado }: Props) {
  const t = textos.inicio
  const proximo = estado.proximoJogo

  return (
    <>
      <header className="cabecalho cabecalho-clube">
        <Escudo corPrimaria={estado.clube.corPrimaria} corSecundaria={estado.clube.corSecundaria} tamanho="grande" />
        <div>
          <h1>{estado.clube.nome}</h1>
          <p>
            {t.temporada(estado.ano)} · {estado.divisaoNome} · {t.rodada(estado.rodadaAtual, estado.totalRodadas)}
          </p>
        </div>
      </header>

      {estado.temporadaEncerrada && <p className="aviso-destaque">{t.fimDaTemporada}</p>}

      <div className="painel">
        {proximo && (
          <section className="cartao">
            <h2>{t.proximoJogo}</h2>
            <p className="proximo-jogo">
              <Escudo corPrimaria={proximo.adversario.corPrimaria} corSecundaria={proximo.adversario.corSecundaria} />
              <strong>{proximo.adversario.nome}</strong>
            </p>
            <p className="secundario">
              {textos.partida.rodada(proximo.rodada)} · {proximo.emCasa ? t.emCasa : t.fora}
            </p>
          </section>
        )}

        <section className="cartao">
          <h2>{t.posicao}</h2>
          <p className="numero-grande">{estado.rodadaAtual === 0 ? '–' : `${estado.posicao}º`}</p>
          <p className="secundario">
            {estado.divisaoNome} · {t.pontos(estado.pontos)}
          </p>
        </section>

        <section className="cartao">
          <h2>{t.ultimos}</h2>
          {estado.ultimosResultados.length === 0 ? (
            <p className="secundario">{t.semJogos}</p>
          ) : (
            <ul className="ultimos">
              {estado.ultimosResultados.map((r) => {
                const tipo = r.golsPro > r.golsContra ? 'vitoria' : r.golsPro === r.golsContra ? 'empate' : 'derrota'
                return (
                  <li key={r.rodada} className={`resultado resultado-${tipo}`} title={`${textos.partida.rodada(r.rodada)}`}>
                    <span className="resultado-letra">{t[tipo]}</span>
                    {r.golsPro}–{r.golsContra} {r.emCasa ? 'x' : '@'} {r.adversarioSigla}
                  </li>
                )
              })}
            </ul>
          )}
        </section>
      </div>

      {estado.ajustesNaEscalacao.length > 0 && (
        <section className="cartao aviso-ajustes" role="note">
          <h2>{t.ajustes}</h2>
          <ul>
            {estado.ajustesNaEscalacao.map((a) => (
              <li key={a}>{a}</li>
            ))}
          </ul>
        </section>
      )}
    </>
  )
}
