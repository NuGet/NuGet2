using System.Windows.Input;

namespace PackageExplorer {
    public static class Commands {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly RoutedUICommand PublishToFeedCommand =
            new RoutedUICommand("Publish...", "PublishToFeed", typeof(Commands),
                new InputGestureCollection() { new KeyGesture(Key.P, ModifierKeys.Control) });

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly RoutedUICommand AddRootFolderCommand =
            new RoutedUICommand("Add Folder...", "AddRootFolder", typeof(Commands));
    }
}