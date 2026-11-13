using Jogo.App.Recursos;

namespace Jogo.App.ViewModels;

/// <summary>Tela inicial. Na entrega da UI mínima passa a mostrar próximo jogo, resultados e notícias.</summary>
public partial class InicioViewModel : ViewModelBase
{
    public string Titulo => Textos.InicioTitulo;

    public string Subtitulo => Textos.InicioSubtitulo;
}
