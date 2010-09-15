namespace NuPack.Dialog.ToolsOptionsUI
{
    partial class ToolsOptionsControl
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.NPackURILabel = new System.Windows.Forms.Label();
            this.NewPackageSource = new System.Windows.Forms.TextBox();
            this.addButton = new System.Windows.Forms.Button();
            this.PackageSourcesListBox = new System.Windows.Forms.ListBox();
            this.removeButton = new System.Windows.Forms.Button();
            this.defaultButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // NPackURILabel
            // 
            this.NPackURILabel.AutoSize = true;
            this.NPackURILabel.Location = new System.Drawing.Point(4, 16);
            this.NPackURILabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.NPackURILabel.Name = "NPackURILabel";
            this.NPackURILabel.Size = new System.Drawing.Size(177, 17);
            this.NPackURILabel.TabIndex = 1;
            this.NPackURILabel.Text = "Available package sources";
            // 
            // NewPackageSource
            // 
            this.NewPackageSource.Location = new System.Drawing.Point(7, 167);
            this.NewPackageSource.Margin = new System.Windows.Forms.Padding(4);
            this.NewPackageSource.Name = "NewPackageSource";
            this.NewPackageSource.Size = new System.Drawing.Size(397, 22);
            this.NewPackageSource.TabIndex = 0;
            // 
            // addButton
            // 
            this.addButton.Location = new System.Drawing.Point(411, 167);
            this.addButton.Name = "addButton";
            this.addButton.Size = new System.Drawing.Size(94, 23);
            this.addButton.TabIndex = 3;
            this.addButton.Text = "Add";
            this.addButton.UseVisualStyleBackColor = true;
            this.addButton.Click += new System.EventHandler(this.addButton_Click);
            // 
            // PackageSourcesListBox
            // 
            this.PackageSourcesListBox.DisplayMember = "Source";
            this.PackageSourcesListBox.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.PackageSourcesListBox.FormattingEnabled = true;
            this.PackageSourcesListBox.ItemHeight = 20;
            this.PackageSourcesListBox.Location = new System.Drawing.Point(7, 36);
            this.PackageSourcesListBox.Name = "PackageSourcesListBox";
            this.PackageSourcesListBox.Size = new System.Drawing.Size(397, 124);
            this.PackageSourcesListBox.TabIndex = 4;
            this.PackageSourcesListBox.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.AllPackageSourcesList_DrawItem);
            // 
            // removeButton
            // 
            this.removeButton.Location = new System.Drawing.Point(411, 65);
            this.removeButton.Name = "removeButton";
            this.removeButton.Size = new System.Drawing.Size(94, 23);
            this.removeButton.TabIndex = 5;
            this.removeButton.Text = "Remove";
            this.removeButton.UseVisualStyleBackColor = true;
            this.removeButton.Click += new System.EventHandler(this.removeButton_Click);
            // 
            // defaultButton
            // 
            this.defaultButton.Location = new System.Drawing.Point(411, 36);
            this.defaultButton.Name = "defaultButton";
            this.defaultButton.Size = new System.Drawing.Size(94, 23);
            this.defaultButton.TabIndex = 6;
            this.defaultButton.Text = "Set Default";
            this.defaultButton.UseVisualStyleBackColor = true;
            this.defaultButton.Click += new System.EventHandler(this.defaultButton_Click);
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
            this.Size = new System.Drawing.Size(514, 270);
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
    }
}
