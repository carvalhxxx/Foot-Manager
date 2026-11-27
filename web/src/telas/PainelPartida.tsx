import type { ReactNode } from 'react'
import { Escudo } from '../componentes/Escudo'
import type { Estado } from '../nucleo/contrato'
import { formatoDinheiro, textos } from '../textos'

interface Props {
  estado: Estado
  caixa: number
  /** Botões da rodada (jogar ao vivo, simular rápido). */
  acoes: ReactNode
}

/**
 * Coluna lateral do jogo: só o que importa para a próxima partida (adversário, mando, rodada),
 * a situação na tabela, os últimos resultados e os avisos. O elenco fica na área principal.
 */
export function PainelPartida({ estado, caixa, acoes }: Props) {
  const t = textos.inicio
  const proximo = estado.proximoJogo

  return (
    <div className="painel-partida">
      <section className="cartao cartao-proximo" aria-label={t.proximoJogo}>
        <h2>{estado.temporadaEncerrada ? t.temporada(estado.ano) : t.proximoJogo}</h2>
        {proximo ? (
          <>
            <p className="proximo-jogo">
              <Escudo corPrimaria={proximo.adversario.corPrimaria} corSecundaria={proximo.adversario.corSecundaria} tamanho="grande" />
              <span>
                <strong className="proximo-nome">{proximo.adversario.nome}</strong>
                <span className="secundario">
                  {estado.divisaoNome} · {proximo.emCasa ? t.emCasa : t.fora}
                </span>
                <span className="secundario">{textos.partida.rodada(proximo.rodada)}</span>
              </span>
            </p>
            <p className="secundario">{t.forcaAdversario(proximo.adversario.forcaTop11.toFixed(1))}</p>
          </>
        ) : (
          <p className="secundario">{estado.temporadaEncerrada ? t.fimDaTemporada : t.semJogoAgora}</p>
        )}
        <div className="acoes-painel">{acoes}</div>
      </section>

      <section className="cartao">
        <dl className="resumo-painel">
          <dt>{t.posicao}</dt>
          <dd>
            {estado.rodadaAtual === 0 ? '–' : `${estado.posicao}º`} · {t.pontos(estado.pontos)}
          </dd>
          <dt>{t.rodadaLabel}</dt>
          <dd>{t.rodada(estado.rodadaAtual, estado.totalRodadas)}</dd>
          <dt>{t.caixa}</dt>
          <dd className={caixa < 0 ? 'negativo' : undefined}>{formatoDinheiro.format(caixa)}</dd>
        </dl>

        <h3 className="subtitulo-painel">{t.ultimos}</h3>
        {estado.ultimosResultados.length === 0 ? (
          <p className="secundario">{t.semJogos}</p>
        ) : (
          <ul className="ultimos">
            {estado.ultimosResultados.map((r) => {
              const tipo = r.golsPro > r.golsContra ? 'vitoria' : r.golsPro === r.golsContra ? 'empate' : 'derrota'
              return (
                <li key={r.rodada} className={`resultado resultado-${tipo}`} title={textos.partida.rodada(r.rodada)}>
                  <span className="resultado-letra">{t[tipo]}</span>
                  {r.golsPro}–{r.golsContra} {r.emCasa ? 'x' : '@'} {r.adversarioSigla}
                </li>
              )
            })}
          </ul>
        )}
      </section>

      {estado.janelaAberta && <p className="aviso-destaque">{t.janelaAberta}</p>}
      {estado.contratosAcabando > 0 && <p className="aviso-destaque">{t.contratosAcabando(estado.contratosAcabando)}</p>}
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
    </div>
  )
}
