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

### D-010 · Adiar o SQLite para depois do motor e da temporada (2026-11-13, era P-001) · **substituída pela D-026**
- **Motivo:** o domínio vai mudar muito durante a calibração. Até lá, o mundo roda em memória, carregado do JSON, e o schema nasce já estável.
- **Impacto:** a entrega 3 passa a ser "Domínio + JSON + gerador de seed"; o SQLite vai para a entrega de save/load.

### D-011 · Dapper em vez de EF Core (2026-11-13, era P-002) · **substituída pela D-026**
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

### D-025 · Partida ao vivo (2026-11-26)
- **Decisão:** a `Temporada` joga a rodada em três passos (`IniciarRodada`, `RodadaEmAndamento.AvancarMinuto`, `ConcluirRodada`); o modo rápido é só "avançar até o fim". Na rodada ao vivo, o time do jogador fica sem treinador automático e as decisões passam por `Carreira` (substituição, mentalidade, formação com reposicionamento pelo `Escalador.Distribuir`). A API devolve sempre a foto completa do jogo, e o C# indica quando pausar (intervalo, lesão ou expulsão do time do jogador, fim). A narração é texto de interface: fica em `web/src/narracao.ts`, com vários modelos por tipo de lance, escolhidos por um hash do lance (o mesmo lance sempre tem a mesma frase).
- **Consequência:** cada jogo usa o seu gerador, então o ritmo não muda resultados: o modo rápido continua idêntico ao anterior. Na tela, uma trava síncrona impede que um minuto extra passe depois de uma pausa na velocidade 10×. Abrir a rodada leva cerca de 0,5 s (a IA escalando os 40 clubes); cada minuto é instantâneo.

### D-026 · Saves no navegador (2026-11-27, era P-006)
- **Decisão:** sem SQLite nem Dapper. O save é a carreira serializada em JSON pelo C# (`ConversorSave`), guardada no IndexedDB do navegador (resumos e conteúdos em "tabelas" separadas) e exportável como arquivo. Guarda o mundo inteiro (o save não depende das definições do jogo base nem de patches), as rodadas disputadas de forma compacta (placar, cartões, quem jogou, quem marcou) e os lances completos só do jogo do jogador na última rodada. O calendário não é salvo: é recalculado pela seed. Salvamento automático depois de cada rodada; não dá para salvar no meio de um jogo ao vivo (fechar a página no meio volta ao fim da rodada anterior). PWA com service worker "rede primeiro".
- **Consequência:** a garantia "salvar e carregar = jogar direto" é testada comparando o save final byte a byte. Mudanças futuras no formato aumentam `VersaoFormato`; saves de versão mais nova são recusados com mensagem clara. Sem compressão por enquanto (~460 KB no fim da temporada; ~70 KB se comprimido, caso precise no futuro).

### D-027 · Economia do clube (2026-11-27)
- **Decisão:** a cada rodada, cada clube recebe 1/38 da cota de TV (linear na reputação; Série B recebe 35%) e do patrocínio (reputação³ × fator, para crescer no ritmo da folha dos grandes) e paga 12/38 da folha mensal. O mandante recebe a bilheteria: público = torcida × 300 × fase (últimos 5 jogos, ±30%) × atração do adversário (até +30%) × (preço sugerido ÷ preço)^1,3, limitado à capacidade. O preço sugerido é reputação × R$ 0,35 (mínimo R$ 5); o jogador pode definir o próprio preço. No fim da temporada, a premiação vai de R$ 10 mi (campeão da Série A) a 1/20 disso para o último (Série B: 25%). Tudo fica num extrato por categoria, zerado a cada temporada; o caixa continua.
- **Consequência:** calibrado para uma temporada sem mercado: grandes ≈ +R$ 7 mi, pequenos ≈ +R$ 1 mi, nenhum clube no vermelho. As metas entram na calibração e no CI. O save foi para a versão 2 (extrato, preço do ingresso e último jogo em casa); saves da versão 1 continuam carregando.

### D-028 · Contratos e agentes livres (2026-11-27)
- **Contexto:** até aqui todo contrato vencido era renovado automaticamente por 2 anos, e ninguém saía dos clubes a não ser por aposentadoria.
- **Decisão:** valor de mercado = R$ 60 mil × 1,15^(força − 40), multiplicado pela idade (jovem com margem de potencial vale até 1,4× ou mais; a partir dos 31 cai) e pelo contrato (último ano: 0,5×; dois anos: 0,8×). O salário pedido é o de mercado para a força, de +20% (moral baixa) a 0% (moral alta), nunca abaixo de 90% do atual. O jogador renova só no último ano de contrato, por 1 a 4 anos. Dispensar custa 3 salários (categoria Rescisões) e exige elenco acima de 18. Quem não renova sai de graça no fim da temporada. A IA renova jogadores de até 30 anos e os de até 33 que estejam entre os 18 melhores do elenco; o resto vira agente livre. Os agentes livres ficam no `Mundo` (no máximo 150, os mais fortes), envelhecem e se aposentam como os demais.
- **Consequência:** a renovação automática acabou. A calibração da temporada e das finanças continua dentro das metas. Os agentes livres entram no save como campo opcional (a versão continua 2, e saves sem o campo carregam com a lista vazia).

