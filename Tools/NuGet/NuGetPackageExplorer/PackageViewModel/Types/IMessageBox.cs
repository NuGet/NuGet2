using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PackageExplorerViewModel.Types {

    public enum MessageLevel {
        Information,
        Warning,
        Error
    }

    public interface IMessageBox {
        bool Confirm(string message);
        bool Confirm(string message, bool isWarning);
        bool? ConfirmWithCancel(string message);
        void Show(string message, MessageLevel messageLevel);
    }
}