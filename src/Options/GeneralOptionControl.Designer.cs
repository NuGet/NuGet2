namespace NuGet.Options {
    partial class GeneralOptionControl {
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GeneralOptionControl));
            this.ClearButton = new System.Windows.Forms.Button();
            this.checkForUpdate = new System.Windows.Forms.CheckBox();
            this.clearPackageCacheButton = new System.Windows.Forms.Button();
            this.browsePackageCacheButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // ClearButton
            // 
            resources.ApplyResources(this.ClearButton, "ClearButton");
            this.ClearButton.Name = "ClearButton";
            this.ClearButton.UseVisualStyleBackColor = true;
            this.ClearButton.Click += new System.EventHandler(this.OnClearRecentPackagesClick);
            // 
            // checkForUpdate
            // 
            resources.ApplyResources(this.checkForUpdate, "checkForUpdate");
            this.checkForUpdate.Name = "checkForUpdate";
            this.checkForUpdate.UseVisualStyleBackColor = true;
            // 
            // clearPackageCacheButton
            // 
            resources.ApplyResources(this.clearPackageCacheButton, "clearPackageCacheButton");
            this.clearPackageCacheButton.Name = "clearPackageCacheButton";
            this.clearPackageCacheButton.UseVisualStyleBackColor = true;
            this.clearPackageCacheButton.Click += new System.EventHandler(this.OnClearPackageCacheClick);
            // 
            // browsePackageCacheButton
            // 
            resources.ApplyResources(this.browsePackageCacheButton, "browsePackageCacheButton");
            this.browsePackageCacheButton.Name = "browsePackageCacheButton";
            this.browsePackageCacheButton.UseVisualStyleBackColor = true;
            this.browsePackageCacheButton.Click += new System.EventHandler(this.OnBrowsePackageCacheClick);
            // 
            // GeneralOptionControl
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.browsePackageCacheButton);
            this.Controls.Add(this.clearPackageCacheButton);
            this.Controls.Add(this.checkForUpdate);
            this.Controls.Add(this.ClearButton);
            this.Name = "GeneralOptionControl";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button ClearButton;
        private System.Windows.Forms.CheckBox checkForUpdate;
        private System.Windows.Forms.Button clearPackageCacheButton;
        private System.Windows.Forms.Button browsePackageCacheButton;
    }
}
