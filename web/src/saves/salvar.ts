import type { SaveExportado } from '../nucleo/contrato'
import { descreverErros } from '../nucleo/iniciar'
import type { Nucleo } from '../nucleo/nucleo'
import { textos } from '../textos'
import type { ArmazenamentoSaves, ResumoSave } from './armazenamento'

export function resumoDe(id: string, save: SaveExportado, agora: Date): ResumoSave {
  return {
    id,
    clubeId: save.clubeId,
    clubeNome: save.clubeNome,
    corPrimaria: save.corPrimaria,
    corSecundaria: save.corSecundaria,
    ano: save.ano,
    rodada: save.rodada,
    divisao: save.divisao,
    atualizadoEm: agora.toISOString(),
  }
}

/** Gera o save no núcleo e guarda no navegador. Devolve a mensagem de erro, se houver. */
export async function salvarCarreira(
  nucleo: Nucleo,
  armazenamento: ArmazenamentoSaves,
  id: string,
  agora = new Date(),
): Promise<string | undefined> {
  const r = nucleo.exportarSave()
  if (!r.ok) {
    return descreverErros(r.erros).join(' ')
  }

  try {
    await armazenamento.guardar(resumoDe(id, r.dados, agora), r.dados.conteudo)
    return undefined
  } catch {
    return textos.saves.erroAoSalvar
  }
}

/** Oferece o save como arquivo para o jogador baixar (backup ou levar para outro aparelho). */
export function baixarSave(save: SaveExportado): void {
  const arquivo = new Blob([save.conteudo], { type: 'application/json' })
  const url = URL.createObjectURL(arquivo)
  const link = document.createElement('a')
  link.href = url
  link.download = textos.saves.nomeDoArquivo(save.clubeId, save.ano, save.rodada)
  link.click()
  URL.revokeObjectURL(url)
}

export const formatoHora = new Intl.DateTimeFormat('pt-BR', { hour: '2-digit', minute: '2-digit' })
export const formatoDataHora = new Intl.DateTimeFormat('pt-BR', { dateStyle: 'short', timeStyle: 'short' })
