using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using NuGet;
using System.IO;

namespace PackageExplorerViewModel {
    internal class SaveContentCommand : ICommand {

        private IPackageFile _file;

        public SaveContentCommand(IPackageFile file) {
            _file = file;
        }

        public bool CanExecute(object parameter) {
            return true;
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter) {
            SaveFile(_file);
        }

        private void SaveFile(IPackageFile file) {
            //SaveFileDialog dialog = new SaveFileDialog() {
            //    OverwritePrompt = true,
            //    Title = "Save " + file.Name,
            //    Filter = "All files (*.*)|*.*",
            //    FileName = file.Name
            //};

            //bool? result = dialog.ShowDialog();
            //if (result ?? false) {
            //    using (FileStream fileStream = File.OpenWrite(dialog.FileName)) {
            //        CopyStream(file.GetStream(), fileStream);
            //    }
            //}
        }

        private void CopyStream(Stream source, Stream target) {
            byte[] buffer = new byte[1024 * 4];     // 4K
            int count;
            while ((count = source.Read(buffer, 0, buffer.Length)) > 0) {
                target.Write(buffer, 0, count);
            }
        }
    }
}