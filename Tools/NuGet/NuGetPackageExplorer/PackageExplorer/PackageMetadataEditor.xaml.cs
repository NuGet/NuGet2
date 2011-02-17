using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using NuGet;
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
            var item = (PackageDependency)button.DataContext;

            var collection = DependencyList.ItemsSource as IList<PackageDependency>;
            if (collection != null) {
                collection.Remove(item);
            }
        }

        private void AddDependencyButtonClicked(object sender, System.Windows.RoutedEventArgs e) {

            if (!Validate(NewDependencyId, ValidateId) || 
                !Validate(NewDependencyVersion, ValidateVersion)) {
                return;
            }

            IVersionSpec versionSpec;
            VersionUtility.TryParseVersionSpec(NewDependencyVersion.Text, out versionSpec);

            var collection = DependencyList.ItemsSource as IList<PackageDependency>;
            if (collection != null) {
                collection.Add(new PackageDependency(NewDependencyId.Text, versionSpec));
                NewDependencyId.Text = NewDependencyVersion.Text = String.Empty;
            }
        }

        private bool Validate(TextBox input, Func<string, string> validator) {
            string value = input.Text;
            string error = validator(value);
            if (error == null) {
                input.ClearValue(Control.BorderBrushProperty);
                input.ClearValue(ToolTipService.ToolTipProperty);
                return true;
            }
            else {
                input.BorderBrush = Brushes.Red;
                ToolTipService.SetToolTip(input, error);
                return false;
            }
        }

        private string ValidateVersion(string version) {
            string errorMessage = null;

            if (!String.IsNullOrEmpty(version)) {
                IVersionSpec versionSpec;
                if (!VersionUtility.TryParseVersionSpec(version, out versionSpec)) {
                    errorMessage = String.Format("Value '{0}' is an invalid version spec.", version);
                }
            }

            return errorMessage;
        }

        private string ValidateId(string id) {
            string errorMessage = null;

            if (String.IsNullOrEmpty(id)) {
                errorMessage = "Id is required.";
            }

            if (!PackageIdValidator.IsValidPackageId(id)) {
                errorMessage = "Value '" + id + "' is an invalid id.";
            }

            return errorMessage;
        }

        private void SelectDependencyButtonClicked(object sender, System.Windows.RoutedEventArgs e) {
            var dialog = new PackageChooserDialog() { Owner = Window.GetWindow(this) };
            var result = dialog.ShowDialog();
            if (result ?? false) {
                var selectedPackage = dialog.SelectedPackage;
                if (selectedPackage != null) {
                    NewDependencyId.Text = selectedPackage.Id;
                    NewDependencyVersion.Text = selectedPackage.Version.ToString();
                }
            }
        }

        private void OkButtonClicked(object sender, RoutedEventArgs e) {
            
        }

        private void CancelButtonClicked(object sender, RoutedEventArgs e) {

        }
    }
}