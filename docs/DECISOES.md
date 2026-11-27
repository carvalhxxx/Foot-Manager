# Registro de decisões

Formato curto: contexto → decisão → consequência. Quando houver decisões **propostas**, elas ficam numa seção própria até serem confirmadas.

## Aceitas

### D-001 · .NET 10 LTS (2026-11-13)
- **Contexto:** o projeto pede a LTS mais recente. Em novembro de 2026, a LTS é o .NET 10; o .NET 11 é STS.
- **Decisão:** `net10.0`, SDK fixado em `global.json` (`10.0.100`, `rollForward: latestFeature`).
- **Consequência:** suporte até novembro de 2028. Migrar para o .NET 12 (próxima LTS) quando ele sair.

### D-002 · Estrutura de build centralizada (2026-11-13)
- **Decisão:** solution no formato `.slnx`; `Directory.Build.props` com nullable, `TreatWarningsAsErrors` e analisadores; versões de pacotes centralizadas em `Directory.Packages.props`.
- **Consequência:** nenhum warning entra no código. Atualizar um pacote é mudar uma linha.

### D-003 · Camadas puras (2026-11-13)
- **Contexto:** o motor e a simulação precisam ser reproduzíveis (replay, testes, multiplayer futuro).
- **Decisão:** Dominio, Motor e Simulacao não fazem I/O, não leem o relógio e não têm aleatoriedade própria. Um teste (`Jogo.Arquitetura.Testes`) lê o IL compilado com Mono.Cecil e falha se encontrar `System.IO`, `System.Net`, `DateTime.Now`, `Guid.NewGuid`, `System.Random` etc.
- **Consequência:** a regra não depende de revisão manual. Para adicionar exceções, é preciso mudar o teste explicitamente.

### D-004 · Dependências entre camadas (2026-11-13)
- **Decisão:** Motor → Dominio; Simulacao → Dominio, Motor; Dados → Dominio; App → todos. Os projetos puros só podem depender da biblioteca padrão.
- **Consequência:** verificado pelo mesmo projeto de testes de arquitetura.

### D-005 · Gerador aleatório próprio (2026-11-13)
- **Contexto:** o documento pede um `IRandom` com seed. O algoritmo de `System.Random` não tem garantia de estabilidade entre versões do .NET, o que quebraria saves e replays antigos.
- **Decisão:** `IGeradorAleatorio` (o "IRandom" do documento), implementado com xoshiro256** semeado por SplitMix64, sorteio inteiro sem viés (método de Lemire) e estado capturável (`EstadoGerador`).
- **Consequência:** um teste "golden" trava a sequência de uma seed fixa. Mudar o algoritmo exige nova decisão aqui.

### D-006 · Testes com xUnit v3 + Microsoft.Testing.Platform (2026-11-13)
- **Decisão:** um projeto de testes por camada, mais `Jogo.Arquitetura.Testes`. Runner MTP configurado em `global.json`.
- **Consequência:** projetos de teste ainda vazios usam `--ignore-exit-code 8` temporariamente, para não reprovar por "zero testes".

### D-007 · UI: Avalonia 12, MVVM simples (2026-11-13) · **substituída pela D-019**
- **Decisão:** Avalonia 12 + CommunityToolkit.Mvvm com compiled bindings. Navegação por `PaginaAtual` na janela principal; `ViewLocator` por convenção de nomes. Dependências montadas à mão em `App.axaml.cs`, sem contêiner de DI por enquanto. Textos em `Recursos/Textos.resx` com classe fortemente tipada gerada no build.
- **Consequência:** pouca cerimônia. Um contêiner de DI só entra quando o número de serviços justificar.

### D-008 · Idioma do código (2026-11-13)
- **Decisão:** código, comentários e testes em português. Termos técnicos consagrados ficam em inglês (ViewModel, View, Command).

### D-009 · Licença MIT (2026-11-13)
- **Contexto:** o repositório no GitHub foi criado com a licença MIT.
- **Decisão:** o código é distribuído sob MIT.
- **Consequência:** qualquer pessoa pode reutilizar o código, inclusive comercialmente, desde que mantenha o aviso de copyright. Isso continua impedindo copiar código GPL (OpenFoot Manager, Bygfoot) para cá, porque a GPL exigiria trocar a licença do projeto inteiro.

