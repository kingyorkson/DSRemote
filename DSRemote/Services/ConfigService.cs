using DSRemote.Models;

namespace DSRemote.Services;

public class ConfigService
{
    public AppConfig Current { get; private set; }

    public ConfigService()
    {
        Current = AppConfig.Load();
    }

    public void Save(AppConfig? config = null)
    {
        if (config != null)
            Current = config;
        Current.Save();
    }
}
