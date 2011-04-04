using System.Windows.Input;

namespace PackageExplorer {

    public static class Commands {
        public static readonly RoutedUICommand PublishToFeedCommand =
            new RoutedUICommand("Publish...", "PublishToFeed", typeof(Commands),
                new InputGestureCollection() { new KeyGesture(Key.P, ModifierKeys.Control) });

        public static readonly RoutedUICommand AddRootFolderCommand =
            new RoutedUICommand("Add Folder...", "AddRootFolder", typeof(Commands));
    }

}