### D-029 · Mercado de transferências (2026-11-27)
- **Contexto:** a E8 pedia comprar e vender sem leilões, empréstimos ou negociação longa (regra dos 10 segundos).
- **Decisão:** há duas janelas, antes das rodadas 1 a 3 (pré-temporada) e antes das rodadas 18 a 21. Agentes livres podem ser contratados a qualquer momento. A compra é por preço fixo (valor de mercado × 1,2) e pelo salário pedido, com contrato de 4 anos até os 25, encurtando com a idade até 1 ano. Tudo se resolve numa única confirmação. A venda vai para a melhor proposta imediata da IA: clubes que teriam o jogador entre os 18 melhores, com espaço e caixa, pagam o valor de mercado, ou +10% se ele chegar como titular. Elenco entre 18 e 32 jogadores. Depois de cada rodada com a janela aberta, cada clube da IA com caixa positivo tem 15% de chance de reforçar a posição mais fraca (força pelo menos 3 acima, até metade do caixa, com salário compatível). Clubes no vermelho vendem o reserva mais valioso. A IA nunca mexe no elenco do jogador. O sorteio usa a seed do ano e da rodada.
- **Consequência:** nada de novo no save (contratos, caixa e extrato já são salvos), e a garantia "salvar e carregar = jogar direto" continua valendo. A calibração segue dentro das metas. Na primeira temporada ainda não há agentes livres; eles surgem com dispensas e na virada do ano.

### D-030 · Polimento para o lançamento (2026-11-27)
- **Contexto:** a calibração media só uma temporada isolada, sem mercado. Faltava saber se, ao longo dos anos, surge uma dinastia ou se o dinheiro se concentra.
- **Decisão:** a calibração ganhou "carreiras com mercado": 6 carreiras de 5 anos seguidos, com acesso, contratos e IA negociando. As metas são de 2 a 4,5 campeões diferentes, no máximo 3,5 títulos para o maior campeão, de 15 a 80 transferências da IA por ano, no máximo 60% do caixa com os 4 mais ricos e elencos entre 18 e 35. Para dar rotatividade, a chance de a IA reforçar o time subiu de 15% para 25% por rodada de janela. Toda carreira nova começa com 40 agentes livres (nível de Série B, todas as posições), sorteados pela seed. Isso não acontece ao carregar um save.
- **Consequência:** com a seed 1, as carreiras têm em média 3 campeões diferentes em 5 anos, cerca de 28 transferências da IA por ano e 3% de clubes no vermelho. A calibração no CI passou a levar cerca de 2 minutos.

### D-031 · Tática no campo, arrastando (2026-11-27)
- **Contexto:** escalar com listas de seleção era lento e pouco visual. O dono do projeto pediu um campo em que se arrastam os jogadores.
- **Decisão:** a tela Tática mostra os titulares num campo desenhado, nas posições da formação, com a força na posição e o aviso de improvisado. Ao lado ficam o banco (7 vagas) e quem está fora do time. Há duas formas de trocar: arrastar (pointer events, funciona com mouse e com toque) ou tocar em um jogador e depois no outro (funciona com teclado e em telas pequenas). A interface só rearruma as vagas; quem valida a escalação continua sendo o núcleo C#. O visual é próprio, sem copiar outros jogos.
- **Consequência:** as coordenadas no campo e as regras de troca de vaga ficam em `web/src/telas/campoTatico.ts`, com testes próprios.

### D-032 · Marcação, concentração, capitão e cobrador (2026-11-27)
- **Contexto:** o dono do projeto pediu essas quatro opções táticas. Cada uma precisa caber numa linha e não pode virar a "tática vencedora".
- **Decisão:**
  - **Marcação** Leve/Normal/Forte. A forte tira 10% dos lances do adversário, dá +35% de faltas (e de cartões) e +15% de desgaste. A leve faz o contrário.
  - **Concentrar ataques** Variado/Esquerda/Meio/Direita. O ataque ganha ou perde até 6% conforme a força média de quem ataca por aquele lado em relação a quem defende aquele lado no rival. Uma vantagem de 15% já dá o efeito máximo. É uma versão simples do modelo de zonas da Fase 2E.
  - **Capitão** (padrão: o titular de maior Determinação). Dá até +2% de força enquanto está em campo, e com ele em campo a derrota tira metade da moral.
  - **Cobrador de faltas** (opcional). Bate as faltas diretas com Finalização e Passe e melhora os escanteios pelo Passe (até +15%). Se sair de campo, o melhor em campo assume. Sem cobrador escolhido, bate quem estiver na jogada, como antes.
  - Marcação e concentração podem ser trocadas no jogo ao vivo (evento `TrocaDeEstilo`). Tudo é salvo (campos opcionais na tática do save). A IA usa Normal/Variado, o capitão automático e não escolhe cobrador.
