

namespace NuGet
{
    public interface ISettingsProvider
    {
        ISettings LoadUserSettings();
    }
}