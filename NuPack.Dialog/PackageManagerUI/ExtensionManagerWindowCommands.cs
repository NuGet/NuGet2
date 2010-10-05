using System.Windows.Input;

namespace NuPack.Dialog.PackageManagerUI {
    public class ExtensionManagerWindowCommands {
        //Custom commands for AddPackage UI
        public static RoutedCommand UninstallPackage = new RoutedCommand();
        public static RoutedCommand ToggleExtensionEnabledState = new RoutedCommand();
        public static RoutedCommand UpdateExtension = new RoutedCommand();
        public static RoutedCommand RestartVisualStudio = new RoutedCommand();
        public static RoutedCommand DownloadExtension = new RoutedCommand();
        public static RoutedCommand SelectOnlineProvider = new RoutedCommand();
        public static RoutedCommand ShowOptionsPage = new RoutedCommand();
        public static RoutedCommand FocusOnSearchBox = new RoutedCommand();
    }
}