- **Consequência:** a calibração ganhou 5 metas, cada opção contra o padrão entre 45% e 55% dos pontos (resultado: de 50,0% a 52,3%). Um cobrador automático "ideal" para todos os times deixava o mais forte campeão em cerca de 78% das temporadas (meta: até 75%). Por isso o cobrador só existe quando o jogador escolhe, e escolher vira uma vantagem pequena e real.

### D-033 · Clubes e jogadores reais (2026-11-27)
- **Contexto:** até aqui o jogo usava 40 clubes e jogadores fictícios, por regra do `CLAUDE.md`. O dono do projeto decidiu usar os clubes reais das Séries A e B de 2026 e os elencos reais de novembro de 2026.
- **Riscos avisados antes da decisão:** nomes de clubes são marcas registradas, e nome e imagem de atletas têm proteção legal (os jogos licenciados pagam por isso). A recomendação foi manter o jogo publicado fictício e permitir importar dados localmente. O dono decidiu usar os nomes reais mesmo assim; antes de um lançamento comercial, vale ouvir um advogado.
- **Decisão:** nomes reais de clubes, cidades, estádios e cores, e nomes, idades e posições reais dos jogadores. Escudos, logotipos e fotos ficam de fora. Os atributos são estimados por nós (valor de mercado, idade, posição); nada é copiado de outros jogos ou de patches de comunidade.
- **Como foi feito:** a composição das Séries A e B de 2026 e os estádios vêm da Wikipédia (26/11/2026). Os elencos vêm do ogol.com.br (nome, idade e valor de mercado, 34 clubes) e, para São Paulo, Grêmio, Fortaleza e Novorizontino, da Wikipédia (sem idade nem valor). O Transfermarkt bloqueia leitura automática. Avaí e América-MG ficaram sem fonte: o elenco deles é gerado até alguém trazer os dados. Os dados brutos ficam em `tools/Jogo.GeradorDados/reais/` (texto com "|"), e o gerador (`GeradorDeMundoReal`) estima a força pelo valor (100 mil € ≈ 50, 1 mi € ≈ 61, 10 mi € ≈ 73, 60 mi € ≈ 80, com ajuste por idade). Quem não tem valor fica no quartil de baixo do elenco, e as posições que a fonte só dá por grupo (defensor, meia, atacante) são distribuídas para o elenco ter laterais, volantes e pontas. A temporada inicial passa a ser 2026.
- **Consequência:** os dados reais ficam desatualizados a cada janela de transferências; atualizar é editar os `.txt` e rodar o gerador. Para a calibração continuar passando com os clubes reais: patrocínio com fator 8 (era 18); a IA reforça qualquer posição que o jogador saiba jogar e compra com até 32 no elenco; e a meta "o mais forte é campeão" virou "um dos 2 mais fortes é campeão" (40% a 85%), porque Flamengo e Palmeiras começam praticamente empatados. O catálogo fictício (`CatalogoBase`) continua só para os testes.

### D-034 · Tela inicial com o elenco e a partida ao lado (2026-11-27)
- **Contexto:** o dono do projeto pediu a organização clássica dos managers: o elenco como tela principal e, ao lado, só os dados da próxima partida.
- **Decisão:** o menu foi para uma barra no topo (clube, telas, save e tema). Uma coluna à esquerda mostra o próximo jogo (adversário, mando, rodada, força do rival), os botões da rodada, a posição, a caixa, os últimos resultados e os avisos. A tela inicial passou a ser o Elenco, em tabela densa (uma linha por jogador). A antiga tela "Início" deixou de existir; o conteúdo dela está na coluna. No jogo ao vivo e no fim de temporada a coluna some. No celular, a coluna fica acima do conteúdo.
- **Consequência:** layout próprio (sem copiar o visual de outros jogos); o que muda é só a organização das telas.

### D-035 · Playoff de acesso na Série B (2026-11-27)
- **Contexto:** o dono do projeto pediu o formato da Série B de 2026: 2 sobem direto e do 3º ao 6º há playoff de ida e volta (3º x 6º e 4º x 5º).
- **Decisão:** a divisão ganhou `vagasDiretas` (opcional no formato de dados e no save). Quando a fase de pontos corridos termina, a temporada cria duas rodadas extras só para quem tem playoff: a ida na casa do pior colocado e a volta na casa do melhor. Vale o agregado; empate classifica o melhor colocado da fase de pontos corridos (o motor ainda não tem pênaltis, previstos para a Copa do Brasil, Fase 2C). Sobem os 2 primeiros e os vencedores do playoff; o rebaixamento não muda. A tabela conta só os pontos corridos. Clubes de outras divisões não jogam nessas rodadas: a coluna e a tela ao vivo mostram só os outros jogos.
- **Consequência:** a temporada tem 40 rodadas (38 + 2). A garantia "salvar e carregar = jogar direto" foi testada no meio do playoff; a calibração segue dentro das metas.
