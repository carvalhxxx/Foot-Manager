namespace Jogo.Simulacao.Geracao;

/// <summary>Porte do clube: define força do elenco, torcida, reputação, caixa e estádio.</summary>
public enum Porte
{
    Grande,
    Medio,
    Pequeno,
}

/// <summary>Molde de um clube a ser gerado.</summary>
public sealed record ModeloClube(
    string Id,
    string Nome,
    string Sigla,
    string Cidade,
    string Uf,
    string CorPrimaria,
    string CorSecundaria,
    Porte Porte,
    int Divisao);

/// <summary>
/// Os 40 clubes fictícios do jogo base (Séries A e B), em cidades brasileiras reais.
/// Os nomes são inventados a partir de apelidos de cidades, rios e regiões, de propósito
/// para não coincidir com clubes profissionais reais.
/// </summary>
public static class CatalogoBase
{
    public static IReadOnlyList<ModeloClube> Clubes { get; } =
    [
        // Série A: 6 grandes
        new("imperial-paulista", "Imperial Paulista FC", "IMP", "São Paulo", "SP", "#5B1E8C", "#FFFFFF", Porte.Grande, 1),
        new("guanabara", "SE Guanabara", "GUA", "Rio de Janeiro", "RJ", "#0B6E3A", "#FFFFFF", Porte.Grande, 1),
        new("atletico-carioca", "Atlético Carioca de Regatas", "ACR", "Rio de Janeiro", "RJ", "#111111", "#E30613", Porte.Grande, 1),
        new("alterosa", "Alterosa FC", "ALT", "Belo Horizonte", "MG", "#1E3A8A", "#F5C400", Porte.Grande, 1),
        new("pampa", "Pampa Sport Club", "PAM", "Porto Alegre", "RS", "#C8102E", "#FFFFFF", Porte.Grande, 1),
        new("soteropolitano", "EC Soteropolitano", "SOT", "Salvador", "BA", "#0057B8", "#E30613", Porte.Grande, 1),

        // Série A: 14 médios
        new("bandeirantes", "Bandeirantes AC", "BAN", "Campinas", "SP", "#006341", "#111111", Porte.Medio, 1),
        new("litoral-santista", "Litoral Santista FC", "LIT", "Santos", "SP", "#00A3E0", "#FFFFFF", Porte.Medio, 1),
        new("capibaribe", "Capibaribe FC", "CAP", "Recife", "PE", "#D4001A", "#F5C400", Porte.Medio, 1),
        new("pinheirais", "Pinheirais EC", "PIN", "Curitiba", "PR", "#1B5E20", "#FFFFFF", Porte.Medio, 1),
        new("iracema", "Iracema SC", "IRA", "Fortaleza", "CE", "#0033A0", "#E30613", Porte.Medio, 1),
        new("cerrado", "Cerrado FC", "CER", "Goiânia", "GO", "#E87722", "#006341", Porte.Medio, 1),
        new("ilha-da-magia", "Ilha da Magia FC", "ILH", "Florianópolis", "SC", "#0077C8", "#FFFFFF", Porte.Medio, 1),
        new("manauara", "Manauara EC", "MAN", "Manaus", "AM", "#007A33", "#F5C400", Porte.Medio, 1),
        new("pantanal", "Pantanal AC", "PAN", "Cuiabá", "MT", "#2E7D32", "#F5C400", Porte.Medio, 1),
        new("marajoara", "Marajoara FC", "MAR", "Belém", "PA", "#002F6C", "#FFFFFF", Porte.Medio, 1),
        new("planalto-central", "Planalto Central FC", "PLA", "Brasília", "DF", "#FFFFFF", "#0033A0", Porte.Medio, 1),
        new("mantiqueira", "Mantiqueira EC", "MTQ", "Juiz de Fora", "MG", "#6D1F2C", "#FFFFFF", Porte.Medio, 1),
        new("vale-do-paraiba", "Vale do Paraíba FC", "VAL", "São José dos Campos", "SP", "#003DA5", "#F5C400", Porte.Medio, 1),
        new("capixaba", "Capixaba AC", "CPX", "Vitória", "ES", "#F5C400", "#111111", Porte.Medio, 1),

        // Série B: 6 médios (tradicionais)
        new("serra-gaucha", "Serra Gaúcha EC", "SER", "Caxias do Sul", "RS", "#7B1FA2", "#FFFFFF", Porte.Medio, 2),
        new("triangulo-mineiro", "Triângulo Mineiro FC", "TRI", "Uberlândia", "MG", "#00843D", "#FFFFFF", Porte.Medio, 2),
        new("pe-vermelho", "Pé Vermelho FC", "PEV", "Londrina", "PR", "#A6192E", "#FFFFFF", Porte.Medio, 2),
        new("california-paulista", "Califórnia Paulista FC", "CAL", "Ribeirão Preto", "SP", "#E4002B", "#111111", Porte.Medio, 2),
        new("icarai", "Icaraí FC", "ICA", "Niterói", "RJ", "#00A3AD", "#FFFFFF", Porte.Medio, 2),
        new("princesa-do-sertao", "Princesa do Sertão FC", "PRS", "Feira de Santana", "BA", "#FFB81C", "#0033A0", Porte.Medio, 2),

        // Série B: 14 pequenos
        new("dunas", "Dunas FC", "DUN", "Natal", "RN", "#F2C14E", "#111111", Porte.Pequeno, 2),
        new("jangadeiro", "Jangadeiro EC", "JAN", "Maceió", "AL", "#0081C6", "#FFFFFF", Porte.Pequeno, 2),
        new("borborema", "Borborema FC", "BOR", "Campina Grande", "PB", "#8B0000", "#FFFFFF", Porte.Pequeno, 2),
        new("poti", "Poti EC", "POT", "Teresina", "PI", "#228B22", "#FFFFFF", Porte.Pequeno, 2),
        new("ludovicense", "Ludovicense FC", "LUD", "São Luís", "MA", "#0033A0", "#E30613", Porte.Pequeno, 2),
        new("atalaia", "Atalaia FC", "ATI", "Aracaju", "SE", "#FF6F00", "#0033A0", Porte.Pequeno, 2),
        new("lagoa-dos-patos", "Lagoa dos Patos FC", "LAG", "Pelotas", "RS", "#4A148C", "#F5C400", Porte.Pequeno, 2),
        new("sorocabano", "Sorocabano AC", "SOR", "Sorocaba", "SP", "#B71C1C", "#FFFFFF", Porte.Pequeno, 2),
        new("babitonga", "Babitonga EC", "BAB", "Joinville", "SC", "#0D47A1", "#E30613", Porte.Pequeno, 2),
        new("vale-do-itajai", "Vale do Itajaí FC", "VIT", "Blumenau", "SC", "#1B5E20", "#F5C400", Porte.Pequeno, 2),
        new("madeira-mamore", "Madeira-Mamoré FC", "MMR", "Porto Velho", "RO", "#5D4037", "#F5C400", Porte.Pequeno, 2),
        new("equinocio", "Equinócio FC", "EQU", "Macapá", "AP", "#FF8F00", "#111111", Porte.Pequeno, 2),
        new("jalapao", "Jalapão EC", "JAL", "Palmas", "TO", "#E65100", "#FFFFFF", Porte.Pequeno, 2),
        new("cidade-morena", "Cidade Morena FC", "MOR", "Campo Grande", "MS", "#3E2723", "#FFFFFF", Porte.Pequeno, 2),
    ];
}
