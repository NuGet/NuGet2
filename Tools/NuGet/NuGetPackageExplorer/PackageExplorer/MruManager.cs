using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Collections.Specialized;
using System.Windows;

namespace PackageExplorer
{
    internal class MruManager
    {
        private const int MaxFile = 7;
        private ObservableCollection<MruItem> _files;

        public MruManager(Window parent)
        {
            var savedFiles = Properties.Settings.Default.MruFiles ?? new StringCollection();
            
            _files = new ObservableCollection<MruItem>();
            for (int i = savedFiles.Count-1; i >= 0; --i) 
            {
                string s = savedFiles[i];
                MruItem item = ConvertStringToMruItem(s);
                if (item != null)
                {
                    AddFile(item);
                }
            }

            parent.Closed += new EventHandler(OnParentWindowClosed);
        }

        private void OnParentWindowClosed(object sender, EventArgs e)
        {
            StringCollection sc = new StringCollection();
            foreach (var item in _files)
            {
                if (item != null)
                {
                    string s = ConvertMruItemToString(item);
                    sc.Add(s);
                }
            }
            Properties.Settings.Default.MruFiles = sc;
        }

        public ObservableCollection<MruItem> Files
        {
            get
            {
                return _files;
            }
        }

        public void NotifyFileAdded(string filepath, string packageName, PackageType packageType)
        {
            var item = new MruItem
            {
                Path = filepath.ToLower(CultureInfo.InvariantCulture),
                PackageName = packageName,
                PackageType = packageType
            };
            AddFile(item);
        }

        private void AddFile(MruItem s)
        {
            if (s == null) {
                throw new ArgumentNullException("s");
            }

            // remove the padding 'null' value at the end
            _files.Remove(null);

            _files.Remove(s);
            _files.Insert(0, s);

            if (_files.Count > MaxFile)
            {
                _files.RemoveAt(_files.Count - 1);
            }

            // pad the 'null' value back to the end
            if (_files.Count > 0)
            {
                _files.Add(null);
            }
        }

        public void Clear()
        {
            _files.Clear();
        }

        private string ConvertMruItemToString(MruItem item)
        {
            return String.Format(CultureInfo.InvariantCulture, "{0}|{1}|{2}", item.Path, item.PackageName, item.PackageType);
        }

        private MruItem ConvertStringToMruItem(string s)
        {
            if (String.IsNullOrEmpty(s))
            {
                return null;
            }

            string[] parts = s.Split('|');
            if (parts.Length != 3)
            {
                return null;
            }

            for (int i = 0; i < parts.Length; i++)
            {
                if (String.IsNullOrEmpty(parts[i]))
                {
                    return null;
                }
            }

            PackageType type;
            if (!Enum.TryParse<PackageType>(parts[2], out type))
            {
                return null;
            }

            return new MruItem
            {
                Path = parts[0].ToLower(CultureInfo.InvariantCulture),
                PackageName = parts[1],
                PackageType = type
            };
        }
    }
}