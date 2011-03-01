using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace PackageExplorer
{
    public static class Commands
    {
        public static readonly RoutedUICommand OpenFromFeedCommand = 
            new RoutedUICommand("Open from feed...", "OpenFromFeed", typeof(Commands), 
                new InputGestureCollection() { new KeyGesture(Key.G, ModifierKeys.Control) });

    }
}
