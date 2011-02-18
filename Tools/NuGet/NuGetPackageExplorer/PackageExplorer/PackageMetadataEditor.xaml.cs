using System;
using System.Collections.ObjectModel;
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

        private ObservableCollection<PackageDependency> _packageDependencies;

        public PackageMetadataEditor() {
            InitializeComponent();
            PopulateLanguagesForLanguageBox();
        }

        private void PackageMetadataEditor_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e) {
            if (this.Visibility == System.Windows.Visibility.Visible) {
                ClearDependencyTextBox();
                PrepareBindingForDependencyList();
            }
        }

        private void PrepareBindingForDependencyList() {
            var viewModel = (PackageViewModel)DataContext;
            _packageDependencies = new ObservableCollection<PackageDependency>(viewModel.PackageMetadata.Dependencies);
            DependencyList.ItemsSource = _packageDependencies;
        }

        private void ClearDependencyTextBox() {
            NewDependencyId.Text = NewDependencyVersion.Text = String.Empty;
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

            _packageDependencies.Remove(item);
        }

        private void AddDependencyButtonClicked(object sender, System.Windows.RoutedEventArgs e) {

            if (!Validate(NewDependencyId, ValidateId) ||
                !Validate(NewDependencyVersion, ValidateVersion)) {
                return;
            }

            IVersionSpec versionSpec;
            VersionUtility.TryParseVersionSpec(NewDependencyVersion.Text, out versionSpec);

            _packageDependencies.Add(new PackageDependency(NewDependencyId.Text, versionSpec));

            // after dependency is added, clear the textbox
            ClearDependencyTextBox();
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
            bool commited = PackageMetadataGroup.CommitEdit();
            if (commited) {
                var viewModel = (PackageViewModel)DataContext;
                _packageDependencies.CopyTo(viewModel.PackageMetadata.Dependencies);
            }
        }

        private void CancelButtonClicked(object sender, RoutedEventArgs e) {
            var viewModel = (PackageViewModel)DataContext;
            _packageDependencies = new ObservableCollection<PackageDependency>(viewModel.PackageMetadata.Dependencies);
            DependencyList.ItemsSource = _packageDependencies;
        }
    }
}