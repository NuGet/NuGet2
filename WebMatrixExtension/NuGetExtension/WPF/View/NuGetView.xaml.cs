using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using Microsoft.WebMatrix.Core;
using Microsoft.WebMatrix.Utility;

namespace NuGet.WebMatrix
{
    /// <summary>
    /// Interaction logic for NuGetView.xaml
    /// </summary>
    internal partial class NuGetView : ModalDialogUserControl
    {
        private NuGetButtonBar _buttonBar;
        private IdleDelayTimer _searchAdapter;
        private SearchPageHeader _searchPageHeader;
        private string _automationName;

        public NuGetView(string heading)
        {
            InitializeComponent();

            this._itemsListView.ItemContainerGenerator.StatusChanged += this.ItemContainerGenerator_StatusChanged;

            // Listen for DataContext changed events
            this.DataContextChanged += new DependencyPropertyChangedEventHandler(NuGetView_DataContextChanged);

            // Replace the default button bar
            ConfigureButtonBar();

            // Configure the heading/search box
            ConfigureHeading(heading);

            this.Unloaded += NuGetView_Unloaded;
            this.Loaded += NuGetView_Loaded;
        }

        private void NuGetView_Loaded(object sender, RoutedEventArgs e)
        {
            if (_searchAdapter != null)
            {
                _searchAdapter.Start();
            }
        }

        private void NuGetView_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_searchAdapter != null)
            {
                _searchAdapter.Stop();
            }
        }

        private void ConfigureHeading(string heading)
        {
            _searchPageHeader = new SearchPageHeader();
            _searchPageHeader.Name = "_searchPageHeader";

            TextBlock headingTextBlock = WPFHelper.CreateTextBlock(heading);
            _searchPageHeader.SetHeaderTextContent(headingTextBlock);

            TextRange text = new TextRange(headingTextBlock.ContentStart, headingTextBlock.ContentEnd);
            _automationName = text.Text;

            this.SetHeading(_searchPageHeader);

            ConfigureSearchStringBinding();
        }

        private void ConfigureSearchStringBinding()
        {
            // Create a Binding to the SearchString property of the header
            Binding searchStringBinding = new Binding("SearchString");
            searchStringBinding.Source = _searchPageHeader;

            // perform the search on idle
            _searchAdapter = new IdleDelayTimer(searchStringBinding);
        }

        private void ConfigureButtonBar()
        {
            _buttonBar = new NuGetButtonBar();
            this.ButtonBar = _buttonBar;
        }

        protected override string AutomationName
        {
            get
            {
                return _automationName;
            }
        }

        private void NuGetView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // Set the DataContext of the ButtonBar and the Header
            this.ButtonBar.DataContext = new ButtonBarViewModel(
                (NuGetViewModel)this.DataContext,
                new RelayCommand((ignore) => ((NuGetButtonBar)this.ButtonBar).CancelCommand()));

            this.Heading.DataContext = this.DataContext;

            if (this.DataContext != null)
            {
                // this binds the search string changes to the view model, and bypasses the 'search'
                // part of multifilter list view, which allows us to perform the search asynchronously
                Binding searchOutputBinding = new Binding("SearchString");
                searchOutputBinding.Source = this.DataContext;
                searchOutputBinding.Mode = BindingMode.OneWayToSource;
                _searchAdapter.SetBinding(IdleDelayTimer.OutputProperty, searchOutputBinding);
            }
        }

        private void ItemContainerGenerator_StatusChanged(object sender, EventArgs e)
        {
            // whenever the listview has a finished updating, select the first items if we don't
            // have a selection
            var listView = this._itemsListView;
            if (listView.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated &&
                listView.SelectedItem == null &&
                listView.Items.Count > 0)
            {
                listView.SelectedItem = listView.Items[0];
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.E
                && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (this._searchPageHeader.IsSearchTextBoxVisible)
                {
                    this._searchPageHeader.SetFocusOnSearchBox();
                    e.Handled = true;
                    return;
                }
            }

            base.OnKeyDown(e);
        }
    }
}
