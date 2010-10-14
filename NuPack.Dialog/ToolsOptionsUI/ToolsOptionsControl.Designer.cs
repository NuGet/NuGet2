using System.Windows.Forms;
namespace NuPack.Dialog.ToolsOptionsUI {
    partial class ToolsOptionsControl {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ToolsOptionsControl));
            this.NPackURILabel = new System.Windows.Forms.Label();
            this.NewPackageSource = new System.Windows.Forms.TextBox();
            this.addButton = new System.Windows.Forms.Button();
            this.PackageSourcesListBox = new System.Windows.Forms.ListBox();
            this.PackageSourcesContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.CopyPackageSourceStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.removeButton = new System.Windows.Forms.Button();
            this.defaultButton = new System.Windows.Forms.Button();
            this.PackageSourcesContextMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // NPackURILabel
            // 
            resources.ApplyResources(this.NPackURILabel, "NPackURILabel");
            this.NPackURILabel.Name = "NPackURILabel";
            // 
            // NewPackageSource
            // 
            resources.ApplyResources(this.NewPackageSource, "NewPackageSource");
            this.NewPackageSource.Name = "NewPackageSource";
            // 
            // addButton
            // 
            resources.ApplyResources(this.addButton, "addButton");
            this.addButton.Name = "addButton";
            this.addButton.UseVisualStyleBackColor = true;
            this.addButton.Click += new System.EventHandler(this.OnAddButtonClick);
            // 
            // PackageSourcesListBox
            // 
            resources.ApplyResources(this.PackageSourcesListBox, "PackageSourcesListBox");
            this.PackageSourcesListBox.ContextMenuStrip = this.PackageSourcesContextMenu;
            this.PackageSourcesListBox.DisplayMember = "Source";
            this.PackageSourcesListBox.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.PackageSourcesListBox.FormattingEnabled = true;
            this.PackageSourcesListBox.Name = "PackageSourcesListBox";
            this.PackageSourcesListBox.ValueMember = "Source";
            this.PackageSourcesListBox.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.AllPackageSourcesList_DrawItem);
            this.PackageSourcesListBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.PackageSourcesListBox_KeyDown);
            this.PackageSourcesListBox.MouseDown += new System.Windows.Forms.MouseEventHandler(this.PackageSourcesListBox_MouseDown);
            // 
            // PackageSourcesContextMenu
            // 
            this.PackageSourcesContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.CopyPackageSourceStripMenuItem});
            this.PackageSourcesContextMenu.Name = "contextMenuStrip1";
            resources.ApplyResources(this.PackageSourcesContextMenu, "PackageSourcesContextMenu");
            this.PackageSourcesContextMenu.ItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.PackageSourcesContextMenu_ItemClicked);
            // 
            // CopyPackageSourceStripMenuItem
            // 
            this.CopyPackageSourceStripMenuItem.Name = "CopyPackageSourceStripMenuItem";
            resources.ApplyResources(this.CopyPackageSourceStripMenuItem, "CopyPackageSourceStripMenuItem");
            // 
            // removeButton
            // 
            resources.ApplyResources(this.removeButton, "removeButton");
            this.removeButton.Name = "removeButton";
            this.removeButton.UseVisualStyleBackColor = true;
            this.removeButton.Click += new System.EventHandler(this.OnRemoveButtonClick);
            // 
            // defaultButton
            // 
            resources.ApplyResources(this.defaultButton, "defaultButton");
            this.defaultButton.Name = "defaultButton";
            this.defaultButton.UseVisualStyleBackColor = true;
            this.defaultButton.Click += new System.EventHandler(this.OnDefaultPackageSourceButtonClick);
            // 
            // ToolsOptionsControl
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.defaultButton);
            this.Controls.Add(this.removeButton);
            this.Controls.Add(this.PackageSourcesListBox);
            this.Controls.Add(this.addButton);
            this.Controls.Add(this.NPackURILabel);
            this.Controls.Add(this.NewPackageSource);
            this.Name = "ToolsOptionsControl";
            this.PackageSourcesContextMenu.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label NPackURILabel;
        private System.Windows.Forms.Button addButton;
        private System.Windows.Forms.ListBox PackageSourcesListBox;
        private System.Windows.Forms.Button removeButton;
        private System.Windows.Forms.Button defaultButton;
        private ContextMenuStrip PackageSourcesContextMenu;
        private ToolStripMenuItem CopyPackageSourceStripMenuItem;
        private TextBox NewPackageSource;
    }
}