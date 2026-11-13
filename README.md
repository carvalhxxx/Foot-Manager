# Foot Manager BR

Manager de futebol brasileiro para PC: simples, leve e viciante, no estilo dos managers clássicos, com algumas mecânicas modernas simplificadas.

> Nome provisório. Projeto em desenvolvimento inicial: veja [`docs/ROADMAP.md`](docs/ROADMAP.md).

## Requisitos
- [.NET SDK 10](https://dotnet.microsoft.com/download)

## Rodando
```bash
dotnet build FootManagerBR.slnx
dotnet test --solution FootManagerBR.slnx
dotnet run --project src/Jogo.App
```

## Documentação
- [`CLAUDE.md`](CLAUDE.md): visão geral, regras de arquitetura e convenções.
- [`docs/DECISOES.md`](docs/DECISOES.md): decisões de arquitetura.
- [`docs/FORMATO_DADOS.md`](docs/FORMATO_DADOS.md): formato aberto das definições (para patches).

## Licença
MIT. Veja [`LICENSE`](LICENSE).
