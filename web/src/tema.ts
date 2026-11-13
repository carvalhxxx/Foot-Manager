import { useCallback, useEffect, useState } from 'react'

export type Tema = 'claro' | 'escuro'

const CHAVE = 'foot-manager:tema'

/** Tema salvo pelo jogador; senão, o do sistema operacional. */
export function temaInicial(): Tema {
  try {
    const salvo = localStorage.getItem(CHAVE)
    if (salvo === 'claro' || salvo === 'escuro') {
      return salvo
    }
  } catch {
    // Armazenamento indisponível (aba anônima, bloqueio): segue com o tema do sistema.
  }

  return window.matchMedia?.('(prefers-color-scheme: dark)').matches ? 'escuro' : 'claro'
}

export function useTema(): [Tema, () => void] {
  const [tema, setTema] = useState<Tema>(temaInicial)

  useEffect(() => {
    document.documentElement.dataset.tema = tema
  }, [tema])

  const alternar = useCallback(() => {
    setTema((atual) => {
      const novo = atual === 'escuro' ? 'claro' : 'escuro'
      try {
        localStorage.setItem(CHAVE, novo)
      } catch {
        // Sem armazenamento: o tema vale só nesta visita.
      }
      return novo
    })
  }, [])

  return [tema, alternar]
}
