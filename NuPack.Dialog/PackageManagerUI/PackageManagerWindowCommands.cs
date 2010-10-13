using System.Windows.Input;

namespace NuPack.Dialog.PackageManagerUI {
    public class PackageManagerWindowCommands {
        //Custom commands for AddPackage UI
        public static RoutedCommand UninstallPackage = new RoutedCommand();
        public static RoutedCommand UpdatePackage = new RoutedCommand();
        public static RoutedCommand InstallPackage = new RoutedCommand();
        public static RoutedCommand SelectOnlineProvider = new RoutedCommand();
        public static RoutedCommand ShowOptionsPage = new RoutedCommand();
        public static RoutedCommand FocusOnSearchBox = new RoutedCommand();
        public static RoutedCommand OpenLicenseLink = new RoutedCommand();
    }
}