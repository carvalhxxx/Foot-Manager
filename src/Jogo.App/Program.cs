using Avalonia;

namespace Jogo.App;

internal sealed class Program
{
    // Não use APIs do Avalonia nem código que dependa de SynchronizationContext antes de Main
    // chamar o AppBuilder: nada está inicializado ainda.
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    // Configuração do Avalonia; também usada pelo designer visual. Não remover.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}
