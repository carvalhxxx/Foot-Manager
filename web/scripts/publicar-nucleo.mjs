// Publica o núcleo C# (src/Jogo.Wasm) em WebAssembly e copia o resultado para web/public/nucleo,
// de onde o site carrega o runtime do .NET. Rode de novo sempre que mudar código C#.
//
//   npm run nucleo

import { execFileSync } from 'node:child_process'
import { cpSync, rmSync } from 'node:fs'
import path from 'node:path'
import { fileURLToPath } from 'node:url'

const web = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..')
const raiz = path.resolve(web, '..')
const saida = path.join(raiz, 'artifacts', 'nucleo')
const destino = path.join(web, 'public', 'nucleo')

rmSync(saida, { recursive: true, force: true })
execFileSync('dotnet', ['publish', path.join(raiz, 'src', 'Jogo.Wasm'), '-c', 'Release', '-o', saida], {
  stdio: 'inherit',
})

rmSync(destino, { recursive: true, force: true })
cpSync(path.join(saida, 'wwwroot', '_framework'), path.join(destino, '_framework'), { recursive: true })
console.log(`Núcleo publicado em ${path.relative(raiz, destino)}`)
