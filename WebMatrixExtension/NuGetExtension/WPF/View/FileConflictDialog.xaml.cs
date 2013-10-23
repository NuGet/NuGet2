using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.WebMatrix.Core;
using NuGet;

namespace NuGet.WebMatrix
{
    /// <summary>
    /// Interaction logic for FileConflictDialog.xaml
    /// </summary>
    public partial class FileConflictDialog : ModalDialogUserControl
    {
        public FileConflictDialog()
        {
            InitializeComponent();
            ButtonBar = new FrameworkElement();
            SetHeading(NuGet.WebMatrix.Resources.FileConflictResolution);
        }

        public new string Message
        {
            get
            {
                return MessageText.Text;
            }
            set
            {
                MessageText.Text = value;
            }
        }

        public FileConflictResolution UserSelection
        {
            get;
            private set;
        }

        private void OnButtonClick(object sender, System.Windows.RoutedEventArgs e)
        {
            var button = (Button)sender;
            string tagValue = (string)button.Tag;

            UserSelection = (FileConflictResolution)Enum.Parse(typeof(FileConflictResolution), tagValue);
            CloseDialog(true);
        }
    }
}
