using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;
using PackageExplorerViewModel;

namespace PackageExplorer {
    /// <summary>
    /// Interaction logic for PackageMetadataEditor.xaml
    /// </summary>
    public partial class PackageMetadataEditor : UserControl {
        public PackageMetadataEditor() {
            InitializeComponent();
            PopulateLanguagesForLanguageBox();
        }

        private void PopulateLanguagesForLanguageBox() {
            LanguageBox.ItemsSource = CultureInfo.GetCultures(CultureTypes.SpecificCultures).Select(c => c.Name).OrderBy(p => p, StringComparer.OrdinalIgnoreCase);
        }

        public BindingGroup MetadataBindingGroup {
            get { return PackageMetadataGroup; }
        }

        private void RemoveDependencyButtonClicked(object sender, System.Windows.RoutedEventArgs e) {
            var button = (Button)sender;
            var item = (EditablePackageDependency)button.DataContext;

            var collection = DependencyList.ItemsSource as IList<EditablePackageDependency>;
            if (collection != null) {
                collection.Remove(item);
            }
        }

        private void AddDependencyButtonClicked(object sender, System.Windows.RoutedEventArgs e) {
            var collection = DependencyList.ItemsSource as IList<EditablePackageDependency>;
            if (collection != null) {
                collection.Add(new EditablePackageDependency());
            }
        }
    }
}
