namespace Jogo.Dominio.Jogadores;

/// <summary>As 15 posições do jogo. Os dois lados de uma mesma função usam os mesmos pesos de força.</summary>
public enum Posicao
{
    Goleiro,
    Zagueiro,
    LateralDireito,
    LateralEsquerdo,
    AlaDireito,
    AlaEsquerdo,
    Volante,
    MeiaCentral,
    MeiaDireito,
    MeiaEsquerdo,
    MeiaOfensivo,
    PontaDireita,
    PontaEsquerda,
    SegundoAtacante,
    Centroavante,
}

/// <summary>Faixa do campo usada para agrupar o elenco na tela e, no motor, para somar a força dos setores.</summary>
public enum Setor
{
    Gol,
    Defesa,
    Meio,
    Ataque,
}

/// <summary>Lado do campo. Jogar no lado oposto ao de costume gera penalidade leve (definida no motor).</summary>
public enum Lado
{
    Centro,
    Direita,
    Esquerda,
}

public static class PosicaoExtensoes
{
    /// <summary>Todas as posições, em ordem fixa (do gol para o ataque).</summary>
    public static IReadOnlyList<Posicao> Todas { get; } = Enum.GetValues<Posicao>();

    public static Setor Setor(this Posicao posicao) => posicao switch
    {
        Posicao.Goleiro => Jogadores.Setor.Gol,
        Posicao.Zagueiro or Posicao.LateralDireito or Posicao.LateralEsquerdo => Jogadores.Setor.Defesa,
        // Alas contam como meio-campo (no 3-5-2 eles são dois dos cinco do meio).
        Posicao.AlaDireito or Posicao.AlaEsquerdo or Posicao.Volante or Posicao.MeiaCentral
            or Posicao.MeiaDireito or Posicao.MeiaEsquerdo or Posicao.MeiaOfensivo => Jogadores.Setor.Meio,
        Posicao.PontaDireita or Posicao.PontaEsquerda or Posicao.SegundoAtacante
            or Posicao.Centroavante => Jogadores.Setor.Ataque,
        _ => throw new ArgumentOutOfRangeException(nameof(posicao), posicao, null),
    };

    public static Lado Lado(this Posicao posicao) => posicao switch
    {
        Posicao.LateralDireito or Posicao.AlaDireito or Posicao.MeiaDireito or Posicao.PontaDireita
            => Jogadores.Lado.Direita,
        Posicao.LateralEsquerdo or Posicao.AlaEsquerdo or Posicao.MeiaEsquerdo or Posicao.PontaEsquerda
            => Jogadores.Lado.Esquerda,
        _ => Jogadores.Lado.Centro,
    };

    /// <summary>
    /// Função da posição sem o lado (ex.: LateralDireito e LateralEsquerdo → LateralDireito).
    /// Usada para buscar os pesos de força, que são iguais nos dois lados.
    /// </summary>
    public static Posicao Funcao(this Posicao posicao) => posicao switch
    {
        Posicao.LateralEsquerdo => Posicao.LateralDireito,
        Posicao.AlaEsquerdo => Posicao.AlaDireito,
        Posicao.MeiaEsquerdo => Posicao.MeiaDireito,
        Posicao.PontaEsquerda => Posicao.PontaDireita,
        _ => posicao,
    };
}
