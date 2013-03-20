
namespace NuGet.VisualStudio.Types
{
    public interface ISettingsManager
    {
        ISettingsStore GetReadOnlySettingsStore();
        IWritableSettingsStore GetWritableSettingsStore();
    }
}
