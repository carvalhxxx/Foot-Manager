# Formato de dados (definições abertas) · versão 1

O jogo lê ligas, clubes e jogadores de arquivos JSON. A pasta padrão é `dados/definicoes/`, e um **patch** é simplesmente outra pasta com a mesma estrutura.

## Estrutura da pasta

```
minha-pasta/
  manifesto.json        nome do pacote, autor e temporada inicial
  divisoes.json         divisões e quais clubes jogam em cada uma
  clubes/
    porto-alegre-fc.json   um arquivo por clube, com o elenco dentro
    uniao-paulista.json
    ...
```

Um arquivo por clube facilita editar um time sem mexer no resto e revisar mudanças no Git.

## Regras gerais

- **Codificação:** UTF-8. Acentos são escritos normalmente (`"União"`).
- **Comentários** (`// ...`) e **vírgula sobrando** no fim de listas são aceitos.
- **`versaoFormato`** é obrigatório em todo arquivo. O jogo recusa, com mensagem clara, arquivos de uma versão mais nova que a dele.
- **Identificadores** (`id` de clubes e divisões): letras minúsculas sem acento, dígitos e hífen. Ex.: `ceara-atletico`, `serie-b`.
- O **nome do arquivo do clube** precisa ser igual ao `id` dele (`clubes/ceara-atletico.json` → `"id": "ceara-atletico"`).
- **Todos os campos são obrigatórios**, exceto os marcados como opcionais.
- **Campos desconhecidos são ignorados**, para que patches feitos para versões futuras não quebrem à toa.
- Dinheiro é em **reais**, com ponto decimal (`1234567.89`).

## manifesto.json

```jsonc
{
  "versaoFormato": 1,
  "nome": "Foot Manager BR · Base oficial",
  "autor": "Equipe Foot Manager",          // opcional
  "descricao": "Clubes fictícios 2027",    // opcional
  "temporadaInicial": 2027
}
```

## divisoes.json

```jsonc
{
  "versaoFormato": 1,
  "divisoes": [
    {
      "id": "serie-a",
      "nome": "Série A",
      "nivel": 1,                 // 1 é a divisão mais alta; níveis sem buracos (1, 2, 3...)
      "vagasAcesso": 0,           // a primeira divisão não tem acesso
      "vagasRebaixamento": 4,
      "clubes": ["porto-alegre-fc", "uniao-paulista", "..."]
    },
    {
      "id": "serie-b",
      "nome": "Série B",
      "nivel": 2,
      "vagasAcesso": 4,           // precisa ser igual ao rebaixamento da divisão de cima
      "vagasRebaixamento": 0,     // a última divisão não rebaixa
      "clubes": ["ceara-atletico", "..."]
    }
  ]
}
```

Regras:
- Número **par** de clubes por divisão (mínimo 2). Uma divisão de N clubes tem (N − 1) × 2 rodadas.
- Cada clube aparece em **uma e só uma** divisão, e todo arquivo em `clubes/` precisa estar em alguma divisão.
- Acesso e rebaixamento valem no máximo metade dos clubes da divisão.

## clubes/&lt;id&gt;.json

```jsonc
{
  "versaoFormato": 1,
  "id": "porto-alegre-fc",
  "nome": "Porto Alegre FC",
  "sigla": "PAF",                      // 3 letras maiúsculas, sem acento
  "cidade": "Porto Alegre",
  "uf": "RS",                          // sigla de estado brasileiro válida
  "cores": { "primaria": "#0A3D91", "secundaria": "#FFFFFF" },   // formato #RRGGBB
  "estadio": { "nome": "Arena do Guaíba", "capacidade": 45000 }, // 1.000 a 250.000
  "torcida": 70,                       // tamanho da torcida, 1 a 100
  "reputacao": 65,                     // 1 a 100
  "caixa": 5000000.00,                 // saldo inicial em reais (pode ser negativo)
  "elenco": [
    {
      "id": 1001,                      // número inteiro positivo, único em todo o mundo
      "nome": "Luizão",
      "idade": 27,                     // 15 a 45
      "posicao": "ATA",
      "posicoesSecundarias": ["SA"],   // opcional, no máximo 2, sem repetir a principal
      "atributos": {                   // todos de 1 a 20
        "finalizacao": 17, "passe": 10, "desarme": 4, "drible": 13,
        "velocidade": 14, "resistencia": 12, "decisao": 15, "determinacao": 13,
        "reflexos": 2, "posicionamento": 3
      },
      "ocultos": {
        "potencial": 82,               // teto de força geral, 1 a 99
        "propensaoLesao": 8,           // 1 a 20 (maior = mais lesões)
        "profissionalismo": 14         // 1 a 20 (maior = evolui melhor)
      },
      "salario": 85000.00,             // mensal, em reais (maior que zero)
      "contratoAte": 2029              // última temporada do contrato
    }
  ]
}
```

### Posições (siglas)

| Sigla | Posição | Sigla | Posição |
|---|---|---|---|
| `GOL` | Goleiro | `MC` | Meia Central |
| `ZAG` | Zagueiro | `MD` | Meia Direito |
| `LD` | Lateral Direito | `ME` | Meia Esquerdo |
| `LE` | Lateral Esquerdo | `MO` | Meia Ofensivo |
| `ALD` | Ala Direito | `PD` | Ponta Direita |
| `ALE` | Ala Esquerdo | `PE` | Ponta Esquerda |
| `VOL` | Volante | `SA` | Segundo Atacante |
| | | `ATA` | Centroavante |

### Atributos

Todo jogador tem os 10 atributos. Os goleiros devem ter Finalização e Drible baixos; os jogadores de linha, Reflexos e Posicionamento baixos. A **força geral** (1-99) não é gravada: o jogo calcula a partir dos atributos e da posição principal (ver D-015 em `docs/DECISOES.md`).

## Erros

Quando um patch tem problemas, o jogo lista **todos** de uma vez, cada um com arquivo, local e motivo. Exemplo:

```
clubes/porto-alegre-fc.json: Sigla "pa" inválida: use 3 letras maiúsculas sem acento.
clubes/porto-alegre-fc.json → elenco[1]: idade deve estar entre 15 e 45.
clubes/porto-alegre-fc.json → elenco[2].posicao: Posição "XYZ" desconhecida. Use uma de: GOL, ZAG, LD, ...
(conjunto): A divisão "serie-b" cita o clube "goiania-ec", que não existe.
```

Erros marcados como `(conjunto)` envolvem mais de um arquivo (clube ausente, jogador com id repetido, acesso e rebaixamento que não batem).

## Evolução do formato

- Campos novos entram sempre como **opcionais**, então patches da versão 1 continuam funcionando.
- Mudanças incompatíveis aumentam `versaoFormato`, e o jogo converte automaticamente as versões antigas que ainda suportar.
- Já previstos para a Fase 2 (opcionais): `estadio.gramado`, `estadio.estadoGramado`, `regiaoClimatica` e coordenadas da cidade.
