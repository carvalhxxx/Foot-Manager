using Avalonia;
using Avalonia.Styling;

namespace Jogo.App.Servicos;

/// <summary>Troca entre tema claro e escuro. Abstraído para manter os ViewModels testáveis.</summary>
public interface IServicoTema
{
    void Alternar();
}

public sealed class ServicoTema : IServicoTema
{
    public void Alternar()
    {
        var app = Application.Current ?? throw new InvalidOperationException("Aplicação não inicializada.");
        app.RequestedThemeVariant = app.ActualThemeVariant == ThemeVariant.Dark
            ? ThemeVariant.Light
            : ThemeVariant.Dark;
    }
}