### D-010 · Adiar o SQLite para depois do motor e da temporada (2026-11-13, era P-001) · **em revisão (P-006)**
- **Motivo:** o domínio vai mudar muito durante a calibração. Até lá, o mundo roda em memória, carregado do JSON, e o schema nasce já estável.
- **Impacto:** a entrega 3 passa a ser "Domínio + JSON + gerador de seed"; o SQLite vai para a entrega de save/load.

### D-011 · Dapper em vez de EF Core (2026-11-13, era P-002) · **em revisão (P-006)**
- **Motivo:** o jogo carrega o mundo inteiro em memória, simula e grava. Não há consultas complexas nem uso para change tracking. Dapper + scripts SQL numerados (`PRAGMA user_version`) é mais leve e mais amigável ao trimming/AOT para o mobile.

### D-012 · Moral por jogador (2026-11-13, era P-003)
- **Motivo:** o documento cita a moral do time e a moral individual. Decisão: a moral é por jogador, e a moral do time é a média dos titulares.

### D-013 · Regras da liga no MVP (2026-11-13, era P-004)
- **Decisão:** 4 sobem e 4 descem entre as Séries A e B; duas janelas de transferência (pré-temporada e meio do campeonato).

### D-014 · Refinos do motor ficam para a Fase 2 (2026-11-13, era P-005)
- **Contexto:** discutido na fase de planejamento.
- **Decisão:** o motor do MVP segue o pipeline do documento (4 setores, mando de campo configurável). Ficam para a Fase 2: modelo de zonas (corredores central/lados), estilos de jogo, mando decomposto (torcida, gramado), clima e adaptação. O motor já nasce com um objeto `CondicoesPartida` para receber esses fatores sem mudar a API.

### D-015 · Posições e força geral (2026-11-13)
- **Decisão:** 15 posições (GOL, ZAG, LD, LE, ALD, ALE, VOL, MC, MD, ME, MO, PD, PE, SA, ATA). Os dois lados de uma função usam os mesmos pesos. Força geral = média ponderada dos atributos pela posição (pesos inteiros somando 100), convertida de 1-20 para 1-99 por `1 + (m − 1) × 98/19`, em aritmética inteira. Tabela de pesos em `src/Jogo.Dominio/Jogadores/CalculadoraForca.cs`.
- **Consequência:** a força exibida é a da posição principal. Cada jogador tem até 2 posições secundárias, com rendimento de 100%. A penalidade de improviso fica com o motor (E4). Mudar os pesos quebra testes de regressão de propósito.

### D-016 · Modelo das entidades (2026-11-13)
- **Decisão:** entidades são classes com invariantes validadas no construtor e alteradas só por métodos (`AjustarMoral`, `AssinarContrato`...). Valores (`Atributos`, `Contrato`, `Estadio`) são records imutáveis. Todo jogador guarda os 10 atributos (os 8 de linha + Reflexos e Posicionamento), e a tela mostra só os 8 relevantes para a posição. Clubes e divisões usam identificadores de texto (`porto-alegre-fc`) e jogadores usam id numérico. Dinheiro é `decimal` em reais.
- **Consequência:** improvisar um jogador fora da posição já cai naturalmente pelos pesos, sem regra especial. Estado mutável é aceitável porque o determinismo vem do gerador com seed e da ordem fixa de processamento.

### D-017 · Formato das definições em JSON (2026-11-13)
- **Decisão:** cada pasta de definições tem `manifesto.json`, `divisoes.json` e `clubes/<id>.json` (um arquivo por clube, com o elenco dentro). Leitura com System.Text.Json gerado em compilação (sem reflexão). Todos os campos dos modelos JSON são anuláveis, e a conversão gera mensagens em português com arquivo + local + motivo, juntando **todos** os erros antes de falhar. A consistência entre arquivos fica no domínio (`Mundo.Inconsistencias`). A gravação é estável: UTF-8 sem BOM, quebra de linha `\n`, acentos legíveis.
- **Consequência:** patches da comunidade são fáceis de editar e revisar no Git. Força geral, moral e condição não são gravadas nas definições: força é calculada, e moral/condição são estado de carreira (vão para o save).

