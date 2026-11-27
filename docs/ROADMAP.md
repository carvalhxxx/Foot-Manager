# Roadmap (kanban)

Estimativas: **P** (até 1 dia) · **M** (2 a 4 dias) · **G** (1 semana ou mais).
Cada entrega termina com testes passando, commit claro e este arquivo atualizado.

## Em andamento

### E8 · Mercado, contratos e finanças · G
- **Objetivo:** comprar, vender e dispensar (com a IA também negociando), janelas de transferência, contratos e renovação, bilheteria com preço do ingresso, patrocínio, TV, premiação e salários.
- **Observação:** hoje os elencos só mudam por evolução, aposentadoria e base, e os contratos são renovados automaticamente. Por isso o clube mais forte domina várias temporadas seguidas. O mercado é o que vai trazer rotatividade entre os grandes.
- **Aceite:** uma temporada simulada fecha o caixa de forma coerente para todos os clubes.
- **Etapas:**
  1. ~~8a · Finanças~~ ✔ bilheteria com preço do ingresso, TV, patrocínio, salários por rodada, premiação, tela Finanças, calibração (D-027);
  2. ~~8b · Contratos~~ ✔ valor de mercado, renovação no Elenco (1 a 4 anos), dispensa com rescisão, agentes livres, IA renovando e liberando (D-028);
  3. 8c · Mercado: janelas, comprar e vender, IA negociando.

## Refinando

_(nada)_

## Backlog (MVP · Fase 1)

## Fase 2 (depois do lançamento do MVP)
- **2.0 Calendário real:** datas, meses, jogos no meio de semana (pré-requisito do resto).
- **2A Carreira:** diretoria (metas, confiança, demissão, propostas), notícias/jornal, prêmios da temporada, categorias de base.
- **2B Editor** de ligas, clubes e jogadores + importação/exportação de patches.
- **2C Competições:** Copa do Brasil (prorrogação e pênaltis), Séries C e D, estaduais.
- **2D Estádio e economia:** ampliação, melhorias, estado do gramado, empréstimo bancário, treino simples.
- **2E Refinos do motor:** modelo de zonas (corredores central/lados), estilos de jogo, mando decomposto (torcida, tipo de gramado, adaptação), clima (chuva/calor por região e mês), cansaço de viagem, torneio de calibração de táticas.
- **2F Continental:** Libertadores, Sul-Americana, altitude.

## Fase 3
Mobile, saves na nuvem, ligas online entre amigos, modelo freemium/assinatura.

## Feito

