using MediaOrcestrator.Core.Services;

namespace MediaOrcestrator.Domain;

public class PluginManager
{
    public Dictionary<string, IMediaSource> MediaSources { get; set; }

    public void Init()
    {
        var path1 = "..\\..\\..\\..\\ModuleBuilds";
        var scanner = new InterfaceScanner();
        var myInterfaceType = typeof(IMediaSource); // Пример интерфейса
        var implementations = scanner.FindImplementations(path1, myInterfaceType);
        MediaSources = new();
        foreach (var x in implementations)
        {
            var id = x.Assembly.FullName.Split(",")[0];
            var instance = (IMediaSource)Activator.CreateInstance(x.Type);
            MediaSources.Add(id, instance);
        }
    }
}
