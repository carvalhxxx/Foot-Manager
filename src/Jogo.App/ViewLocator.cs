using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Jogo.App.Recursos;
using Jogo.App.ViewModels;

namespace Jogo.App;

/// <summary>
/// Dado um ViewModel, devolve a View correspondente pela convenção de nomes
/// (<c>ViewModels.XxxViewModel</c> → <c>Views.XxxView</c>).
/// </summary>
[RequiresUnreferencedCode(
    "O ViewLocator padrão usa reflexão, que pode ser removida por trimming.",
    Url = "https://docs.avaloniaui.net/docs/concepts/view-locator")]
public class ViewLocator : IDataTemplate
{
    public Control? Build(object? param)
    {
        if (param is null)
        {
            return null;
        }

        var nome = param.GetType().FullName!.Replace("ViewModel", "View", StringComparison.Ordinal);
        var tipo = Type.GetType(nome);

        if (tipo is not null)
        {
            return (Control)Activator.CreateInstance(tipo)!;
        }

        return new TextBlock { Text = string.Format(CultureInfo.CurrentCulture, Textos.TelaNaoEncontrada, nome) };
    }

    public bool Match(object? data) => data is ViewModelBase;
}
