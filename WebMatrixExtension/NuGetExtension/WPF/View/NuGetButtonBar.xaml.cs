using System;
using System.Windows.Controls;
using Microsoft.WebMatrix.Core;

namespace NuGet.WebMatrix
{
    /// <summary>
    /// Interaction logic for NuGetButtonBar.xaml
    /// </summary>
    public partial class NuGetButtonBar : UserControl, IModalDialogButtonBar
    {
        private Action _okAction;
        private Action _cancelAction;

        public NuGetButtonBar()
        {
            InitializeComponent();

            _okAction = new Action(() =>
            {
                if (Ok != null)
                {
                    Ok(this, EventArgs.Empty);
                }
            });

            _cancelAction = new Action(() =>
            {
                if (Cancel != null)
                {
                    Cancel(this, EventArgs.Empty);
                }
            });
        }

        public event EventHandler Cancel;

        public event EventHandler Ok;

        public Action OkCommand
        {
            get
            {
                return _okAction;
            }
        }

        public Action CancelCommand
        {
            get
            {
                return _cancelAction;
            }
        }

        /// <summary>
        /// This handler force the combo box selection to go to a 'real' feed source. This combo-box
        /// will always have N+1 items, where N is the number of feeds configured. The last item is the
        /// 'configure feeds' entry -- if it gets selected, we basically veto the selection.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.RemovedItems.Count > 0)
            {
                var oldItem = e.RemovedItems[0];

                if (_comboBox.SelectedIndex == (_comboBox.Items.Count - 1))
                {
                    var originalIndex = _comboBox.Items.IndexOf(oldItem);
                    if (originalIndex > -1)
                    {
                        _comboBox.SelectedIndex = originalIndex;
                    }
                }
            }
        }
    }
}
