using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using NuGet.VisualStudio;

namespace NuGet.Options {
    /// <summary>
    /// Represents the Tools - Options - Package Manager dialog
    /// </summary>
    /// <remarks>
    /// The code in this class assumes that while the dialog is open, noone is modifying the VSPackageSourceProvider directly.
    /// Otherwise, we have a problem with synchronization with the package source provider.
    /// </remarks>
    public partial class PackageSourcesOptionsControl : UserControl {
        private readonly IVsPackageSourceProvider _packageSourceProvider;
        private PackageSource _aggregateSource;
        private PackageSource _activeSource;
        private BindingSource _allPackageSources;
        private readonly IServiceProvider _serviceProvider;
        private bool _initialized;
        private Size _checkBoxSize;

        public PackageSourcesOptionsControl(IServiceProvider serviceProvider)
            : this(ServiceLocator.GetInstance<IVsPackageSourceProvider>(), serviceProvider) {
        }

        public PackageSourcesOptionsControl(IVsPackageSourceProvider packageSourceProvider, IServiceProvider serviceProvider) {
            InitializeComponent();

            _serviceProvider = serviceProvider;
            _packageSourceProvider = packageSourceProvider;
            SetupEventHandlers();
        }

        private void SetupEventHandlers() {
            NewPackageName.TextChanged += (o, e) => UpdateUI();
            NewPackageSource.TextChanged += (o, e) => UpdateUI();
            MoveUpButton.Click += (o, e) => MoveSelectedItem(-1);
            MoveDownButton.Click += (o, e) => MoveSelectedItem(1);
            PackageSourcesListBox.SelectedIndexChanged += (o, e) => UpdateUI();
            NewPackageName.Focus();
            UpdateUI();
        }

        private void UpdateUI() {
            MoveUpButton.Enabled = PackageSourcesListBox.SelectedItem != null &&
                                    PackageSourcesListBox.SelectedIndex > 0;
            MoveDownButton.Enabled = PackageSourcesListBox.SelectedItem != null &&
                                    PackageSourcesListBox.SelectedIndex < PackageSourcesListBox.Items.Count - 1;
            removeButton.Enabled = PackageSourcesListBox.SelectedItem != null;
        }

        private void MoveSelectedItem(int offset) {
            if (PackageSourcesListBox.SelectedItem == null) {
                return;
            }

            int oldIndex = PackageSourcesListBox.SelectedIndex;
            int newIndex = oldIndex + offset;

            if (newIndex < 0 || newIndex > PackageSourcesListBox.Items.Count - 1) {
                return;
            }
            var item = PackageSourcesListBox.SelectedItem;
            _allPackageSources.Remove(item);
            _allPackageSources.Insert(newIndex, item);

            PackageSourcesListBox.SelectedIndex = newIndex;
            UpdateUI();
        }

        internal void InitializeOnActivated() {
            if (_initialized) {
                return;
            }

            _initialized = true;
            // get packages sources
            IList<PackageSource> packageSources = _packageSourceProvider.LoadPackageSources().ToList();
            _aggregateSource = AggregatePackageSource.Instance;
            _activeSource = _packageSourceProvider.ActivePackageSource;

            // bind to the package sources, excluding Aggregate
            _allPackageSources = new BindingSource(packageSources.Select(ps => ps.Clone()).ToList(), null);
            PackageSourcesListBox.DataSource = _allPackageSources;
        }

        /// <summary>
        /// Persist the package sources, which was add/removed via the Options page, to the VS Settings store.
        /// This gets called when users click OK button.
        /// </summary>
        internal bool ApplyChangedSettings() {
            // if user presses Enter after filling in Name/Source but doesn't click Add
            // the options will be closed without adding the source, try adding before closing
            // Only apply if nothing was added
            TryAddSourceResults result = TryAddSource();
            if (result != TryAddSourceResults.NothingAdded) {
                return false;
            }

            // get package sources as ordered list
            var packageSources = PackageSourcesListBox.Items.Cast<PackageSource>().ToList();
            _packageSourceProvider.SavePackageSources(packageSources);
            // restore current active source if it still exists, or reset to aggregate source
            if (packageSources.Contains(_activeSource)) {
                _packageSourceProvider.ActivePackageSource = _activeSource;
            }
            else {
                _packageSourceProvider.ActivePackageSource = _aggregateSource;
            }
            return true;
        }

