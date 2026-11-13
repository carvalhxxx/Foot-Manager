using Jogo.Dominio.Aleatoriedade;

namespace Jogo.Simulacao.Geracao;

/// <summary>
/// Gera nomes de jogadores no estilo do futebol brasileiro: "Nome Sobrenome" (73%), apelidos
/// ("Pedrinho", "Carlão", 12%) e nome + origem ("Wesley Baiano", 15%).
/// Os apelidos são genéricos de propósito: nada de apelidos que remetam a craques famosos.
/// </summary>
public sealed class GeradorDeNomes(IGeradorAleatorio aleatorio)
{
    private static readonly string[] Nomes =
    [
        "Adriano", "Alan", "Alex", "Alexandre", "Anderson", "André", "Antônio", "Arthur", "Bernardo", "Breno",
        "Bruno", "Caio", "Carlos", "César", "Cícero", "Cláudio", "Cristiano", "Daniel", "Danilo", "Davi",
        "Denílson", "Diego", "Douglas", "Edson", "Eduardo", "Elias", "Emerson", "Enzo", "Erick", "Everton",
        "Fábio", "Felipe", "Fernando", "Flávio", "Francisco", "Gabriel", "Gilberto", "Gustavo", "Heitor", "Henrique",
        "Hugo", "Igor", "Ítalo", "Ivan", "Jean", "Jefferson", "Jonas", "Jorge", "José", "João",
        "Júlio", "Kaio", "Kauã", "Leandro", "Leonardo", "Lucas", "Luciano", "Luís", "Marcelo", "Marcos",
        "Mário", "Matheus", "Maurício", "Miguel", "Murilo", "Nathan", "Nicolas", "Otávio", "Pablo", "Paulo",
        "Pedro", "Rafael", "Raul", "Reinaldo", "Renan", "Renato", "Ricardo", "Robson", "Rodrigo", "Rogério",
        "Samuel", "Sandro", "Sérgio", "Thiago", "Tiago", "Valdir", "Vinícius", "Vítor", "Wagner", "Wallace",
        "Wanderson", "Washington", "Wellington", "Wesley", "Willian", "Yago", "Yuri", "Zeca", "Juliano", "Rômulo",
    ];

    private static readonly string[] Sobrenomes =
    [
        "Almeida", "Alves", "Amaral", "Andrade", "Antunes", "Araújo", "Assis", "Azevedo", "Barbosa", "Barros",
        "Batista", "Bezerra", "Brito", "Campos", "Cardoso", "Carvalho", "Castro", "Cavalcanti", "Correia", "Costa",
        "Cruz", "Cunha", "Dantas", "Dias", "Duarte", "Farias", "Fernandes", "Ferreira", "Figueiredo", "Fonseca",
        "Freitas", "Garcia", "Gomes", "Gonçalves", "Guimarães", "Lima", "Lopes", "Macedo", "Machado", "Maciel",
        "Magalhães", "Marques", "Martins", "Medeiros", "Melo", "Mendes", "Menezes", "Miranda", "Monteiro", "Moraes",
        "Moreira", "Moura", "Nascimento", "Neves", "Nogueira", "Nunes", "Oliveira", "Pacheco", "Paiva", "Peixoto",
        "Pereira", "Pinheiro", "Pinto", "Queiroz", "Ramos", "Rezende", "Ribeiro", "Rocha", "Rodrigues", "Sales",
        "Sampaio", "Santana", "Santos", "Siqueira", "Silva", "Soares", "Souza", "Tavares", "Teixeira", "Vasconcelos",
    ];

    private static readonly string[] Apelidos =
    [
        "Pedrinho", "Paulinho", "Marcelinho", "Joãozinho", "Luizão", "Carlão", "Serginho", "Tiaguinho", "Leandrinho", "Fabinho",
        "Zé Carlos", "Zé Mário", "Zé Augusto", "Zé Henrique", "Tião", "Chico", "Duda", "Juninho", "Netinho", "Toninho",
        "Betinho", "Nando", "Guga", "Biel", "Léo", "Gui", "Diguinho", "Bruninho", "Lelê", "Tetê",
        "Fernandão", "Marcão", "Robertão", "Edinho", "Claudinho", "Tchê", "Bigode", "Magrão", "Alemão", "Baiano",
        "Vitinho", "Danilinho", "Matheuzinho", "Lucão", "Brunão", "Andrezinho", "Rodrigão", "Felipão", "Gustavinho", "Caiozinho",
    ];

    private static readonly string[] Origens =
    [
        "Baiano", "Mineiro", "Gaúcho", "Paulista", "Carioca", "Cearense", "Paraense", "Capixaba", "Goiano", "Paraíba",
        "Potiguar", "Maranhão", "Pernambucano", "Catarinense", "Paranaense", "Amazonense", "Sergipano", "Alagoano",
    ];

    /// <summary>Gera um nome que ainda não esteja em <paramref name="usados"/> (e o adiciona lá).</summary>
    public string Gerar(ISet<string> usados)
    {
        ArgumentNullException.ThrowIfNull(usados);

        for (var tentativa = 0; tentativa < 50; tentativa++)
        {
            var nome = GerarUm();
            if (usados.Add(nome))
            {
                return nome;
            }
        }

        // Praticamente impossível com as listas atuais, mas garante término.
        var reserva = $"{Sortear(Nomes)} {Sortear(Sobrenomes)} {Sortear(Sobrenomes)}";
        usados.Add(reserva);
        return reserva;
    }

    private string GerarUm()
    {
        var formato = aleatorio.Proximo(0, 100);
        return formato switch
        {
            < 73 => $"{Sortear(Nomes)} {Sortear(Sobrenomes)}",
            < 85 => Sortear(Apelidos),
            _ => $"{Sortear(Nomes)} {Sortear(Origens)}",
        };
    }

    /// <summary>Nome para estádios: "Estádio Nome Sobrenome".</summary>
    public string NomeDePessoa() => $"{Sortear(Nomes)} {Sortear(Sobrenomes)}";

    private string Sortear(string[] lista) => lista[aleatorio.Proximo(0, lista.Length)];
}
