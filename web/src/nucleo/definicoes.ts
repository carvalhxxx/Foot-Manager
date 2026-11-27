// As definições do jogo entram no pacote do site em tempo de build.
// O núcleo C# recebe um mapa "caminho relativo" → conteúdo, igual a uma pasta no disco.
//
// Normalmente: dados/definicoes (os dados pesquisados, publicados no site).
// No modo "dados locais" (npm run dev:local), dados/local/definicoes, gerada com a API-Football só
// para uso na própria máquina (D-038). Se essa pasta não existir, cai para a base.

const PREFIXO_BASE = '../../../dados/definicoes/'
const PREFIXO_LOCAL = '../../../dados/local/definicoes/'

const base = import.meta.glob('../../../dados/definicoes/**/*.json', {
  query: '?raw',
  import: 'default',
  eager: true,
}) as Record<string, string>

// Só entra no pacote quando o modo local está ligado (o glob vazio some no build normal).
const locais = (
  import.meta.env.VITE_DADOS_LOCAIS === '1'
    ? import.meta.glob('../../../dados/local/definicoes/**/*.json', { query: '?raw', import: 'default', eager: true })
    : {}
) as Record<string, string>

function semPrefixo(arquivos: Record<string, string>, prefixo: string): Record<string, string> {
  return Object.fromEntries(Object.entries(arquivos).map(([caminho, conteudo]) => [caminho.slice(prefixo.length), conteudo]))
}

/** Usa os dados locais quando o modo está ligado e a pasta existe; senão, a base publicada. */
export function arquivosDaBase(): Record<string, string> {
  return Object.keys(locais).length > 0 ? semPrefixo(locais, PREFIXO_LOCAL) : semPrefixo(base, PREFIXO_BASE)
}

/** Verdadeiro quando o jogo está rodando com os dados locais da API. */
export const usandoDadosLocais = Object.keys(locais).length > 0