        /// <summary>
        /// This gets called when users close the Options dialog
        /// </summary>
        internal void ClearSettings() {
            // clear this flag so that we will set up the bindings again when the option page is activated next time
            _initialized = false;

            _allPackageSources = null;
            ClearNameSource();
            UpdateUI();
        }

        private void OnRemoveButtonClick(object sender, EventArgs e) {
            if (PackageSourcesListBox.SelectedItem == null) {
                return;
            }
            _allPackageSources.Remove(PackageSourcesListBox.SelectedItem);
            UpdateUI();
        }

        private void OnAddButtonClick(object sender, EventArgs e) {
            TryAddSourceResults result = TryAddSource();
            if (result == TryAddSourceResults.NothingAdded) {
                MessageHelper.ShowWarningMessage(Resources.ShowWarning_NameAndSourceRequired, Resources.ShowWarning_Title);
                SelectAndFocus(NewPackageName);
            }
            UpdateUI();
        }

        private TryAddSourceResults TryAddSource() {
            var name = NewPackageName.Text.Trim();
            var source = NewPackageSource.Text.Trim();
            if (String.IsNullOrWhiteSpace(name) && String.IsNullOrWhiteSpace(source)) {
                return TryAddSourceResults.NothingAdded;
            }

            // validate name
            if (String.IsNullOrWhiteSpace(name)) {
                MessageHelper.ShowWarningMessage(Resources.ShowWarning_NameRequired, Resources.ShowWarning_Title);
                SelectAndFocus(NewPackageName);
                return TryAddSourceResults.InvalidSource;
            }

            // validate source
            if (String.IsNullOrWhiteSpace(source)) {
                MessageHelper.ShowWarningMessage(Resources.ShowWarning_SourceRequried, Resources.ShowWarning_Title);
                SelectAndFocus(NewPackageSource);
                return TryAddSourceResults.InvalidSource;
            }

            if (!(PathValidator.IsValidLocalPath(source) || PathValidator.IsValidUncPath(source) || PathValidator.IsValidUrl(source))) {
                MessageHelper.ShowWarningMessage(Resources.ShowWarning_InvalidSource, Resources.ShowWarning_Title);
                SelectAndFocus(NewPackageSource);
                return TryAddSourceResults.InvalidSource;
            }

            var sourcesList = (IEnumerable<PackageSource>)_allPackageSources.List;

            // check to see if name has already been added
            // also make sure it's not the same as the aggregate source ('All')
            bool hasName = sourcesList.Any(ps => String.Equals(name, ps.Name, StringComparison.OrdinalIgnoreCase)
                || String.Equals(name, _aggregateSource.Name, StringComparison.OrdinalIgnoreCase));
            if (hasName) {
                MessageHelper.ShowWarningMessage(Resources.ShowWarning_UniqueName, Resources.ShowWarning_Title);
                SelectAndFocus(NewPackageName);
                return TryAddSourceResults.SourceAlreadyAdded;
            }

            // check to see if source has already been added
            bool hasSource = sourcesList.Any(ps => String.Equals(PathUtility.GetCanonicalPath(source), PathUtility.GetCanonicalPath(ps.Source), StringComparison.OrdinalIgnoreCase));
            if (hasSource) {
                MessageHelper.ShowWarningMessage(Resources.ShowWarning_UniqueSource, Resources.ShowWarning_Title);
                SelectAndFocus(NewPackageSource);
                return TryAddSourceResults.SourceAlreadyAdded;
            }

            var newPackageSource = new PackageSource(source, name);
            _allPackageSources.Add(newPackageSource);
            // set selection to newly added item
            PackageSourcesListBox.SelectedIndex = PackageSourcesListBox.Items.Count - 1;

            // now clear the text boxes
            ClearNameSource();

            return TryAddSourceResults.SourceAdded;
        }

        private static void SelectAndFocus(TextBox textBox) {
            textBox.Focus();
            textBox.SelectAll();
        }

        private void ClearNameSource() {
            NewPackageName.Text = String.Empty;
            NewPackageSource.Text = String.Empty;
            NewPackageName.Focus();
        }

