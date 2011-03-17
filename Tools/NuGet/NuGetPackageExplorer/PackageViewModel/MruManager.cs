using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Globalization;
using PackageExplorerViewModel.Types;

namespace PackageExplorerViewModel {

    [Export(typeof(IMruManager))]
    internal class MruManager : IMruManager {
        private const int MaxFile = 7;
        private readonly ObservableCollection<MruItem> _files;
        private readonly ISettingsManager _settingsManager;

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Performance", 
            "CA1811:AvoidUncalledPrivateCode",
            Justification="Called by MEF")]
        [ImportingConstructor]
        public MruManager(ISettingsManager settingsManager) {
            var savedFiles = settingsManager.GetMruFiles();

            _files = new ObservableCollection<MruItem>();
            for (int i = savedFiles.Count - 1; i >= 0; --i) {
                string s = savedFiles[i];
                MruItem item = ConvertStringToMruItem(s);
                if (item != null) {
                    AddFile(item);
                }
            }

            _settingsManager = settingsManager;
        }

        public void OnApplicationExit() {
            List<string> sc = new List<string>();
            foreach (var item in _files) {
                if (item != null) {
                    string s = ConvertMruItemToString(item);
                    sc.Add(s);
                }
            }
            _settingsManager.SetMruFiles(sc);
        }

        public ObservableCollection<MruItem> Files {
            get {
                return _files;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Globalization",
            "CA1308:NormalizeStringsToUppercase",
            Justification = "We don't want to show upper case path.")]
        public void NotifyFileAdded(string filepath, string packageName, PackageType packageType) {
            var item = new MruItem {
                Path = filepath.ToLowerInvariant(),
                PackageName = packageName,
                PackageType = packageType
            };
            AddFile(item);
        }

        private void AddFile(MruItem s) {
            if (s == null) {
                throw new ArgumentNullException("s");
            }

            // remove the padding 'null' value at the end
            _files.Remove(null);

            _files.Remove(s);
            _files.Insert(0, s);

            if (_files.Count > MaxFile) {
                _files.RemoveAt(_files.Count - 1);
            }

            // pad the 'null' value back to the end
            if (_files.Count > 0) {
                _files.Add(null);
            }
        }

        public void Clear() {
            _files.Clear();
        }

        private static string ConvertMruItemToString(MruItem item) {
            return String.Format(CultureInfo.InvariantCulture, "{0}|{1}|{2}", item.Path, item.PackageName, item.PackageType);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Performance", 
            "CA1811:AvoidUncalledPrivateCode",
            Justification="Called by MEF.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Globalization", 
            "CA1308:NormalizeStringsToUppercase",
            Justification="We don't want to show upper case path.")]
        private static MruItem ConvertStringToMruItem(string s) {
            if (String.IsNullOrEmpty(s)) {
                return null;
            }

            string[] parts = s.Split('|');
            if (parts.Length != 3) {
                return null;
            }

            for (int i = 0; i < parts.Length; i++) {
                if (String.IsNullOrEmpty(parts[i])) {
                    return null;
                }
            }

            PackageType type;
            if (!Enum.TryParse<PackageType>(parts[2], out type)) {
                return null;
            }

            return new MruItem {
                Path = parts[0].ToLowerInvariant(),
                PackageName = parts[1],
                PackageType = type
            };
        }
    }
}