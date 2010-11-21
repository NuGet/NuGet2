using System.Windows.Forms;
namespace NuGet.Dialog.ToolsOptionsUI {
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
            this.HeaderLabel = new System.Windows.Forms.Label();
            this.NewPackageSource = new System.Windows.Forms.TextBox();
            this.addButton = new System.Windows.Forms.Button();
            this.PackageSourcesContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.CopyPackageSourceStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.removeButton = new System.Windows.Forms.Button();
            this.NewPackageName = new System.Windows.Forms.TextBox();
            this.NewPackageNameLabel = new System.Windows.Forms.Label();
            this.NewPackageSourceLabel = new System.Windows.Forms.Label();
            this.MoveUpButton = new System.Windows.Forms.Button();
            this.MoveDownButton = new System.Windows.Forms.Button();
            this.PackageSourcesListBox = new System.Windows.Forms.ListBox();
            this.PackageSourcesContextMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // HeaderLabel
            // 
            resources.ApplyResources(this.HeaderLabel, "HeaderLabel");
            this.HeaderLabel.Name = "HeaderLabel";
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
            // NewPackageName
            // 
            resources.ApplyResources(this.NewPackageName, "NewPackageName");
            this.NewPackageName.Name = "NewPackageName";
            // 
            // NewPackageNameLabel
            // 
            resources.ApplyResources(this.NewPackageNameLabel, "NewPackageNameLabel");
            this.NewPackageNameLabel.Name = "NewPackageNameLabel";
            // 
            // NewPackageSourceLabel
            // 
            resources.ApplyResources(this.NewPackageSourceLabel, "NewPackageSourceLabel");
            this.NewPackageSourceLabel.Name = "NewPackageSourceLabel";
            // 
            // MoveUpButton
            // 
            resources.ApplyResources(this.MoveUpButton, "MoveUpButton");
            this.MoveUpButton.Name = "MoveUpButton";
            this.MoveUpButton.UseVisualStyleBackColor = true;
            // 
            // MoveDownButton
            // 
            resources.ApplyResources(this.MoveDownButton, "MoveDownButton");
            this.MoveDownButton.Name = "MoveDownButton";
            this.MoveDownButton.UseVisualStyleBackColor = true;
            // 
            // PackageSourcesListBox
            // 
            resources.ApplyResources(this.PackageSourcesListBox, "PackageSourcesListBox");
            this.PackageSourcesListBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.PackageSourcesListBox.ContextMenuStrip = this.PackageSourcesContextMenu;
            this.PackageSourcesListBox.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.PackageSourcesListBox.FormattingEnabled = true;
            this.PackageSourcesListBox.Name = "PackageSourcesListBox";
            this.PackageSourcesListBox.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.PackageSourcesListBox_DrawItem);
            this.PackageSourcesListBox.KeyUp += new System.Windows.Forms.KeyEventHandler(this.PackageSourcesListBox_KeyUp);
            this.PackageSourcesListBox.MouseUp += new System.Windows.Forms.MouseEventHandler(this.PackageSourcesListBox_MouseUp);
            // 
            // ToolsOptionsControl
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.PackageSourcesListBox);
            this.Controls.Add(this.MoveDownButton);
            this.Controls.Add(this.MoveUpButton);
            this.Controls.Add(this.HeaderLabel);
            this.Controls.Add(this.NewPackageName);
            this.Controls.Add(this.NewPackageNameLabel);
            this.Controls.Add(this.NewPackageSource);
            this.Controls.Add(this.NewPackageSourceLabel);
            this.Controls.Add(this.addButton);
            this.Controls.Add(this.removeButton);
            this.Name = "ToolsOptionsControl";
            this.PackageSourcesContextMenu.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label HeaderLabel;
        private System.Windows.Forms.TextBox NewPackageSource;
        private System.Windows.Forms.Button addButton;
        private System.Windows.Forms.Button removeButton;
        private ContextMenuStrip PackageSourcesContextMenu;
        private ToolStripMenuItem CopyPackageSourceStripMenuItem;
        private TextBox NewPackageName;
        private Label NewPackageNameLabel;
        private Label NewPackageSourceLabel;
        private Button MoveUpButton;
        private Button MoveDownButton;
        private ListBox PackageSourcesListBox;
    }
}
