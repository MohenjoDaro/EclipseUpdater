using Avalonia;
using Avalonia.Markup.Xaml;

namespace EclipseUpdater
{
    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
