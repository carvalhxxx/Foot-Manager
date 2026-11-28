import type {
  AoVivo,
  Classificacao,
  Copa,
  Decisao,
  DefinicaoTatica,
  Estado,
  FimDeTemporada,
  Financas,
  BuscaMercado,
  Mercado,
  Oferta,
  Jogador,
  Resposta,
  ResumoMundo,
  Rodada,
  SaveExportado,
  Tatica,
} from './contrato'

/**
 * Tudo o que a interface pode pedir ao núcleo C#. As telas recebem esta interface por parâmetro,
 * então os testes usam uma versão falsa, sem WebAssembly.
 */
export interface Nucleo {
  versao(): string
  carregarDefinicoes(arquivos: Record<string, string>): Resposta<ResumoMundo>
  novaCarreira(clubeId: string, seed: string): Resposta<Estado>
  estado(): Resposta<Estado>
  elenco(): Resposta<Jogador[]>
  renovarContrato(jogadorId: number, anos: number): Resposta<Jogador[]>
  dispensar(jogadorId: number): Resposta<Jogador[]>
  mercado(busca: BuscaMercado): Resposta<Mercado>
  comprar(jogadorId: number): Resposta<Jogador[]>
  ofertaPor(jogadorId: number): Resposta<Oferta>
  vender(jogadorId: number): Resposta<Jogador[]>
  tatica(): Resposta<Tatica>
  definirTatica(definicao: DefinicaoTatica): Resposta<Tatica>
  escalacaoAutomatica(): Resposta<Tatica>
  avancarRodada(): Resposta<Rodada>
  ultimaRodada(): Resposta<Rodada>
  classificacao(divisaoId: string): Resposta<Classificacao>
  encerrarTemporada(): Resposta<FimDeTemporada>
  copa(): Resposta<Copa>
  iniciarAoVivo(): Resposta<AoVivo>
  aoVivo(): Resposta<AoVivo>
  avancarAoVivo(minutos: number): Resposta<AoVivo>
  decidirAoVivo(decisao: Decisao): Resposta<AoVivo>
  concluirAoVivo(): Resposta<Rodada>
  financas(): Resposta<Financas>
  /** Preço do ingresso; undefined volta ao preço sugerido. */
  definirPrecoIngresso(preco: number | undefined): Resposta<Financas>
  exportarSave(): Resposta<SaveExportado>
  carregarSave(conteudo: string): Resposta<Estado>
}

// Formato bruto exportado por src/Jogo.Wasm/NucleoJs.cs ([JSExport]): só strings.
interface ApiBruta {
  Versao(): string
  CarregarDefinicoes(arquivosJson: string): string
  NovaCarreira(clubeId: string, seed: string): string
  Estado(): string
  Elenco(): string
  Tatica(): string
  DefinirTatica(definicaoJson: string): string
  EscalacaoAutomatica(): string
  AvancarRodada(): string
  UltimaRodada(): string
  Classificacao(divisaoId: string): string
  EncerrarTemporada(): string
  Copa(): string
  IniciarAoVivo(): string
  AoVivo(): string
  AvancarAoVivo(minutos: number): string
  DecidirAoVivo(decisaoJson: string): string
  ConcluirAoVivo(): string
  Financas(): string
  RenovarContrato(jogadorId: number, anos: number): string
  Dispensar(jogadorId: number): string
  Mercado(buscaJson: string): string
  Comprar(jogadorId: number): string
  OfertaPor(jogadorId: number): string
  Vender(jogadorId: number): string
  DefinirPrecoIngresso(preco: string): string
  ExportarSave(): string
  CarregarSave(conteudo: string): string
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
    return embrulhar(api)
  })()

  // Se falhar, permite tentar de novo depois.
  carregamento.catch(() => {
    carregamento = undefined
  })

  return carregamento
}

function ler<T>(json: string): Resposta<T> {
  return JSON.parse(json) as Resposta<T>
}

function embrulhar(api: ApiBruta): Nucleo {
  return {
    versao: () => api.Versao(),
    carregarDefinicoes: (arquivos) => ler(api.CarregarDefinicoes(JSON.stringify(arquivos))),
    novaCarreira: (clubeId, seed) => ler(api.NovaCarreira(clubeId, seed)),
    estado: () => ler(api.Estado()),
    elenco: () => ler(api.Elenco()),
    renovarContrato: (jogadorId, anos) => ler(api.RenovarContrato(jogadorId, anos)),
    dispensar: (jogadorId) => ler(api.Dispensar(jogadorId)),
    mercado: (busca) => ler(api.Mercado(JSON.stringify(busca))),
    comprar: (jogadorId) => ler(api.Comprar(jogadorId)),
    ofertaPor: (jogadorId) => ler(api.OfertaPor(jogadorId)),
    vender: (jogadorId) => ler(api.Vender(jogadorId)),
    tatica: () => ler(api.Tatica()),
    definirTatica: (definicao) => ler(api.DefinirTatica(JSON.stringify(definicao))),
    escalacaoAutomatica: () => ler(api.EscalacaoAutomatica()),
    avancarRodada: () => ler(api.AvancarRodada()),
    ultimaRodada: () => ler(api.UltimaRodada()),
    classificacao: (divisaoId) => ler(api.Classificacao(divisaoId)),
    encerrarTemporada: () => ler(api.EncerrarTemporada()),
    copa: () => ler(api.Copa()),
    iniciarAoVivo: () => ler(api.IniciarAoVivo()),
    aoVivo: () => ler(api.AoVivo()),
    avancarAoVivo: (minutos) => ler(api.AvancarAoVivo(minutos)),
    decidirAoVivo: (decisao) => ler(api.DecidirAoVivo(JSON.stringify(decisao))),
    concluirAoVivo: () => ler(api.ConcluirAoVivo()),
    financas: () => ler(api.Financas()),
    definirPrecoIngresso: (preco) => ler(api.DefinirPrecoIngresso(preco === undefined ? '' : String(preco))),
    exportarSave: () => ler(api.ExportarSave()),
    carregarSave: (conteudo) => ler(api.CarregarSave(conteudo)),
  }
}
