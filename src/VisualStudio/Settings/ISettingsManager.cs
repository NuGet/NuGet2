
namespace NuGet.VisualStudio
{
    public interface ISettingsManager
    {
        ISettingsStore GetReadOnlySettingsStore();
        IWritableSettingsStore GetWritableSettingsStore();
    }
}
