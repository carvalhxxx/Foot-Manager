// As definições do jogo base (dados/definicoes) entram no pacote do site em tempo de build.
// O núcleo C# recebe um mapa "caminho relativo" → conteúdo, igual a uma pasta no disco.

const PREFIXO = '../../../dados/definicoes/'

const brutos = import.meta.glob('../../../dados/definicoes/**/*.json', {
  query: '?raw',
  import: 'default',
  eager: true,
}) as Record<string, string>

export function arquivosDaBase(): Record<string, string> {
  return Object.fromEntries(Object.entries(brutos).map(([caminho, conteudo]) => [caminho.slice(PREFIXO.length), conteudo]))
}
