using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using NuGet.Dialog.ToolsOptionsUI;

namespace NuGet.TestUI {
    public partial class TestForm : Form {
        private MockPackageSourceProvider _packageSourceProvider = new MockPackageSourceProvider();
        private ToolsOptionsControl _optionsControl;
        public TestForm() {
            InitializeComponent();

            var list = new List<PackageSource> {
                                                   new PackageSource("NuGet official package source",
                                                                     "http://go.microsoft.com/fwlink/?LinkID=199193"),
                                                   new PackageSource("My Package Source",
                                                                     @"C:\Path\To\My\Packages")
                                               };
            _packageSourceProvider.SetPackageSources(list);
            _packageSourceProvider.ActivePackageSource = list[1];
            _optionsControl = new ToolsOptionsControl(_packageSourceProvider);
            _optionsControl.Dock = DockStyle.Fill;

            panel1.Controls.Add(_optionsControl);

            _optionsControl.InitializeOnActivated();
        }

        private void OkButton_Click(object sender, EventArgs e) {
            _optionsControl.ApplyChangedSettings();
            var sb = new StringBuilder();
            _packageSourceProvider.GetPackageSources().ToList().ForEach(ps => sb.AppendFormat("Name={0}, Source={1}\n",
                                                                                              ps.Name, ps.Source));
            sb.AppendFormat("Default: {0}", _packageSourceProvider.ActivePackageSource.Name);
            MessageBox.Show(sb.ToString());

            this.Close();
        }

    }
}
