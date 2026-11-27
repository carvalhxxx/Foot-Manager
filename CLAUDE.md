# Foot Manager BR (nome provisório)

Manager de futebol brasileiro **no navegador**, inspirado na simplicidade e no loop de "só mais uma rodada" de Brasfoot/Elifoot, com algumas mecânicas do Football Manager **muito simplificadas**.

**Regra de ouro do design:** cada mecânica precisa caber em uma tela e ser entendida em 10 segundos. Se exigir tutorial, entra simplificada ou não entra.

Prioridade: lançar rápido na web (desktop e celular), sem servidor: tudo roda no navegador do jogador.

## Aviso legal (obrigatório)
- Não usar os nomes "Brasfoot" ou "Elifoot", nem escudos, sons, dados ou código desses jogos.
- **Não copiar código** de projetos GPL (OpenFoot Manager, Bygfoot). Ler para entender ideias é ok.
- **Clubes e jogadores reais** (nomes, cidades, estádios, cores), por decisão do dono do projeto em 2026-11 (D-033), ciente do risco de marca e de direito de imagem. **Escudos, logotipos e fotos continuam proibidos**: o jogo só usa as cores dos clubes.
- **Atributos são nossos**: estimados a partir de dados públicos (idade, posição, valor de mercado), nunca copiados de outro jogo (Brasfoot, Elifoot, FIFA/FC, Football Manager) ou de patches de comunidade.

## Stack
- **Núcleo do jogo:** C# / .NET 10 (LTS), SDK fixado em `global.json`. Domínio, gerador, motor e simulação.
- **Aplicação:** `src/Jogo.Aplicacao` tem os casos de uso da interface (carreira, tática, rodada, tabelas) e o contrato JSON. É C# comum, testado com xUnit.
- **Ponte:** `src/Jogo.Wasm` compila para WebAssembly e só repassa `Jogo.Aplicacao.ApiJson` ao JavaScript com `[JSExport]`.
- **Interface:** React 19 + TypeScript + Vite em `web/`. Lint com oxlint, testes com Vitest + Testing Library.
- Testes C#: xUnit v3 no Microsoft.Testing.Platform; testes de arquitetura com Mono.Cecil.
- Saves: carreira serializada pelo C# (`Jogo.Aplicacao/Saves`) e guardada no IndexedDB do navegador, com exportar/importar arquivo (D-026). PWA com service worker em `web/public/sw.js`.
- CI/CD: GitHub Actions (`.github/workflows/ci.yml`). Núcleo testado em Linux e Windows; site publicado no GitHub Pages a cada push na `main`.

## Comandos
```bash
# Núcleo C#
dotnet build FootManagerBR.slnx                 # build (warnings viram erro)
dotnet test --solution FootManagerBR.slnx        # testes do núcleo
dotnet run --project tools/Jogo.Calibracao       # calibração do motor, da temporada e de carreiras de 5 anos com mercado (sai com erro fora das metas)
dotnet run --project tools/Jogo.Simulador -- --temporadas 3   # simula temporadas e mostra tabelas/artilharia
dotnet run --project tools/Jogo.GeradorDados     # regera dados/definicoes a partir dos dados reais (seed 2027, temporada 2026)
dotnet run --project tools/Jogo.GeradorDados -- --api   # SÓ LOCAL: elencos da API-Football (chave em API_FOOTBALL_KEY) em dados/local/definicoes

# Site (rodar dentro de web/)
npm install                                      # primeira vez
npm run nucleo                                   # publica o núcleo C# em web/public/nucleo (rodar após mudar C#)
npm run dev                                      # servidor de desenvolvimento
npm run dev:local                                # idem, com os dados locais da API (dados/local/definicoes)
npm run lint && npm test && npm run build        # o que o CI roda
```

