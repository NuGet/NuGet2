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
            this.RepositoryURITextBox = new System.Windows.Forms.TextBox();
            this.NPackURILabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // RepositoryURITextBox
            // 
            this.RepositoryURITextBox.Location = new System.Drawing.Point(28, 29);
            this.RepositoryURITextBox.Name = "RepositoryURITextBox";
            this.RepositoryURITextBox.Size = new System.Drawing.Size(428, 20);
            this.RepositoryURITextBox.TabIndex = 0;
            // 
            // NPackURILabel
            // 
            this.NPackURILabel.AutoSize = true;
            this.NPackURILabel.Location = new System.Drawing.Point(25, 13);
            this.NPackURILabel.Name = "NPackURILabel";
            this.NPackURILabel.Size = new System.Drawing.Size(79, 13);
            this.NPackURILabel.TabIndex = 1;
            this.NPackURILabel.Text = "Repository URI";
            // 
            // ToolsOptionsControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.NPackURILabel);
            this.Controls.Add(this.RepositoryURITextBox);
            this.Name = "ToolsOptionsControl";
            this.Size = new System.Drawing.Size(522, 173);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label NPackURILabel;
        public System.Windows.Forms.TextBox RepositoryURITextBox;
    }
}
