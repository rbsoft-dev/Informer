using System.Reflection;

namespace Informer.App.ViewModels;

public class AboutWindowViewModel
{
    public string Version { get; } =
        Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";
}