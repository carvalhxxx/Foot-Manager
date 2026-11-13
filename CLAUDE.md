# Foot Manager BR (nome provisório)

Manager de futebol brasileiro para PC, inspirado na simplicidade e no loop de "só mais uma rodada" de Brasfoot/Elifoot, com algumas mecânicas do Football Manager **muito simplificadas**.

**Regra de ouro do design:** cada mecânica precisa caber em uma tela e ser entendida em 10 segundos. Se exigir tutorial, entra simplificada ou não entra.

Prioridade: lançar rápido no Windows, com arquitetura pronta para mobile depois.

## Aviso legal (obrigatório)
- Não usar os nomes "Brasfoot" ou "Elifoot", nem escudos, sons, dados ou código desses jogos.
- **Não copiar código** de projetos GPL (OpenFoot Manager, Bygfoot). Ler para entender ideias é ok.
- Clubes e jogadores **fictícios** por padrão. Cidades brasileiras reais podem ser usadas; nomes de clubes reais, não.

## Stack
- .NET 10 (LTS), C# `latest`. SDK fixado em `global.json`.
- UI: Avalonia 12 + CommunityToolkit.Mvvm.
- Testes: xUnit v3 rodando no Microsoft.Testing.Platform. Testes de arquitetura com Mono.Cecil.
- Banco: SQLite, um arquivo `.db` por save (ainda não implementado; ver `docs/DECISOES.md`).
- CI: GitHub Actions (`.github/workflows/ci.yml`), Windows + Linux.

## Comandos
```bash
dotnet build FootManagerBR.slnx                 # build (warnings viram erro)
dotnet test --solution FootManagerBR.slnx        # todos os testes
dotnet run --project src/Jogo.App                # abre o jogo
dotnet run --project tools/Jogo.Calibracao       # calibração do motor
```

## Estrutura
```
src/Jogo.Dominio     Entidades e regras puras. Sem dependências.
src/Jogo.Motor       Motor de partida. Puro, determinístico com seed. → Dominio
src/Jogo.Simulacao   Rodadas, calendário, IA, mercado, fim de temporada. → Dominio, Motor
src/Jogo.Dados       SQLite e importação das definições JSON. → Dominio
src/Jogo.App         Avalonia (Views + ViewModels). → todos
tools/Jogo.Calibracao  CLI que simula milhares de partidas e confere as metas.
tests/               Um projeto de testes por camada + Jogo.Arquitetura.Testes.
dados/definicoes/    Ligas, clubes e jogadores em JSON (formato aberto).
docs/                ROADMAP.md (kanban), DECISOES.md, FORMATO_DADOS.md.
```

## Regras de arquitetura (verificadas por `tests/Jogo.Arquitetura.Testes`)
- Dominio, Motor e Simulacao são **puros**: recebem estado e devolvem estado + eventos. Proibido: I/O (`System.IO`, `System.Net`, `System.Data`), `Console`, `Environment`, `DateTime.Now/UtcNow/Today`, `Guid.NewGuid`, `System.Random`, `Stopwatch`, `Parallel`.
- **Todo sorteio** usa `IGeradorAleatorio` (em `Jogo.Dominio.Aleatoriedade`), injetado com seed. Mesma seed + mesmo estado = mesmo resultado.
- Datas do jogo vêm do estado do jogo (calendário), nunca do relógio do computador.
- Cada camada só referencia as camadas abaixo dela (ver tabela acima).
- Sem over-engineering: nada de microserviços, CQRS, event sourcing ou contêiner de DI no MVP.

## Convenções de código
- Código, comentários, testes e textos do jogo em **português**. Termos técnicos consagrados ficam em inglês (`ViewModel`, `View`, `Command`).
- Namespaces com escopo de arquivo; `sealed` por padrão em classes que não foram feitas para herança.
- Pacotes: versões só em `Directory.Packages.props`, nunca no `.csproj`.
- Textos de UI só em `src/Jogo.App/Recursos/Textos.resx` (classe gerada `Jogo.App.Recursos.Textos`). Nada de texto fixo em XAML ou C#.
- ViewModels em `ViewModels/XxxViewModel.cs`, Views em `Views/XxxView.axaml`: o `ViewLocator` liga os dois pelo nome.
- Testes: `Metodo_Cenario_ResultadoEsperado`, padrão Arrange/Act/Assert. Sorteios em testes sempre com seed fixa.
- Ordem determinística: não depender da ordem de iteração de `Dictionary`/`HashSet` dentro da simulação; ordene explicitamente.

## Forma de trabalho
- **Uma entrega por vez**, com testes passando, commit claro e `docs/ROADMAP.md` atualizado.
- Decisões relevantes vão para `docs/DECISOES.md` (curto: contexto, decisão, consequência).
- Se algo estiver ambíguo ou conflitante, **perguntar antes de implementar**.
- Projetos de teste ainda vazios usam `--ignore-exit-code 8` no `.csproj` (marcado como TEMPORÁRIO). Remova a linha ao escrever o primeiro teste do projeto.
