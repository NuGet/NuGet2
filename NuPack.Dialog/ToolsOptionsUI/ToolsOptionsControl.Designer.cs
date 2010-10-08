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
            this.NPackURILabel.Location = new System.Drawing.Point(2, 7);
            this.NPackURILabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.NPackURILabel.Name = "NPackURILabel";
            this.NPackURILabel.Size = new System.Drawing.Size(138, 13);
            this.NPackURILabel.TabIndex = 0;
            this.NPackURILabel.Text = "Available package sources:";
            // 
            // NewPackageSource
            // 
            this.NewPackageSource.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.NewPackageSource.Location = new System.Drawing.Point(5, 136);
            this.NewPackageSource.Name = "NewPackageSource";
            this.NewPackageSource.Size = new System.Drawing.Size(302, 20);
            this.NewPackageSource.TabIndex = 4;
            // 
            // addButton
            // 
            this.addButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.addButton.Location = new System.Drawing.Point(311, 133);
            this.addButton.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.addButton.Name = "addButton";
            this.addButton.Size = new System.Drawing.Size(79, 23);
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
            this.PackageSourcesListBox.Location = new System.Drawing.Point(5, 29);
            this.PackageSourcesListBox.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.PackageSourcesListBox.Name = "PackageSourcesListBox";
            this.PackageSourcesListBox.Size = new System.Drawing.Size(302, 84);
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
            this.removeButton.Location = new System.Drawing.Point(311, 57);
            this.removeButton.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.removeButton.Name = "removeButton";
            this.removeButton.Size = new System.Drawing.Size(80, 23);
            this.removeButton.TabIndex = 3;
            this.removeButton.Text = "&Remove";
            this.removeButton.UseVisualStyleBackColor = true;
            this.removeButton.Click += new System.EventHandler(this.OnRemoveButtonClick);
            // 
            // defaultButton
            // 
            this.defaultButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.defaultButton.Location = new System.Drawing.Point(310, 29);
            this.defaultButton.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.defaultButton.Name = "defaultButton";
            this.defaultButton.Size = new System.Drawing.Size(80, 23);
            this.defaultButton.TabIndex = 2;
            this.defaultButton.Text = "&Set Default";
            this.defaultButton.UseVisualStyleBackColor = true;
            this.defaultButton.Click += new System.EventHandler(this.OnDefaultPackageSourceButtonClick);
            // 
            // ToolsOptionsControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.defaultButton);
            this.Controls.Add(this.removeButton);
            this.Controls.Add(this.PackageSourcesListBox);
            this.Controls.Add(this.addButton);
            this.Controls.Add(this.NPackURILabel);
            this.Controls.Add(this.NewPackageSource);
            this.Name = "ToolsOptionsControl";
            this.Size = new System.Drawing.Size(397, 174);
            this.PackageSourcesContextMenu.ResumeLayout(false);
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