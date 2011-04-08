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
            this.RecentPackagesHeader = new System.Windows.Forms.Label();
            this.RecentPackagesHeaderDivider = new System.Windows.Forms.GroupBox();
            this.UpdateHeaderDivider = new System.Windows.Forms.GroupBox();
            this.UpdateHeader = new System.Windows.Forms.Label();
            this.PackagesCacheHeaderDivider = new System.Windows.Forms.GroupBox();
            this.PackagesCacheHeader = new System.Windows.Forms.Label();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel1.SuspendLayout();
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
            // RecentPackagesHeader
            // 
            resources.ApplyResources(this.RecentPackagesHeader, "RecentPackagesHeader");
            this.RecentPackagesHeader.Name = "RecentPackagesHeader";
            // 
            // RecentPackagesHeaderDivider
            // 
            resources.ApplyResources(this.RecentPackagesHeaderDivider, "RecentPackagesHeaderDivider");
            this.RecentPackagesHeaderDivider.Name = "RecentPackagesHeaderDivider";
            this.RecentPackagesHeaderDivider.TabStop = false;
            // 
            // UpdateHeaderDivider
            // 
            resources.ApplyResources(this.UpdateHeaderDivider, "UpdateHeaderDivider");
            this.UpdateHeaderDivider.Name = "UpdateHeaderDivider";
            this.UpdateHeaderDivider.TabStop = false;
            // 
            // UpdateHeader
            // 
            resources.ApplyResources(this.UpdateHeader, "UpdateHeader");
            this.UpdateHeader.Name = "UpdateHeader";
            // 
            // PackagesCacheHeaderDivider
            // 
            resources.ApplyResources(this.PackagesCacheHeaderDivider, "PackagesCacheHeaderDivider");
            this.PackagesCacheHeaderDivider.Name = "PackagesCacheHeaderDivider";
            this.PackagesCacheHeaderDivider.TabStop = false;
            // 
            // PackagesCacheHeader
            // 
            resources.ApplyResources(this.PackagesCacheHeader, "PackagesCacheHeader");
            this.PackagesCacheHeader.Name = "PackagesCacheHeader";
            // 
            // tableLayoutPanel1
            // 
            resources.ApplyResources(this.tableLayoutPanel1, "tableLayoutPanel1");
            this.tableLayoutPanel1.Controls.Add(this.clearPackageCacheButton, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.browsePackageCacheButton, 1, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            // 
            // GeneralOptionControl
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.UpdateHeader);
            this.Controls.Add(this.UpdateHeaderDivider);
            this.Controls.Add(this.checkForUpdate);
            this.Controls.Add(this.RecentPackagesHeader);
            this.Controls.Add(this.RecentPackagesHeaderDivider);
            this.Controls.Add(this.ClearButton);
            this.Controls.Add(this.PackagesCacheHeader);
            this.Controls.Add(this.PackagesCacheHeaderDivider);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "GeneralOptionControl";
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button ClearButton;
        private System.Windows.Forms.CheckBox checkForUpdate;
        private System.Windows.Forms.Button clearPackageCacheButton;
        private System.Windows.Forms.Button browsePackageCacheButton;
        private System.Windows.Forms.Label RecentPackagesHeader;
        private System.Windows.Forms.GroupBox RecentPackagesHeaderDivider;
        private System.Windows.Forms.GroupBox UpdateHeaderDivider;
        private System.Windows.Forms.Label UpdateHeader;
        private System.Windows.Forms.GroupBox PackagesCacheHeaderDivider;
        private System.Windows.Forms.Label PackagesCacheHeader;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
    }
}
