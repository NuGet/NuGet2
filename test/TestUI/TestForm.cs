using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using NuGet.Options;
using NuGet.VisualStudio;

namespace NuGet.TestUI
{
    public partial class TestForm : Form
    {
        private MockPackageSourceProvider _packageSourceProvider = new MockPackageSourceProvider();
        private PackageSourcesOptionsControl _optionsControl;
        private bool _isClosing;
        public TestForm()
        {
            InitializeComponent();

            var list = new List<PackageSource> {
                                                   AggregatePackageSource.Instance,
                                                   new PackageSource("http://go.microsoft.com/fwlink/?LinkID=199193",
                                                                     "NuGet official package source"),
                                                   new PackageSource(@"C:\Path\To\My\Packages",
                                                                     "My Package Source"),
                                                   new PackageSource(@"C:\Test", 
                                                                     "Test"),
                                                   new PackageSource(@"C:\Test2", 
                                                                     "Test2")
                                               };
            _packageSourceProvider.SavePackageSources(list);
            _packageSourceProvider.ActivePackageSource = list[1];
            _optionsControl = new PackageSourcesOptionsControl(_packageSourceProvider, null);
            _optionsControl.Dock = DockStyle.Fill;

            panel1.Controls.Add(_optionsControl);
            this.AcceptButton = OkButton;
            this.CancelButton = theCancelButton;
            _optionsControl.InitializeOnActivated();
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            bool wasApplied = _optionsControl.ApplyChangedSettings();
            if (!wasApplied)
            {
                _isClosing = false;
                return; // don't close
            }
            var sb = new StringBuilder();
            _packageSourceProvider.LoadPackageSources().ToList().ForEach(ps => sb.AppendFormat("Name={0}, Source={1}\n",
                                                                                              ps.Name, ps.Source));
            sb.AppendFormat("Default: {0}", _packageSourceProvider.ActivePackageSource.Name);
            MessageBox.Show(sb.ToString());

            _isClosing = true;
            this.Close();
        }

        private void TestForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = !_isClosing;
        }

        private void theCancelButton_Click(object sender, EventArgs e)
        {
            _isClosing = true;
            this.Close();
        }
    }
}
