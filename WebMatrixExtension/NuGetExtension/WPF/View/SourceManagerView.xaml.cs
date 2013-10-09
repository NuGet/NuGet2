using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using Microsoft.WebMatrix.Core;
using Microsoft.WebMatrix.Utility;

namespace NuGet.WebMatrix
{
    /// <summary>
    /// Interaction logic for SourceManagerView.xaml
    /// </summary>
    internal partial class SourceManagerView : ModalDialogUserControl
    {
        private string _automationName;
        private DefaultModalButtonBar _buttonBar;

        public SourceManagerView()
            : this(NuGet.WebMatrix.Resources.String_SourceManagerb)
        {
        }

        public SourceManagerView(string heading)
        {
            InitializeComponent();
            ConfigureHeading(heading);

            _buttonBar = this.ButtonBar as DefaultModalButtonBar;
            Debug.Assert(_buttonBar != null, "The this.ButtonBar must be an DefaultModalButtonBar");
            _buttonBar.OkButton.SetBinding(Button.CommandProperty, new Binding("SaveCommand"));
            _buttonBar.CancelButton.SetBinding(Button.CommandProperty, new Binding("CancelCommand"));

            this.DataContextChanged += new DependencyPropertyChangedEventHandler(SourceManagerView_DataContextChanged);
        }

        private void SourceManagerView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_buttonBar != null)
            {
                _buttonBar.DataContext = this.DataContext;
            }
        }

        private void ConfigureHeading(string heading)
        {
            TextBlock headingTextBlock = WPFHelper.CreateTextBlock(heading);

            TextRange text = new TextRange(headingTextBlock.ContentStart, headingTextBlock.ContentEnd);
            _automationName = text.Text;

            this.SetHeading(headingTextBlock);
        }

        protected override string AutomationName
        {
            get
            {
                return _automationName;
            }
        }

        private void TextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter
                && _addButton.Command.CanExecute(null))
            {
                _addButton.Command.Execute(null);
                e.Handled = true;
            }
        }
    }
}
