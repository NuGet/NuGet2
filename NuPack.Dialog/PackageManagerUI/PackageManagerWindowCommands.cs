using System.Windows.Input;

namespace NuPack.Dialog.PackageManagerUI {

    public static class PackageManagerWindowCommands {
        public readonly static RoutedCommand UninstallPackage = new RoutedCommand();
        public readonly static RoutedCommand UpdatePackage = new RoutedCommand();
        public readonly static RoutedCommand InstallPackage = new RoutedCommand();
        public readonly static RoutedCommand ShowOptionsPage = new RoutedCommand();
        public readonly static RoutedCommand FocusOnSearchBox = new RoutedCommand();
        public readonly static RoutedCommand OpenLicenseLink = new RoutedCommand();
    }
}