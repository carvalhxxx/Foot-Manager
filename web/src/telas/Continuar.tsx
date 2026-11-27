import { useState } from 'react'
import { Escudo } from '../componentes/Escudo'
import type { ResumoSave } from '../saves/armazenamento'
import { formatoDataHora } from '../saves/salvar'
import { textos } from '../textos'

interface Props {
  saves: ResumoSave[]
  erro?: string
  aoContinuar: (id: string) => void
  aoApagar: (id: string) => void
  aoNovoJogo: () => void
  aoImportar: (conteudo: string) => void
}

export function Continuar({ saves, erro, aoContinuar, aoApagar, aoNovoJogo, aoImportar }: Props) {
  const t = textos.saves
  const [confirmando, setConfirmando] = useState<string | undefined>()

  const importar = async (arquivo: File | undefined) => {
    if (arquivo) {
      aoImportar(await arquivo.text())
    }
  }

  return (
    <div className="continuar">
      <header className="cabecalho cabecalho-acoes">
        <div>
          <h1>{t.titulo}</h1>
          <p>{t.subtitulo}</p>
        </div>
        <div className="acoes-rodada">
          <label className="botao botao-secundario botao-arquivo">
            {t.importar}
            <input type="file" accept=".json,application/json" onChange={(e) => void importar(e.target.files?.[0])} />
          </label>
          <button type="button" className="botao botao-grande" onClick={aoNovoJogo}>
            {t.novoJogo}
          </button>
        </div>
      </header>

      {erro && (
        <p className="erro-lista" role="alert">
          {erro}
        </p>
      )}

      <ul className="lista-saves">
        {saves.map((s) => (
          <li key={s.id} className="cartao save">
            <Escudo corPrimaria={s.corPrimaria} corSecundaria={s.corSecundaria} tamanho="grande" />
            <div className="save-info">
              <strong>{s.clubeNome}</strong>
              <span className="secundario">{t.resumo(s.ano, s.divisao, s.rodada)}</span>
              <span className="secundario">{formatoDataHora.format(new Date(s.atualizadoEm))}</span>
            </div>
            <div className="acoes-rodada">
              {confirmando === s.id ? (
                <button type="button" className="botao botao-perigo" onClick={() => aoApagar(s.id)}>
                  {t.confirmarApagar}
                </button>
              ) : (
                <button type="button" className="botao botao-secundario" onClick={() => setConfirmando(s.id)}>
                  {t.apagar}
                </button>
              )}
              <button type="button" className="botao" onClick={() => aoContinuar(s.id)} aria-label={`${t.continuar}: ${s.clubeNome}`}>
                {t.continuar}
              </button>
            </div>
          </li>
        ))}
      </ul>
    </div>
  )
}
