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
            this.NPackURILabel.AutoSize = true;
            this.NPackURILabel.Location = new System.Drawing.Point(3, 9);
            this.NPackURILabel.Name = "NPackURILabel";
            this.NPackURILabel.Size = new System.Drawing.Size(177, 17);
            this.NPackURILabel.TabIndex = 0;
            this.NPackURILabel.Text = "Available package sources:";
            // 
            // NewPackageSource
            // 
            this.NewPackageSource.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.NewPackageSource.Location = new System.Drawing.Point(7, 167);
            this.NewPackageSource.Margin = new System.Windows.Forms.Padding(4);
            this.NewPackageSource.Name = "NewPackageSource";
            this.NewPackageSource.Size = new System.Drawing.Size(401, 22);
            this.NewPackageSource.TabIndex = 4;
            // 
            // addButton
            // 
            this.addButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.addButton.Location = new System.Drawing.Point(415, 164);
            this.addButton.Name = "addButton";
            this.addButton.Size = new System.Drawing.Size(105, 28);
            this.addButton.TabIndex = 5;
            this.addButton.Text = "&Add";
            this.addButton.UseVisualStyleBackColor = true;
            this.addButton.Click += new System.EventHandler(this.OnAddButtonClick);
            // 
            // PackageSourcesListBox
            // 
            this.PackageSourcesListBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.PackageSourcesListBox.ContextMenuStrip = this.PackageSourcesContextMenu;
            this.PackageSourcesListBox.DisplayMember = "Source";
            this.PackageSourcesListBox.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.PackageSourcesListBox.FormattingEnabled = true;
            this.PackageSourcesListBox.ItemHeight = 20;
            this.PackageSourcesListBox.Location = new System.Drawing.Point(7, 36);
            this.PackageSourcesListBox.Name = "PackageSourcesListBox";
            this.PackageSourcesListBox.Size = new System.Drawing.Size(401, 124);
            this.PackageSourcesListBox.TabIndex = 1;
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
            this.PackageSourcesContextMenu.Size = new System.Drawing.Size(153, 48);
            this.PackageSourcesContextMenu.ItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.PackageSourcesContextMenu_ItemClicked);
            // 
            // CopyPackageSourceStripMenuItem
            // 
            this.CopyPackageSourceStripMenuItem.Name = "CopyPackageSourceStripMenuItem";
            this.CopyPackageSourceStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.CopyPackageSourceStripMenuItem.Text = "Copy";
            // 
            // removeButton
            // 
            this.removeButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.removeButton.Location = new System.Drawing.Point(415, 70);
            this.removeButton.Name = "removeButton";
            this.removeButton.Size = new System.Drawing.Size(106, 28);
            this.removeButton.TabIndex = 3;
            this.removeButton.Text = "&Remove";
            this.removeButton.UseVisualStyleBackColor = true;
            this.removeButton.Click += new System.EventHandler(this.OnRemoveButtonClick);
            // 
            // defaultButton
            // 
            this.defaultButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.defaultButton.Location = new System.Drawing.Point(414, 36);
            this.defaultButton.Name = "defaultButton";
            this.defaultButton.Size = new System.Drawing.Size(106, 28);
            this.defaultButton.TabIndex = 2;
            this.defaultButton.Text = "&Set Default";
            this.defaultButton.UseVisualStyleBackColor = true;
            this.defaultButton.Click += new System.EventHandler(this.OnDefaultPackageSourceButtonClick);
            // 
            // ToolsOptionsControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.defaultButton);
            this.Controls.Add(this.removeButton);
            this.Controls.Add(this.PackageSourcesListBox);
            this.Controls.Add(this.addButton);
            this.Controls.Add(this.NPackURILabel);
            this.Controls.Add(this.NewPackageSource);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "ToolsOptionsControl";
            this.Size = new System.Drawing.Size(529, 214);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label NPackURILabel;
        public System.Windows.Forms.TextBox NewPackageSource;
        private System.Windows.Forms.Button addButton;
        private System.Windows.Forms.ListBox PackageSourcesListBox;
        private System.Windows.Forms.Button removeButton;
        private System.Windows.Forms.Button defaultButton;
        private ContextMenuStrip PackageSourcesContextMenu;
        private ToolStripMenuItem CopyPackageSourceStripMenuItem;
    }
}