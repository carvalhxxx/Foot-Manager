# Definições do jogo base

Ligas, clubes e jogadores **fictícios** em JSON: 40 clubes (Séries A e B) com elencos de 25 a 30 jogadores. O formato está documentado em [`docs/FORMATO_DADOS.md`](../../docs/FORMATO_DADOS.md).

Estes arquivos são **gerados** pela ferramenta `tools/Jogo.GeradorDados` com a seed 2027:

```bash
dotnet run --project tools/Jogo.GeradorDados
```

Para criar um patch, copie esta pasta, edite os arquivos e aponte o jogo para a sua cópia.
