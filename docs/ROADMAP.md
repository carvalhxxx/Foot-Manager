# Roadmap (kanban)

Estimativas: **P** (até 1 dia) · **M** (2 a 4 dias) · **G** (1 semana ou mais).
Cada entrega termina com testes passando, commit claro e este arquivo atualizado.

## Em andamento

_(nada)_

## Refinando

_(nada)_

## Backlog (MVP · Fase 1)

- **Competições continentais:** Libertadores e Sul-Americana disputadas, usando `VagasContinentais` (D-037) com o campeão e o vice da Copa do Brasil para classificar os brasileiros a cada ano.
- **Copa do Brasil ao vivo:** jogar o jogo da Copa na tela ao vivo, com decisões e pênaltis lance a lance.

## Fase 2 (depois do lançamento do MVP)
- **2.0 Calendário real:** datas, meses, jogos no meio de semana (pré-requisito do resto).
- **2A Carreira:** diretoria (metas, confiança, demissão, propostas), notícias/jornal, prêmios da temporada, categorias de base.
- **2B Editor** de ligas, clubes e jogadores + importação/exportação de patches.
- **2C Competições:** Série D, estaduais.
- **2D Estádio e economia:** ampliação, melhorias, estado do gramado, empréstimo bancário, treino simples.
- **2E Refinos do motor:** modelo de zonas (corredores central/lados), estilos de jogo, mando decomposto (torcida, tipo de gramado, adaptação), clima (chuva/calor por região e mês), cansaço de viagem, torneio de calibração de táticas.
- **2F Continental:** Libertadores, Sul-Americana, altitude.

## Fase 3
Mobile, saves na nuvem, ligas online entre amigos, modelo freemium/assinatura.

## Feito

### Copa do Brasil · M
- Mata-mata com os 60 clubes: 4 cabeças na 2ª fase, 1ª fase em jogo único na casa do clube de divisão mais baixa, ida e volta até a final, pênaltis no empate do agregado. Jogos no meio de semana depois de 11 rodadas do Brasileirão, simulados (sem ao vivo), com prêmio por fase. Aba Copa do Brasil na Classificação, situação na coluna da partida, cartão do jogo na tela da Partida e campeão no fim de temporada (D-040).

### Série C · P
- 20 clubes reais da Série C 2026 com elencos do ogol (4 gerados por falta de fonte), turno e returno, 4 sobem para a B; a B passa a rebaixar 4 (D-039).

### Elencos atuais pela API-Football (só local) · P
- Importador com cache e cruzamento com os dados pesquisados; modo `npm run dev:local` no site; nada da API vai para o repositório nem para o site publicado (D-038).

### Vagas continentais no padrão 2026 · P
- Tabela com 1º a 4º Libertadores (grupos), 5º preliminar e 6º a 11º Sul-Americana; motor de redistribuição das vagas (títulos continentais, Copa do Brasil, rebaixados) testado com os exemplos do regulamento (D-037).

### Regras e legenda da tabela · P
- Faixas de Libertadores, fase preliminar, Sul-Americana, acesso, playoff e rebaixamento vindas do núcleo, e os critérios de desempate no quadro "Regras e legenda" (D-036).

### Playoff de acesso na Série B · P
- 1º e 2º sobem direto; 3º x 6º e 4º x 5º em ida e volta depois da rodada 38 (volta na casa do melhor; empate no agregado classifica o melhor colocado). Zonas e confrontos na Classificação; rodada ao vivo sem jogo do meu clube mostra os outros jogos (D-035).

### Tela inicial com o elenco · P
- Menu no topo, coluna da próxima partida à esquerda (adversário, mando, rodada, botões da rodada, posição, caixa, últimos resultados e avisos) e o elenco como tela principal, em tabela densa (D-034).

### Clubes e jogadores reais · M
- As Séries A e B de 2026 com os 40 clubes reais (nomes, cidades, estádios e cores, sem escudos) e os elencos de novembro de 2026 (nome, idade e valor de mercado de fontes públicas). Atributos estimados pelo nosso gerador a partir do valor de mercado. Temporada inicial 2026. Calibração ajustada para os dados reais (D-033).
- **Pendente:** elencos reais de Avaí e América-MG (sem fonte acessível; hoje gerados), idades e valores de São Paulo, Grêmio, Fortaleza e Novorizontino, e atacantes de Remo e Mirassol (a fonte cortou a lista).

### Opções táticas · M
- Marcação (leve, normal, forte), concentrar ataques (variado, esquerda, meio, direita), capitão e cobrador de faltas. Na tela Tática (toque no jogador do campo para fazer capitão ou cobrador, com os selos C e F), e marcação e concentração também no jogo ao vivo. Calibradas para nenhuma opção dominar (D-032).

### Tática no campo · P
- Escalação arrastando os jogadores num campo (ou tocando em um e depois no outro), com banco e lista de quem está fora do time. Verificada no navegador com mouse e na largura de celular (D-031).

### E10 · Polimento para o lançamento do MVP · M
- Calibração de carreiras de 5 anos com mercado (rotatividade de campeões, concentração de dinheiro, tamanho dos elencos) no CI. A IA ficou mais ativa no mercado (D-030).
- Carreira nova começa com 40 agentes livres.
- Tela da partida mostra o "Mercado da rodada" (transferências da IA).
- Início avisa, nas últimas 8 rodadas, quantos jogadores estão no último ano de contrato.
- Tela de escolha de clube com 3 dicas de "Como jogar".
- Verificado no navegador (desktop e 375 px): novo jogo, rodada com transferências, mercado com agentes livres.
- **Pendente:** nome definitivo do jogo e ícone (decisão do dono do projeto), e teste de instalação/offline como PWA no Chrome do celular.

### E8 · Mercado, contratos e finanças · G
- **Finanças (8a):** bilheteria com preço do ingresso escolhido pelo jogador, cota de TV, patrocínio e salários a cada rodada, premiação no fim da temporada, extrato por categoria e tela Finanças. Metas de economia na calibração (D-027).
- **Contratos (8b):** valor de mercado, renovação no Elenco (1 a 4 anos, pelo salário pedido), dispensa com 3 salários de rescisão e agentes livres. Quem não renova sai de graça. A IA renova os úteis e libera os veteranos fracos (D-028).
- **Mercado (8c):** janelas antes das rodadas 1–3 e 18–21; agentes livres a qualquer momento. Tela Mercado com busca por posição, força mínima, preço máximo e só livres; compra por preço fixo com uma confirmação; venda pelo Elenco para a melhor proposta. A IA reforça a posição mais fraca e vende quando fica no vermelho. Aviso de janela no Início (D-029).
- Três temporadas simuladas com mercado: elencos entre 20 e 35 e no máximo 20% dos clubes no vermelho (teste).

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
