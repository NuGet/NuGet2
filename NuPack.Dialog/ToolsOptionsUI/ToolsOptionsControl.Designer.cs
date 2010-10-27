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
            this.defaultButton = new System.Windows.Forms.Button();
            this.NewPackageName = new System.Windows.Forms.TextBox();
            this.NewPackageNameLabel = new System.Windows.Forms.Label();
            this.NewPackageSourceLabel = new System.Windows.Forms.Label();
            this.PackageSourcesListView = new System.Windows.Forms.ListView();
            this.NameColumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.SourceColumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.PackageSourcesImages = new System.Windows.Forms.ImageList(this.components);
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
            // defaultButton
            // 
            resources.ApplyResources(this.defaultButton, "defaultButton");
            this.defaultButton.Name = "defaultButton";
            this.defaultButton.UseVisualStyleBackColor = true;
            this.defaultButton.Click += new System.EventHandler(this.OnDefaultPackageSourceButtonClick);
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
            // PackageSourcesListView
            // 
            resources.ApplyResources(this.PackageSourcesListView, "PackageSourcesListView");
            this.PackageSourcesListView.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.PackageSourcesListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.NameColumn,
            this.SourceColumn});
            this.PackageSourcesListView.FullRowSelect = true;
            this.PackageSourcesListView.HideSelection = false;
            this.PackageSourcesListView.LargeImageList = this.PackageSourcesImages;
            this.PackageSourcesListView.MultiSelect = false;
            this.PackageSourcesListView.Name = "PackageSourcesListView";
            this.PackageSourcesListView.SmallImageList = this.PackageSourcesImages;
            this.PackageSourcesListView.UseCompatibleStateImageBehavior = false;
            this.PackageSourcesListView.View = System.Windows.Forms.View.Details;
            this.PackageSourcesListView.MouseClick += new System.Windows.Forms.MouseEventHandler(this.PackageSourcesListView_MouseClick);
            // 
            // NameColumn
            // 
            resources.ApplyResources(this.NameColumn, "NameColumn");
            // 
            // SourceColumn
            // 
            resources.ApplyResources(this.SourceColumn, "SourceColumn");
            // 
            // PackageSourcesImages
            // 
            this.PackageSourcesImages.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("PackageSourcesImages.ImageStream")));
            this.PackageSourcesImages.TransparentColor = System.Drawing.Color.Transparent;
            this.PackageSourcesImages.Images.SetKeyName(0, "checkmark.png");
            // 
            // ToolsOptionsControl
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.HeaderLabel);
            this.Controls.Add(this.PackageSourcesListView);
            this.Controls.Add(this.NewPackageName);
            this.Controls.Add(this.NewPackageNameLabel);
            this.Controls.Add(this.NewPackageSource);
            this.Controls.Add(this.NewPackageSourceLabel);
            this.Controls.Add(this.addButton);
            this.Controls.Add(this.defaultButton);
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
        private System.Windows.Forms.Button defaultButton;
        private ContextMenuStrip PackageSourcesContextMenu;
        private ToolStripMenuItem CopyPackageSourceStripMenuItem;
        private TextBox NewPackageName;
        private Label NewPackageNameLabel;
        private Label NewPackageSourceLabel;
        private System.Windows.Forms.ListView PackageSourcesListView;
        private ColumnHeader NameColumn;
        private ColumnHeader SourceColumn;
        private ImageList PackageSourcesImages;
    }
}
