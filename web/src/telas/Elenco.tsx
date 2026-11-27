import type { Jogador } from '../nucleo/contrato'
import { formatoDinheiro, textos } from '../textos'

interface Props {
  jogadores: Jogador[]
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

export function Elenco({ jogadores }: Props) {
  const t = textos.elenco
  return (
    <>
      <header className="cabecalho">
        <h1>{t.titulo}</h1>
      </header>
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
                    {j.nome}
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
                  <td className="numero ocultar-celular">{formatoDinheiro.format(j.salario)}</td>
                  <td className="numero ocultar-celular">{j.contratoAte}</td>
                </tr>
              )
            })}
          </tbody>
        </table>
      </div>
    </>
  )
}
