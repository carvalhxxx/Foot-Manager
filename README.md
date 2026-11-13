# Foot Manager BR

Manager de futebol brasileiro **no navegador**: simples, leve e viciante, no estilo dos managers clássicos, com algumas mecânicas modernas simplificadas. Roda inteiro no seu navegador, inclusive no celular.

> Nome provisório. Projeto em desenvolvimento inicial: veja [`docs/ROADMAP.md`](docs/ROADMAP.md).

## Como funciona
- **Núcleo do jogo em C#** (.NET 10): regras, geração do mundo e simulação, compilados para WebAssembly.
- **Interface em React + TypeScript** (Vite), que conversa com o núcleo dentro do próprio navegador. Não há servidor.

## Requisitos
- [.NET SDK 10](https://dotnet.microsoft.com/download)
- [Node.js 24](https://nodejs.org/)

## Rodando
```bash
cd web
npm install
npm run nucleo   # compila o núcleo C# para WebAssembly
npm run dev      # abre em http://localhost:5173
```

Testes:
```bash
dotnet test --solution FootManagerBR.slnx   # núcleo C#
cd web && npm test                           # interface
```

## Documentação
- [`CLAUDE.md`](CLAUDE.md): visão geral, regras de arquitetura e convenções.
- [`docs/DECISOES.md`](docs/DECISOES.md): decisões de arquitetura.
- [`docs/FORMATO_DADOS.md`](docs/FORMATO_DADOS.md): formato aberto das definições (para patches).

## Licença
MIT. Veja [`LICENSE`](LICENSE).
