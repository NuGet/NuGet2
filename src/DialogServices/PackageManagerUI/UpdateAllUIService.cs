using System;
using System.Windows;

namespace NuGet.Dialog.PackageManagerUI
{
    public class UpdateAllUIService : IUpdateAllUIService
    {
        private UpdateAllUI _currentElement;

        public void Show()
        {
            if (_currentElement == null)
            {
                throw new InvalidOperationException();
            }

            _currentElement.Visibility = Visibility.Visible;
        }

        public void Hide()
        {
            if (_currentElement != null)
            {
                _currentElement.Visibility = Visibility.Collapsed;
            }
        }

        public UpdateAllUI CreateUIElement()
        {
            return _currentElement = new UpdateAllUI
                {
                    Visibility = Visibility.Collapsed
                };
        }

        public void DisposeElement()
        {
            _currentElement = null;
        }
    }
}