# Roadmap (kanban)

Estimativas: **P** (até 1 dia) · **M** (2 a 4 dias) · **G** (1 semana ou mais).
Cada entrega termina com testes passando, commit claro e este arquivo atualizado.

## Em andamento

_(nada)_

## Refinando

### E3 · Domínio + formato de dados + seed · G
- **Objetivo:** entidades principais (Jogador com 8 atributos + ocultos, Clube, Contrato, Estádio, Liga), formato JSON documentado e gerador dos 40 clubes fictícios.
- **Aceite:**
  - `docs/FORMATO_DADOS.md` descreve o formato;
  - o gerador cria Série A e B com 20 clubes cada, elencos de 25-30 jogadores e distribuição de força coerente (grandes, médios, pequenos);
  - mesma seed gera o mesmo mundo;
  - o JSON faz ida e volta (carregar → salvar → carregar) sem perda.
- **Depende de:** confirmar P-001 (SQLite adiado) e P-003 (moral por jogador).

## Backlog (MVP · Fase 1)

### E4 · Motor de partida + calibração · G
- **Objetivo:** motor estatístico minuto a minuto (setores → posse → duelo → finalização), determinístico com seed. API `Iniciar` / `AvancarMinuto` / `AplicarDecisao` e modo rápido.
- **Aceite:**
  - a CLI de calibração simula 10.000+ partidas e confere: média de gols entre 2,3 e 2,7; mandante vence cerca de 45%; empates entre 25 e 28%;
  - formações extremas (5-0-5, 3-0-7) não dominam;
  - o CI falha se alguma meta sair da margem.

### E5 · Simulação de temporada · M
- **Objetivo:** calendário de pontos corridos (38 rodadas), rodada completa, tabela, fim de temporada com acesso/rebaixamento, tudo via CLI, sem UI.
- **Aceite:** uma temporada inteira simulada pela CLI; tabela com os critérios de desempate; o time mais forte vence com frequência alta, mas não absoluta.

### E6 · UI mínima · G
- **Objetivo:** telas Início, Elenco, Tática, Partida (resultado + eventos), Classificação e botão grande "Avançar rodada" com atalho de teclado.
- **Aceite:** é possível jogar uma temporada inteira pela interface.

### E7 · Partida ao vivo · M
- **Objetivo:** narração minuto a minuto, placar da rodada em tempo real, substituições e troca de mentalidade durante o jogo.
- **Aceite:** decisões no meio da partida alteram o andamento; a mesma seed com as mesmas decisões gera o mesmo jogo.

### E8 · Mercado, contratos e finanças · G
- **Objetivo:** comprar, vender e dispensar (com a IA também negociando), janelas de transferência, contratos e renovação, bilheteria com preço do ingresso, patrocínio, TV, premiação e salários.
- **Aceite:** uma temporada simulada fecha o caixa de forma coerente para todos os clubes.

### E9 · Save/load + instalador Windows · M
- **Objetivo:** vários saves (um `.db` SQLite por save, no perfil do usuário) e build instalável para Windows.
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
