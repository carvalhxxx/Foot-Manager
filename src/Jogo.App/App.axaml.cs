using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Jogo.App.Servicos;
using Jogo.App.ViewModels;
using Jogo.App.Views;

namespace Jogo.App;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <summary>
    /// Raiz de composição: as dependências são montadas aqui, à mão.
    /// Um contêiner de injeção de dependência só entra quando o número de serviços justificar.
    /// </summary>
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new JanelaPrincipal
            {
                DataContext = new JanelaPrincipalViewModel(new ServicoTema()),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
