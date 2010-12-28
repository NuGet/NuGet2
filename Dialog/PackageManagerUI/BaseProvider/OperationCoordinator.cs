namespace NuGet.Dialog.Providers {

    /// <summary>
    /// This class may need to be made thread-safe in the future.
    /// </summary>
    internal static class OperationCoordinator {

        public static bool IsBusy {
            get;
            set;
        }
    }
}
