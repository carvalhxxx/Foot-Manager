using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Jogo.App.Servicos;

namespace Jogo.App.ViewModels;

/// <summary>
/// Casca da aplicação: menu lateral + área de conteúdo onde a página atual é exibida.
/// Cada tela nova do jogo vira um ViewModel de página trocado em <see cref="PaginaAtual"/>.
/// </summary>
public partial class JanelaPrincipalViewModel(IServicoTema servicoTema) : ViewModelBase
{
    [ObservableProperty]
    public partial ViewModelBase PaginaAtual { get; set; } = new InicioViewModel();

    [RelayCommand]
    private void IrParaInicio() => PaginaAtual = new InicioViewModel();

    [RelayCommand]
    private void AlternarTema() => servicoTema.Alternar();
}