## Estrutura
```
src/Jogo.Dominio     Entidades e regras puras. Sem dependências.
src/Jogo.Motor       Motor de partida. Puro, determinístico com seed. → Dominio
src/Jogo.Simulacao   Rodadas, calendário, IA, mercado, fim de temporada, geração do mundo. → Dominio, Motor
src/Jogo.Dados       Leitura/gravação das definições JSON (de pasta ou da memória). → Dominio
src/Jogo.Aplicacao   Casos de uso + DTOs + ApiJson (texto JSON entra e sai). → Dominio, Motor, Simulacao, Dados
src/Jogo.Wasm        Casca WebAssembly: [JSExport] que repassa para ApiJson. → Aplicacao
web/                 Interface React + TypeScript (Vite).
  src/nucleo/        Carregamento do runtime .NET e contrato com o núcleo (contrato.ts espelha Jogo.Aplicacao/Contrato.cs).
  src/telas/         Uma tela por arquivo; recebem dados prontos por props.
  src/JogoEmAndamento.tsx  Casca do jogo: menu no topo, coluna da próxima partida (telas/PainelPartida.tsx) com "Jogar rodada" (atalho Espaço) e "Simular rápido", e troca de telas (a inicial é o Elenco).
  src/telas/AoVivo.tsx     Jogo ao vivo: relógio, velocidades, pausas, decisões e placar da rodada.
  src/textos.ts      Todos os textos da interface (inclusive a descrição dos eventos da partida).
  src/narracao.ts    Narração ao vivo: vários textos por tipo de lance (texto de interface, não regra de jogo).
  src/saves/         Armazenamento dos saves (IndexedDB; versão em memória para testes), salvar e baixar arquivo.
tools/Jogo.Calibracao    CLI que simula milhares de partidas e temporadas e confere as metas.
tools/Jogo.Simulador     CLI que simula temporadas completas (Séries A e B) sem interface.
tools/Jogo.GeradorDados  Gera o mundo inicial (clubes e elencos reais de tools/Jogo.GeradorDados/reais/) em dados/definicoes.
tests/               Um projeto de testes por camada + Jogo.Arquitetura.Testes.
dados/definicoes/    Ligas, clubes e jogadores em JSON (formato aberto). Entram no site em tempo de build.
docs/                ROADMAP.md (kanban), DECISOES.md, FORMATO_DADOS.md.
```

## Regras de arquitetura (verificadas por `tests/Jogo.Arquitetura.Testes`)
- Dominio, Motor e Simulacao são **puros**: recebem estado e devolvem estado + eventos. Proibido: I/O (`System.IO`, `System.Net`, `System.Data`), `Console`, `Environment`, `DateTime.Now/UtcNow/Today`, `Guid.NewGuid`, `System.Random`, `Stopwatch`, `Parallel`.
- **Todo sorteio** usa `IGeradorAleatorio` (em `Jogo.Dominio.Aleatoriedade`), injetado com seed. Mesma seed + mesmo estado = mesmo resultado.
- Datas do jogo vêm do estado do jogo (calendário), nunca do relógio do computador.
- Cada camada só referencia as camadas abaixo dela (ver tabela acima).
- **Regras do jogo moram no C#.** A interface só exibe e envia comandos; nada de cálculo de jogo em TypeScript.
- Fronteira C# ↔ JS: só JSON; nenhuma exceção atravessa (erros viram `{ ok: false, erros }`). Mudou `Jogo.Aplicacao/Contrato.cs`? Atualize `web/src/nucleo/contrato.ts`.
- O estado do jogo mora no C# (`ServicoDeJogo`). A interface não guarda cópia: depois de cada ação, relê o que precisa.
- Mudou algo que afeta o estado da carreira (nova propriedade em `Jogador`, `Clube`, `Temporada`...)? Inclua no save (`Jogo.Aplicacao/Saves`) e mantenha o teste "salvar e carregar = jogar direto" passando.
- Sem over-engineering: nada de microserviços, CQRS, event sourcing ou contêiner de DI no MVP.

## Convenções de código
- Código, comentários, testes e textos do jogo em **português**, em C# e em TypeScript. Termos técnicos consagrados ficam em inglês.
- C#: namespaces com escopo de arquivo; `sealed` por padrão. Pacotes: versões só em `Directory.Packages.props`.
- TypeScript: componentes de função, sem `any`. Textos só em `web/src/textos.ts`: nada de texto fixo nos componentes.
- Estilo: CSS próprio com variáveis de tema em `web/src/index.css` (claro/escuro via `data-tema`). Sem biblioteca de componentes por enquanto.
- Testes C#: `Metodo_Cenario_ResultadoEsperado`, padrão Arrange/Act/Assert. Sorteios em testes sempre com seed fixa.
- Testes web: o núcleo é injetado (ex.: `App iniciar={...}`), então os testes de UI não precisam do WebAssembly.
- Ordem determinística: não depender da ordem de iteração de `Dictionary`/`HashSet` dentro da simulação; ordene explicitamente.

## Forma de trabalho
- **Uma entrega por vez**, com testes passando, commit claro e `docs/ROADMAP.md` atualizado.
- Decisões relevantes vão para `docs/DECISOES.md` (curto: contexto, decisão, consequência).
- Se algo estiver ambíguo ou conflitante, **perguntar antes de implementar**.
- `dados/definicoes` é **gerado**: não edite à mão. Mudou o gerador? Rode `tools/Jogo.GeradorDados` e commite os arquivos (há um teste que confere).
- Projetos de teste ainda vazios usam `--ignore-exit-code 8` no `.csproj` (marcado como TEMPORÁRIO). Remova a linha ao escrever o primeiro teste do projeto.
