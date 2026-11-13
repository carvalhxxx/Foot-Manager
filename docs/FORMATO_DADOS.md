# Formato de dados (definições abertas)

> **Status:** a definir na entrega E3 (ver `docs/ROADMAP.md`).

O jogo lê ligas, clubes e jogadores de arquivos JSON em `dados/definicoes/`. O formato é aberto e documentado aqui para que a comunidade possa criar "patches" (pastas de definições alternativas).

Princípios que o formato vai seguir:
- **Estável:** campos novos entram sempre como opcionais, para que patches antigos continuem funcionando.
- **Versionado:** cada arquivo traz `"versaoFormato"`.
- **Legível à mão:** nomes de campos em português, sem IDs numéricos obrigatórios. Referências entre arquivos usam identificadores de texto (`"id": "porto-alegre-fc"`).
- **Validado:** o jogo aponta o arquivo, o campo e o problema quando um patch tem erro, em vez de quebrar.
