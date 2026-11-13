using CommunityToolkit.Mvvm.ComponentModel;

namespace Jogo.App.ViewModels;

/// <summary>
/// Base de todos os ViewModels. O <see cref="ViewLocator"/> encontra a View pelo nome:
/// <c>Jogo.App.ViewModels.XxxViewModel</c> → <c>Jogo.App.Views.XxxView</c>.
/// </summary>
public abstract class ViewModelBase : ObservableObject
{
}