        private void PackageSourcesContextMenu_ItemClicked(object sender, ToolStripItemClickedEventArgs e) {
            if (e.ClickedItem == CopyPackageSourceStripMenuItem && PackageSourcesListBox.SelectedItem != null) {
                CopySelectedItem((PackageSource)PackageSourcesListBox.SelectedItem);
            }
        }

        private void PackageSourcesListBox_KeyUp(object sender, KeyEventArgs e) {
            if (e.KeyCode == Keys.C && e.Control) {
                CopySelectedItem((PackageSource)PackageSourcesListBox.SelectedItem);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Space) {
                TogglePackageSourceEnabled(PackageSourcesListBox.SelectedIndex);
                e.Handled = true;
            }
        }

        private void TogglePackageSourceEnabled(int itemIndex) {
            if (itemIndex < 0 || itemIndex >= PackageSourcesListBox.Items.Count) {
                return;
            }

            var item = (PackageSource)PackageSourcesListBox.Items[itemIndex];
            item.IsEnabled = !item.IsEnabled;
            
            PackageSourcesListBox.Invalidate(GetCheckBoxRectangleForListBoxItem(itemIndex));
        }

        private Rectangle GetCheckBoxRectangleForListBoxItem(int itemIndex) {
            const int edgeMargin = 8;

            Rectangle itemRectangle = PackageSourcesListBox.GetItemRectangle(itemIndex);

            // this is the bound of the checkbox
            var checkBoxRectangle = new Rectangle(
                itemRectangle.Left + edgeMargin + 2,
                itemRectangle.Top + edgeMargin,
                _checkBoxSize.Width,
                _checkBoxSize.Height);

            return checkBoxRectangle;
        }

        private static void CopySelectedItem(PackageSource selectedPackageSource) {
            Clipboard.Clear();
            Clipboard.SetText(selectedPackageSource.Source);
        }

        private void PackageSourcesListBox_MouseUp(object sender, MouseEventArgs e) {
            if (e.Button == MouseButtons.Right) {
                PackageSourcesListBox.SelectedIndex = PackageSourcesListBox.IndexFromPoint(e.Location);
            }
            else if (e.Button == MouseButtons.Left) {
                int itemIndex = PackageSourcesListBox.IndexFromPoint(e.Location);
                if (itemIndex >= 0 && itemIndex < PackageSourcesListBox.Items.Count) {
                    Rectangle checkBoxRectangle = GetCheckBoxRectangleForListBoxItem(itemIndex);
                    // if the mouse click position is inside the checkbox, toggle the IsEnabled property
                    if (checkBoxRectangle.Contains(e.Location)) {
                        TogglePackageSourceEnabled(itemIndex);
                    }
                }
            }
        }