### D-018 · Gerador do mundo inicial (2026-11-13)
- **Decisão:** o gerador de jogadores e do mundo fica em `Jogo.Simulacao/Geracao` (puro, com seed), porque também vai gerar as revelações da base no fim de temporada. O catálogo dos 40 clubes fictícios fica em `CatalogoBase`, com nomes inventados a partir de apelidos de cidades, rios e regiões, para não coincidir com clubes reais. Cada clube usa um gerador aleatório próprio (seed XOR hash FNV-1a do id), então mudar um clube não altera os outros. Porte (grande/médio/pequeno) define força do elenco, torcida, reputação, caixa e estádio. Grandes têm 2 craques (+15 de força) e médios da Série A, 0 ou 1.
- **Consequência:** `dados/definicoes` é gerado por `tools/Jogo.GeradorDados` com a seed 2027, e um teste falha se os arquivos commitados não baterem com o gerador. Ao mudar o gerador, é preciso rodar a ferramenta e commitar os dados. Quando a base passar a ser ajustada à mão, esse teste deve ser removido.

### D-019 · Jogo no navegador: React + TypeScript com núcleo C# em WebAssembly (2026-11-14)
- **Contexto:** um jogo desktop seria pouco chamativo; no navegador o jogo fica a um link de distância, roda no celular e não exige instalação.
- **Decisão:** a interface passa a ser React + TypeScript (Vite) em `web/`. O núcleo C# (domínio, gerador, motor, simulação) continua igual e roda **dentro do navegador** via WebAssembly (`src/Jogo.Wasm`, `Microsoft.NET.Sdk.WebAssembly`, `[JSExport]`), trocando JSON com a interface. Sem servidor: hospedagem estática no GitHub Pages, publicada pelo CI. O projeto Avalonia (`Jogo.App`) foi removido.
- **Consequência:** regras do jogo ficam só no C# (testadas com xUnit); o TypeScript só exibe e envia comandos. O primeiro acesso baixa o runtime do .NET (alguns MB, depois em cache). As definições do jogo base entram no site em tempo de build. O leitor de definições passou a aceitar qualquer fonte (`IFonteDefinicoes`: pasta ou memória), o que também prepara patches enviados pelo jogador.

### D-020 · Motor de partida estatístico em 4 setores (2026-11-26)
- **Decisão:** cada jogador em campo vale `força na posição × improviso × condição × moral (× mando)`. Cada posição contribui para Defesa, Meio e Ataque com pesos fixos (ex.: lateral 70% defesa e 30% meio). A força do setor é a qualidade média × (ocupação / ocupação do 4-4-2)^0,5. A cada minuto: posse sorteada por `meio^1,5`; chance de lance de perigo; duelo `ataque^2 × defesa^2`; finalização pela qualidade do atacante contra o goleiro; quando o ataque perde o duelo, saem escanteios e faltas. Há também faltas "de meio de campo", cartões (o segundo amarelo é mais raro), lesões ligadas à propensão e ao cansaço, e uma trava anti-goleada a partir de 3 gols de diferença. Improviso fora da posição: 100% na principal e nas secundárias, 93% no lado oposto, 85% no mesmo setor, 70% fora dele, 50% no gol. Goleiro expulso ou lesionado sem goleiro no banco: o jogador de linha com mais jeito vai para o gol.
- **Consequência:** o meio-campo decide a posse, então tirar os meias (5-0-5) não compensa: a correção do bug do Elifoot está garantida por teste. Todos os números ficam em `ConfiguracaoMotor`, e mudar qualquer um exige recalibrar.

### D-021 · Metas de calibração (2026-11-26)
- **Decisão:** a calibração (`Calibrador` em `Jogo.Simulacao`, CLI `tools/Jogo.Calibracao`) exige: gols por jogo entre 2,3 e 2,7; vitórias do mandante entre 42% e 48%; empates entre 24% e 29%; favorito (+6 de força no top 11) vencendo entre 55% e 80%; cada formação padrão com 40% a 60% dos pontos contra o 4-4-2 (50% = equilíbrio, medido como fatia dos pontos do confronto para não depender da taxa de empates); 5-0-5 e 3-0-7 com no máximo 40%.
- **Consequência:** a CLI sai com erro se uma meta falhar (o CI barra), e uma versão menor roda nos testes. Valores atuais, iguais em 3 seeds: 2,44 gols, 45% mandante, 26% empates, 58% favorito, 23 finalizações e 3,5 amarelos por jogo.
- **Ajuste na E5:** quem finaliza passou a ser sorteado com peso linear na Finalização (antes era ao quadrado), com mais participação dos meias e dos zagueiros nos escanteios. Com o peso ao quadrado, o artilheiro fazia ~37 gols em 38 jogos; agora faz ~25. A chance de gol no alvo subiu de 0,285 para 0,295 para manter a média de gols.

