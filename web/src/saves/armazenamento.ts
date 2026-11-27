// Onde os saves ficam guardados no navegador. O conteúdo é texto opaco gerado pelo núcleo C#;
// aqui só guardamos e listamos (com um resumo para a tela "Continuar").

export interface ResumoSave {
  id: string
  clubeId: string
  clubeNome: string
  corPrimaria: string
  corSecundaria: string
  ano: number
  rodada: number
  divisao: string
  /** Data e hora do último salvamento (ISO 8601). */
  atualizadoEm: string
}

export interface ArmazenamentoSaves {
  /** Saves do mais recente para o mais antigo. */
  listar(): Promise<ResumoSave[]>
  guardar(resumo: ResumoSave, conteudo: string): Promise<void>
  lerConteudo(id: string): Promise<string | undefined>
  apagar(id: string): Promise<void>
}

const BANCO = 'foot-manager'
const RESUMOS = 'resumos'
const CONTEUDOS = 'conteudos'

function promessa<T>(pedido: IDBRequest<T>): Promise<T> {
  return new Promise((resolve, reject) => {
    pedido.onsuccess = () => resolve(pedido.result)
    pedido.onerror = () => reject(pedido.error)
  })
}

function concluida(transacao: IDBTransaction): Promise<void> {
  return new Promise((resolve, reject) => {
    transacao.oncomplete = () => resolve()
    transacao.onerror = () => reject(transacao.error)
    transacao.onabort = () => reject(transacao.error)
  })
}

const maisRecentePrimeiro = (a: ResumoSave, b: ResumoSave) => b.atualizadoEm.localeCompare(a.atualizadoEm)

/**
 * IndexedDB: resumos e conteúdos em "tabelas" separadas, para a lista não precisar carregar
 * centenas de KB de cada save.
 */
export function armazenamentoIndexedDb(): ArmazenamentoSaves {
  let banco: Promise<IDBDatabase> | undefined

  const abrir = () => {
    banco ??= new Promise((resolve, reject) => {
      const pedido = indexedDB.open(BANCO, 1)
      pedido.onupgradeneeded = () => {
        pedido.result.createObjectStore(RESUMOS, { keyPath: 'id' })
        pedido.result.createObjectStore(CONTEUDOS)
      }
      pedido.onsuccess = () => resolve(pedido.result)
      pedido.onerror = () => reject(pedido.error)
    })
    return banco
  }

  return {
    async listar() {
      const db = await abrir()
      const resumos = await promessa(db.transaction(RESUMOS).objectStore(RESUMOS).getAll() as IDBRequest<ResumoSave[]>)
      return resumos.sort(maisRecentePrimeiro)
    },
    async guardar(resumo, conteudo) {
      const db = await abrir()
      const transacao = db.transaction([RESUMOS, CONTEUDOS], 'readwrite')
      transacao.objectStore(RESUMOS).put(resumo)
      transacao.objectStore(CONTEUDOS).put(conteudo, resumo.id)
      await concluida(transacao)
    },
    async lerConteudo(id) {
      const db = await abrir()
      return promessa(db.transaction(CONTEUDOS).objectStore(CONTEUDOS).get(id) as IDBRequest<string | undefined>)
    },
    async apagar(id) {
      const db = await abrir()
      const transacao = db.transaction([RESUMOS, CONTEUDOS], 'readwrite')
      transacao.objectStore(RESUMOS).delete(id)
      transacao.objectStore(CONTEUDOS).delete(id)
      await concluida(transacao)
    },
  }
}

/** Versão em memória (testes e navegadores sem IndexedDB, como abas anônimas restritas). */
export function armazenamentoEmMemoria(): ArmazenamentoSaves {
  const resumos = new Map<string, ResumoSave>()
  const conteudos = new Map<string, string>()
  return {
    listar: () => Promise.resolve([...resumos.values()].sort(maisRecentePrimeiro)),
    guardar: (resumo, conteudo) => {
      resumos.set(resumo.id, resumo)
      conteudos.set(resumo.id, conteudo)
      return Promise.resolve()
    },
    lerConteudo: (id) => Promise.resolve(conteudos.get(id)),
    apagar: (id) => {
      resumos.delete(id)
      conteudos.delete(id)
      return Promise.resolve()
    },
  }
}

/** IndexedDB se existir; senão, memória (o jogo funciona, mas não guarda entre visitas). */
export function armazenamentoPadrao(): ArmazenamentoSaves {
  return typeof indexedDB === 'undefined' ? armazenamentoEmMemoria() : armazenamentoIndexedDb()
}