        private readonly Color SelectionFocusGradientLightColor = Color.FromArgb(0xF9, 0xFC, 0xFF);
        private readonly Color SelectionFocusGradientDarkColor = Color.FromArgb(0xC9, 0xE0, 0xFC);
        private readonly Color SelectionFocusBorderColor = Color.FromArgb(0x89, 0xB0, 0xDF);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "System.Drawing.Graphics.MeasureString(System.String,System.Drawing.Font,System.Int32,System.Drawing.StringFormat)")]
        private void PackageSourcesListBox_DrawItem(object sender, DrawItemEventArgs e) {
            Graphics graphics = e.Graphics;

            // Draw the background of the ListBox control for each item.
            if (e.BackColor.Name == KnownColor.Highlight.ToString()) {
                using (var gradientBrush = new LinearGradientBrush(e.Bounds, SelectionFocusGradientLightColor, SelectionFocusGradientDarkColor, 90.0F)) {
                    graphics.FillRectangle(gradientBrush, e.Bounds);
                }
                using (var borderPen = new Pen(SelectionFocusBorderColor)) {
                    graphics.DrawRectangle(borderPen, e.Bounds.Left, e.Bounds.Top, e.Bounds.Width - 1, e.Bounds.Height - 1);
                }
            }
            else {
                // alternate background color for even/odd rows
                Color backColor = e.Index % 2 == 0
                                      ? Color.FromKnownColor(KnownColor.Window)
                                      : Color.FromArgb(0xF6, 0xF6, 0xF6);
                using (Brush backBrush = new SolidBrush(backColor)) {
                    graphics.FillRectangle(backBrush, e.Bounds);
                }
            }

            if (e.Index < 0 || e.Index >= PackageSourcesListBox.Items.Count) {
                return;
            }

            PackageSource currentItem = (PackageSource)PackageSourcesListBox.Items[e.Index];

            using (StringFormat drawFormat = new StringFormat())
            using (Brush foreBrush = new SolidBrush(Color.FromKnownColor(KnownColor.WindowText)))
            using (Brush sourceBrush = new SolidBrush(Color.FromKnownColor(KnownColor.Navy)))
            using (Font italicFont = new Font(e.Font, FontStyle.Italic)) {
                drawFormat.Alignment = StringAlignment.Near;
                drawFormat.Trimming = StringTrimming.EllipsisCharacter;
                drawFormat.LineAlignment = StringAlignment.Near;

                // the margin between the checkbox and the edge of the list box
                const int edgeMargin = 8;
                // the margin between the checkbox and the text
                const int textMargin = 4;

                // draw the enabled/disabled checkbox
                CheckBoxState checkBoxState = currentItem.IsEnabled ? CheckBoxState.CheckedNormal : CheckBoxState.UncheckedNormal;
                Size checkBoxSize = CheckBoxRenderer.GetGlyphSize(graphics, checkBoxState);
                CheckBoxRenderer.DrawCheckBox(
                    graphics,
                    new Point(edgeMargin, e.Bounds.Top + edgeMargin),
                    checkBoxState);

                if (_checkBoxSize.IsEmpty) {
                    // save the checkbox size so that we can detect mouse click on the 
                    // checkbox in the MouseUp event handler.
                    // here we assume that all checkboxes have the same size, which is reasonable. 
                    _checkBoxSize = checkBoxSize;
                }

                GraphicsState oldState = graphics.Save();
                try {
                    // turn on high quality text rendering mode
                    graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                    // draw each package source as
                    // 
                    // [checkbox] Name
                    //            Source (italics)

                    // resize the bound rectangle to make room for the checkbox above
                    var textBounds = new Rectangle(
                        e.Bounds.Left + checkBoxSize.Width + edgeMargin + textMargin,
                        e.Bounds.Top,
                        e.Bounds.Width - checkBoxSize.Width - edgeMargin - textMargin,
                        e.Bounds.Height);

                    graphics.DrawString(currentItem.Name, e.Font, foreBrush, textBounds, drawFormat);
                    SizeF nameSize = graphics.MeasureString(currentItem.Name, e.Font, textBounds.Width, drawFormat);

                    var sourceBounds = NewBounds(textBounds, 0, (int)nameSize.Height);
                    graphics.DrawString(currentItem.Source, italicFont, sourceBrush, sourceBounds, drawFormat);
                }
                finally {
                    graphics.Restore(oldState);
                }

                // If the ListBox has focus, draw a focus rectangle around the selected item.
                e.DrawFocusRectangle();
            }
        }

        private void PackageSourcesListBox_MeasureItem(object sender, MeasureItemEventArgs e) {
            if (e.Index < 0 || e.Index >= PackageSourcesListBox.Items.Count) {
                return;
            }

            PackageSource currentItem = (PackageSource)PackageSourcesListBox.Items[e.Index];
            using (StringFormat drawFormat = new StringFormat())
            using (Font italicFont = new Font(Font, FontStyle.Italic)) {
                drawFormat.Alignment = StringAlignment.Near;
                drawFormat.Trimming = StringTrimming.EllipsisCharacter;
                drawFormat.LineAlignment = StringAlignment.Near;

                SizeF nameLineHeight = e.Graphics.MeasureString(currentItem.Name, Font, e.ItemWidth, drawFormat);
                SizeF sourceLineHeight = e.Graphics.MeasureString(currentItem.Source, italicFont, e.ItemWidth, drawFormat);

                e.ItemHeight = (int)Math.Ceiling(nameLineHeight.Height + sourceLineHeight.Height);
            }
        }

        private void PackageSourcesListBox_MouseMove(object sender, MouseEventArgs e) {
            int index = PackageSourcesListBox.IndexFromPoint(e.X, e.Y);

            if (index >= 0 && index < PackageSourcesListBox.Items.Count && e.Y <= PackageSourcesListBox.PreferredHeight) {
                string newToolTip = ((PackageSource)PackageSourcesListBox.Items[index]).Source;
                string currentToolTip = packageListToolTip.GetToolTip(PackageSourcesListBox);
                if (currentToolTip != newToolTip) {
                    packageListToolTip.SetToolTip(PackageSourcesListBox, newToolTip);
                }
            }
            else {
                packageListToolTip.SetToolTip(PackageSourcesListBox, null);
                packageListToolTip.Hide(PackageSourcesListBox);
            }
        }

        private static Rectangle NewBounds(Rectangle sourceBounds, int xOffset, int yOffset) {
            return new Rectangle(sourceBounds.Left + xOffset, sourceBounds.Top + yOffset,
                sourceBounds.Width - xOffset, sourceBounds.Height - yOffset);
        }

        private void OnBrowseButtonClicked(object sender, EventArgs e) {
            const int MaxDirectoryLength = 1000;

            //const int BIF_RETURNONLYFSDIRS = 0x00000001;   // For finding a folder to start document searching.
            const int BIF_BROWSEINCLUDEURLS = 0x00000080;   // Allow URLs to be displayed or entered.

            var uiShell = (IVsUIShell2)_serviceProvider.GetService(typeof(SVsUIShell));

            char[] rgch = new char[MaxDirectoryLength + 1];

            // allocate a buffer in unmanaged memory for file name (string)
            IntPtr bufferPtr = Marshal.AllocCoTaskMem((rgch.Length + 1) * 2);
            // copy initial path to bufferPtr
            Marshal.Copy(rgch, 0, bufferPtr, rgch.Length);

            VSBROWSEINFOW[] pBrowse = new VSBROWSEINFOW[1];
            pBrowse[0] = new VSBROWSEINFOW() {
                lStructSize = (uint)Marshal.SizeOf(pBrowse[0]),
                dwFlags = (uint)(BIF_BROWSEINCLUDEURLS),
                pwzDlgTitle = Resources.BrowseFolderDialogDescription,
                nMaxDirName = (uint)MaxDirectoryLength,
                hwndOwner = this.Handle,
                pwzDirName = bufferPtr,
                pwzInitialDir = DetermineInitialDirectory()
            };

            var browseInfo = new VSNSEBROWSEINFOW[1] { new VSNSEBROWSEINFOW() };

            int ret = uiShell.GetDirectoryViaBrowseDlgEx(pBrowse, "", Resources.BrowseFolderDialogSelectButton, "", browseInfo);
            if (ret == VSConstants.S_OK) {
                var pathPtr = pBrowse[0].pwzDirName;
                var path = Marshal.PtrToStringAuto(pathPtr);
                NewPackageSource.Text = path;

                // if the package name text box is empty, we fill it with the selected folder's name
                if (String.IsNullOrEmpty(NewPackageName.Text)) {
                    NewPackageName.Text = Path.GetFileName(path);
                }
            }
        }

        private string DetermineInitialDirectory() {
            // determine the inital directory to show in the folder dialog
            string initialDir = NewPackageSource.Text;

            if (IsPathRootedSafe(initialDir) && Directory.Exists(initialDir)) {
                return initialDir;
            }

            var selectedItem = (PackageSource)PackageSourcesListBox.SelectedItem;
            if (selectedItem != null) {
                initialDir = selectedItem.Source;
                if (IsPathRootedSafe(initialDir)) {
                    return initialDir;
                }
            }

            // fallback to MyDocuments folder
            return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        }

        private static bool IsPathRootedSafe(string path) {
            // Check to make sure path does not contain any invalid chars.
            // Otherwise, Path.IsPathRooted() will throw an ArgumentException.
            return path.IndexOfAny(Path.GetInvalidPathChars()) == -1 && Path.IsPathRooted(path);
        }
    }

    internal enum TryAddSourceResults {
        NothingAdded = 0,
        SourceAdded = 1,
        InvalidSource = 2,
        SourceAlreadyAdded = 3
    }
}
