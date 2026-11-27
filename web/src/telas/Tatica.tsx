import type { DefinicaoTatica, Jogador, Tatica as TaticaDto } from '../nucleo/contrato'
import { textos } from '../textos'

interface Props {
  tatica: TaticaDto
  jogadores: Jogador[]
  erros: string[]
  /** Envia a tática nova; o núcleo valida e devolve as forças na posição. */
  aoMudar: (definicao: DefinicaoTatica) => void
  aoUsarAutomatica: () => void
}

const MAXIMO_RESERVAS = 7

export function Tatica({ tatica, jogadores, erros, aoMudar, aoUsarAutomatica }: Props) {
  const t = textos.tatica
  const porId = new Map(jogadores.map((j) => [j.id, j]))
  const titulares = tatica.titulares.map((x) => x.jogadorId)

  const enviar = (mudanca: Partial<DefinicaoTatica>) =>
    aoMudar({
      formacao: tatica.formacao,
      mentalidade: tatica.mentalidade,
      titulares,
      reservas: tatica.reservas,
      ...mudanca,
    })

  const trocarTitular = (indice: number, novoId: number) => {
    const novos = [...titulares]
    const jaEstava = novos.indexOf(novoId)
    if (jaEstava >= 0) {
      // Quem já era titular troca de lugar com quem sai.
      novos[jaEstava] = novos[indice]
    }
    novos[indice] = novoId
    enviar({ titulares: novos, reservas: tatica.reservas.filter((id) => !novos.includes(id)) })
  }

  const alternarReserva = (id: number) => {
    const reservas = tatica.reservas.includes(id)
      ? tatica.reservas.filter((r) => r !== id)
      : [...tatica.reservas, id].slice(0, MAXIMO_RESERVAS)
    enviar({ reservas })
  }

  const foraDoTime = jogadores.filter((j) => !titulares.includes(j.id))

  return (
    <>
      <header className="cabecalho cabecalho-acoes">
        <h1>{t.titulo}</h1>
        <button type="button" className="botao botao-secundario" onClick={aoUsarAutomatica}>
          {t.automatica}
        </button>
      </header>

      {erros.length > 0 && (
        <ul className="erro-lista" role="alert">
          {erros.map((e) => (
            <li key={e}>{e}</li>
          ))}
        </ul>
      )}

      <div className="controles-tatica">
        <label>
          {t.formacao}
          <select value={tatica.formacao} onChange={(e) => enviar({ formacao: e.target.value })}>
            {tatica.formacoes.map((f) => (
              <option key={f.nome} value={f.nome}>
                {f.nome}
              </option>
            ))}
          </select>
        </label>

        <fieldset className="mentalidades">
          <legend>{t.mentalidade}</legend>
          {tatica.mentalidades.map((m) => (
            <button
              key={m}
              type="button"
              className={`pilula ${m === tatica.mentalidade ? 'pilula-ativa' : ''}`}
              aria-pressed={m === tatica.mentalidade}
              onClick={() => enviar({ mentalidade: m })}
            >
              {t.mentalidades[m] ?? m}
            </button>
          ))}
        </fieldset>
      </div>

      <div className="grade-tatica">
        <section className="cartao">
          <h2>{t.titulares}</h2>
          <ol className="lista-titulares">
            {tatica.titulares.map((titular, i) => {
              const jogador = porId.get(titular.jogadorId)
              return (
                <li key={`${titular.posicao}-${i}`} className="linha-titular">
                  <span className="sigla-posicao">{titular.posicao}</span>
                  <select
                    aria-label={`${t.titulares} ${i + 1} (${titular.posicao})`}
                    value={titular.jogadorId}
                    onChange={(e) => trocarTitular(i, Number(e.target.value))}
                  >
                    {jogadores.map((j) => (
                      <option key={j.id} value={j.id} disabled={!j.disponivel && j.id !== titular.jogadorId}>
                        {j.nome} · {j.posicao} · {j.forca}
                        {!j.disponivel ? ' ✖' : ''}
                      </option>
                    ))}
                  </select>
                  <span className="forca-posicao" title={t.forcaNaPosicao}>
                    {titular.forcaNaPosicao}
                  </span>
                  {titular.improvisado && <span className="selo-alerta">{t.improvisado}</span>}
                  {jogador && !jogador.disponivel && <span className="selo-fora">✖</span>}
                </li>
              )
            })}
          </ol>
        </section>

        <section className="cartao">
          <h2>{t.reservas}</h2>
          <p className="secundario">{t.dicaReservas(MAXIMO_RESERVAS)}</p>
          <ul className="lista-reservas">
            {foraDoTime.map((j) => (
              <li key={j.id}>
                <label className={j.disponivel ? undefined : 'linha-indisponivel'}>
                  <input
                    type="checkbox"
                    checked={tatica.reservas.includes(j.id)}
                    disabled={!j.disponivel || (!tatica.reservas.includes(j.id) && tatica.reservas.length >= MAXIMO_RESERVAS)}
                    onChange={() => alternarReserva(j.id)}
                  />
                  <span className="sigla-posicao">{j.posicao}</span> {j.nome} · {j.forca}
                </label>
              </li>
            ))}
          </ul>
        </section>
      </div>
    </>
  )
}