### D-022 · Temporada e carreira (2026-11-26)
- **Calendário:** fatoração canônica do método do círculo, com a ordem dos clubes sorteada pela seed; returno igual ao turno com mandos invertidos.
- **Desempate:** pontos, vitórias, saldo, gols pró, confronto direto (só entre 2 clubes), menos vermelhos, menos amarelos e sorteio determinístico.
- **Pós-jogo:** a condição física fica com o valor do fim do jogo e recupera `15 + Resistência/2` por rodada. Vermelho = 1 jogo de suspensão; 3 amarelos = 1 jogo. A lesão tem a duração sorteada pelo motor. Moral: vitória +3, derrota −3, quem jogou +1, quem não jogou −1, e 10% de volta ao neutro a cada rodada. Entrosamento: até +3% de força quando os titulares vêm jogando juntos há 5 rodadas.
- **Fim de temporada:** a evolução depende da idade e o profissionalismo acelera o crescimento e freia o declínio; o crescimento nunca passa do potencial. Aposentadoria a partir dos 34 (chance crescente; obrigatória aos 39). Revelações da base: 1 ou 2 por ano, mais o que faltar para o elenco ter 25. Contratos vencidos são renovados automaticamente por 2 anos até a E8.
- **Consequência:** cada jogo tem semente própria (ano/divisão/rodada/jogo), então a temporada é reproduzível e mudar um jogo não afeta os outros. Lesões, suspensões e sequências ficam no `Jogador` como estado de carreira (fora das definições JSON; vão para o save na E9).

### D-023 · Metas de temporada (2026-11-26)
- **Decisão:** em simulações independentes da Série A, partindo do mundo base: o clube mais forte é campeão entre 25% e 75% das vezes; o campeão está entre os 4 mais fortes em pelo menos 70% das vezes; o campeão faz entre 68 e 88 pontos; o lanterna, entre 18 e 40.
- **Consequência:** com 30 temporadas, os valores atuais são 67%, 93%, 84 pontos e 27 pontos. Amostras pequenas (10 temporadas) oscilam bastante, por isso a CLI usa 30.

### D-024 · Camada de aplicação e interface do jogo (2026-11-26)
- **Decisão:** os casos de uso da interface ficam em `Jogo.Aplicacao` (`ServicoDeJogo` com DTOs, `ApiJson` com texto JSON entrando e saindo), e o `Jogo.Wasm` só repassa as chamadas. A `Carreira` (em `Jogo.Simulacao`) guarda o clube do jogador e a tática; antes de cada rodada, troca titulares lesionados ou suspensos pelo melhor disponível e avisa o que mudou. O estado do jogo mora no C#; a interface relê os dados depois de cada ação, sem cópia própria. A força mostrada na Tática é a efetiva (com a penalidade de improviso). O atalho para avançar é a barra de espaço.
- **Consequência:** a fronteira inteira (inclusive uma temporada completa e a virada de ano) é testada com xUnit, sem navegador. A interface é testada com um núcleo falso. Uma rodada leva cerca de 0,5 s no WebAssembly (sem AOT); se incomodar, o caminho é compilar com AOT (`wasm-tools`).

## Propostas (aguardando confirmação)

### P-006 · Saves no navegador em vez de SQLite
- **Contexto:** com o jogo no navegador, um arquivo SQLite por save (D-010/D-011) deixa de ser o caminho natural.
- **Proposta:** o save é o estado do jogo serializado em JSON (compactado), guardado no IndexedDB do navegador, com opção de **exportar/importar arquivo** para backup ou para jogar em outro aparelho. Sem SQLite e sem Dapper.
- **Impacto:** a E9 vira "save/load no navegador + exportação"; sincronização na nuvem continua na Fase 3.
