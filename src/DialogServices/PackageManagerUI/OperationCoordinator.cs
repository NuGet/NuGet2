using System.Windows.Input;

namespace NuGet.Dialog.Providers
{
    /// <summary>
    /// This class may need to be made thread-safe in the future.
    /// </summary>
    public static class OperationCoordinator
    {
        private static bool _isBusy;

        public static bool IsBusy
        {
            get
            {
                return _isBusy;
            }
            set
            {
                if (_isBusy != value)
                {
                    _isBusy = value;

                    // force every command to requery CanExecute() 
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }
    }
}