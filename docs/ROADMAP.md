# Roadmap (kanban)

Estimativas: **P** (até 1 dia) · **M** (2 a 4 dias) · **G** (1 semana ou mais).
Cada entrega termina com testes passando, commit claro e este arquivo atualizado.

## Em andamento

_(nada)_

## Refinando

_(nada)_

## Backlog (MVP · Fase 1)

### E5 · Simulação de temporada · M
- **Objetivo:** calendário de pontos corridos (38 rodadas), rodada completa, tabela, fim de temporada com acesso/rebaixamento, tudo via CLI, sem UI.
- **Aceite:** uma temporada inteira simulada pela CLI; tabela com os critérios de desempate; o time mais forte vence com frequência alta, mas não absoluta.
- **Inclui:** aplicar o pós-jogo ao elenco (condição, suspensões por cartões, lesões, moral por resultado) e o **entrosamento** (bônus leve para quem joga junto há várias rodadas), que depende do histórico de rodadas.

### E6 · UI mínima (web) · G
- **Objetivo:** telas Início, Elenco, Tática, Partida (resultado + eventos), Classificação e botão grande "Avançar rodada" com atalho de teclado, em React, funcionando no desktop e no celular.
- **Aceite:** é possível jogar uma temporada inteira pela interface.

### E7 · Partida ao vivo · M
- **Objetivo:** narração minuto a minuto, placar da rodada em tempo real, substituições e troca de mentalidade durante o jogo.
- **Aceite:** decisões no meio da partida alteram o andamento; a mesma seed com as mesmas decisões gera o mesmo jogo.

### E8 · Mercado, contratos e finanças · G
- **Objetivo:** comprar, vender e dispensar (com a IA também negociando), janelas de transferência, contratos e renovação, bilheteria com preço do ingresso, patrocínio, TV, premiação e salários.
- **Aceite:** uma temporada simulada fecha o caixa de forma coerente para todos os clubes.

### E9 · Save/load no navegador · M
- **Objetivo:** vários saves guardados no navegador (IndexedDB), com exportar/importar arquivo (P-006). Site instalável como PWA.
- **Aceite:** salvar no meio da temporada, fechar e carregar continua exatamente igual (incluindo o estado do gerador aleatório).

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