### E9 · Save/load no navegador · M
- A carreira é salva sozinha no navegador (IndexedDB) ao começar, depois de cada rodada, na virada de temporada e quando a tática muda. Recarregar a página não perde mais nada.
- Tela "Suas carreiras": várias carreiras, continuar, apagar (com confirmação), novo jogo e importar arquivo. No jogo: "Exportar save" (arquivo .json para backup ou outro aparelho) e "Menu inicial".
- O save (C#, `Jogo.Aplicacao/Saves`) guarda o mundo inteiro com o estado de carreira de cada jogador, as rodadas disputadas (compactas) e a tática; o calendário é recalculado pela seed. Cerca de 460 KB no fim da temporada.
- Garantia testada: salvar e carregar no meio (inclusive atravessando a virada de temporada) termina com o save idêntico, byte a byte, ao de jogar direto.
- PWA: manifesto, ícones e service worker "rede primeiro" (instalável e jogável offline depois do primeiro acesso). Decisão D-026.

### E7 · Partida ao vivo · M
- "Jogar rodada" abre o jogo ao vivo: os 20 jogos correm juntos, minuto a minuto, com narração variada do seu jogo e o placar da rodada em tempo real.
- Controles: pausar/continuar (Espaço), velocidades 1×, 3× e 10×, pular para o fim. Pausa automática no intervalo, em lesão ou expulsão do seu time e no fim do jogo.
- Decisões durante o jogo: substituições (até 5, inclusive no lugar de lesionado), mentalidade e formação (o C# reposiciona quem está em campo). "Simular rápido" continua disponível.
- A rodada agora tem três passos (iniciar, avançar minuto, concluir); o modo rápido dá resultado idêntico ao de antes (verificado byte a byte no simulador). Mesma seed e mesmas decisões geram o mesmo jogo (teste). Decisão D-025.

### E6 · UI mínima (web) · G
- Jogável no navegador: escolher clube, Início (próximo jogo, posição, últimos resultados, ajustes na escalação), Elenco, Tática (formação, mentalidade, titulares, reservas, escalação automática), Partida (placar, lances, posse, estatísticas, outros jogos), Classificação (Séries A e B, zonas, artilharia) e fim de temporada com virada de ano.
- Botão "Avançar rodada" sempre visível, com atalho Espaço; cerca de 0,5 s por rodada no navegador.
- `Carreira` no C# (tática do jogador, troca automática de lesionados e suspensos com aviso) e nova camada `Jogo.Aplicacao` (casos de uso + `ApiJson`), testada sem navegador; `Jogo.Wasm` virou só a casca.
- Testada pelo navegador: temporada inteira de 38 rodadas, rebaixamento e início do ano seguinte na Série B, celular (375 px, sem rolagem lateral) e tema escuro. Decisão D-024.

### E5 · Simulação de temporada · M
- Calendário de pontos corridos (turno e returno espelhados, 19 jogos em casa, no máximo 3 seguidos no mesmo mando).
- Rodada completa com IA escalando sem lesionados e suspensos; pós-jogo no elenco: cansaço e recuperação, suspensão (vermelho e 3º amarelo), lesões, moral e entrosamento.
- Tabela com os critérios do Brasileirão (vitórias, saldo, gols pró, confronto direto, cartões, sorteio) e artilharia.
- Fim de temporada: 4 sobem e 4 descem, evolução por idade e profissionalismo, aposentadorias, revelações da base, renovação automática de contratos (até a E8).
- `tools/Jogo.Simulador` (1 temporada em ~3 s); metas de temporada na calibração: o mais forte é campeão em 67% das vezes, o campeão faz 84 pontos e o lanterna 27. Decisões D-022 e D-023.

### E4 · Motor de partida + calibração · G
- `Jogo.Motor`: força em 4 setores (contribuição por posição, ocupação com retorno decrescente), posse pelo meio-campo, duelo ataque × defesa, finalização (gol, defesa, trave, travessão, fora, por cima), escanteios, faltas, cartões, lesões, cansaço, moral, improviso fora de posição, mando e mentalidade.
- API ao vivo (`Iniciar`, `AvancarMinuto`, `AplicarDecisao`: substituição, mentalidade, formação) e modo rápido com treinador automático.
- `Escalador` (IA) escolhe formação e titulares; `Calibrador` + CLI conferem as metas, que o CI e os testes barram.
- Calibração (10 mil jogos): 2,44 gols/jogo, mandante 45%, empates 26%, favorito vence 58%; formações padrão entre 45% e 50% contra o 4-4-2; 5-0-5 e 3-0-7 perdem. Decisões D-020 e D-021.

### E2b · Migração para a web · M
- Interface trocada de Avalonia (desktop) para React + TypeScript + Vite (`web/`), com tema claro/escuro e layout para celular.
- Núcleo C# rodando no navegador via WebAssembly (`src/Jogo.Wasm`, `[JSExport]` + JSON); tela Início já mostra o mundo carregado pelo C#.
- Leitor de definições independente de disco (`IFonteDefinicoes`).
- CI: job do site (núcleo + lint + testes + build) e deploy no GitHub Pages. Decisão D-019.

### E3 · Domínio + formato de dados + seed · G
- Entidades (15 posições, atributos, força geral 1-99, Jogador, Contrato, Clube, Estádio, Divisão, Mundo) com invariantes validadas.
- Formato JSON aberto (`docs/FORMATO_DADOS.md`): um arquivo por clube, erros de patch em português com arquivo + campo, todos juntos; gravação estável byte a byte.
- Gerador determinístico com 40 clubes fictícios (6 grandes, 20 médios, 14 pequenos) e elencos de 25-30 jogadores; base em `dados/definicoes` gerada por `tools/Jogo.GeradorDados` (seed 2027).
- Decisões D-015 a D-018.

### E1 · Plano · P
Estrutura da solution, stack e roadmap definidos.

### E2 · Esqueleto · M
- Solution `.slnx` com 5 projetos em `src/`, CLI de calibração e 5 projetos de teste.
- Build centralizado (`Directory.Build.props`, `Directory.Packages.props`, `global.json`), warnings como erro.
- `IGeradorAleatorio` determinístico (xoshiro256**) com testes, incluindo trava de regressão da sequência.
- Testes de arquitetura: camadas e pureza (sem I/O, relógio ou `Random`) verificados no IL.
- App Avalonia com menu lateral, página Início, alternância de tema e textos em `.resx`.
- CI no GitHub Actions (Windows + Linux): build, testes e calibração.
- `CLAUDE.md`, `docs/DECISOES.md` e este roadmap.
