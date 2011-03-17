using System.Collections.Generic;

namespace PackageExplorerViewModel.Types {

    public interface ISettingsManager {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        IList<string> GetMruFiles();
        void SetMruFiles(IEnumerable<string> files);

        string ReadApiKeyFromSettingFile();
        void WriteApiKeyToSettingFile(string apiKey);
    }
}