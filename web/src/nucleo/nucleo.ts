import type { Resposta, ResumoMundo } from './contrato'

/** Funções do núcleo C# já com tipos de TypeScript. */
export interface Nucleo {
  versao(): string
  carregarDefinicoes(arquivos: Record<string, string>): Resposta<ResumoMundo>
}

// Formato bruto exportado por src/Jogo.Wasm/NucleoJs.cs ([JSExport]).
interface ApiBruta {
  Versao(): string
  CarregarDefinicoes(arquivosJson: string): string
}

interface RuntimeDotnet {
  getConfig(): { mainAssemblyName?: string }
  getAssemblyExports(assembly: string): Promise<{ Jogo: { Wasm: { NucleoJs: ApiBruta } } }>
}

interface ModuloDotnet {
  dotnet: { create(): Promise<RuntimeDotnet> }
}

let carregamento: Promise<Nucleo> | undefined

/**
 * Carrega o runtime do .NET e o núcleo do jogo (uma vez só; chamadas seguintes reaproveitam).
 * Os arquivos vêm de web/public/nucleo, gerados por `npm run nucleo`.
 */
export function carregarNucleo(): Promise<Nucleo> {
  carregamento ??= (async () => {
    const url = `${import.meta.env.BASE_URL}nucleo/_framework/dotnet.js`
    const { dotnet } = (await import(/* @vite-ignore */ url)) as ModuloDotnet
    const runtime = await dotnet.create()
    const assembly = runtime.getConfig().mainAssemblyName ?? 'Jogo.Wasm'
    const api = (await runtime.getAssemblyExports(assembly)).Jogo.Wasm.NucleoJs

    return {
      versao: () => api.Versao(),
      carregarDefinicoes: (arquivos) =>
        JSON.parse(api.CarregarDefinicoes(JSON.stringify(arquivos))) as Resposta<ResumoMundo>,
    }
  })()

  // Se falhar, permite tentar de novo depois.
  carregamento.catch(() => {
    carregamento = undefined
  })

  return carregamento
}
