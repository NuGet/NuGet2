using System.Windows.Forms;
namespace NuGet.Options
{
    partial class PackageSourcesOptionsControl
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PackageSourcesOptionsControl));
            this.HeaderLabel = new System.Windows.Forms.Label();
            this.PackageSourcesContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.CopyPackageSourceStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.removeButton = new System.Windows.Forms.Button();
            this.imageList1 = new System.Windows.Forms.ImageList(this.components);
            this.MoveUpButton = new System.Windows.Forms.Button();
            this.MoveDownButton = new System.Windows.Forms.Button();
            this.packageListToolTip = new System.Windows.Forms.ToolTip(this.components);
            this.updateButton = new System.Windows.Forms.Button();
            this.BrowseButton = new System.Windows.Forms.Button();
            this.NewPackageSource = new System.Windows.Forms.TextBox();
            this.NewPackageSourceLabel = new System.Windows.Forms.Label();
            this.NewPackageName = new System.Windows.Forms.TextBox();
            this.NewPackageNameLabel = new System.Windows.Forms.Label();
            this.PackageSourcesListBox = new System.Windows.Forms.ListBox();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.addButton = new System.Windows.Forms.Button();
            this.MachineWideSourcesLabel = new System.Windows.Forms.Label();
            this.MachineWidePackageSourcesListBox = new System.Windows.Forms.ListBox();
            this.imageList2 = new System.Windows.Forms.ImageList(this.components);
            this.PackageSourcesContextMenu.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // HeaderLabel
            // 
            resources.ApplyResources(this.HeaderLabel, "HeaderLabel");
            this.HeaderLabel.Name = "HeaderLabel";
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
            this.removeButton.ImageList = this.imageList1;
            this.removeButton.Name = "removeButton";
            this.removeButton.UseVisualStyleBackColor = true;
            this.removeButton.Click += new System.EventHandler(this.OnRemoveButtonClick);
            // 
            // imageList1
            // 
            this.imageList1.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList1.ImageStream")));
            this.imageList1.TransparentColor = System.Drawing.Color.Transparent;
            this.imageList1.Images.SetKeyName(0, "uparrow.png");
            this.imageList1.Images.SetKeyName(1, "downarrow.png");
            this.imageList1.Images.SetKeyName(2, "Delete.png");
            this.imageList1.Images.SetKeyName(3, "add.png");
            // 
            // MoveUpButton
            // 
            resources.ApplyResources(this.MoveUpButton, "MoveUpButton");
            this.MoveUpButton.ImageList = this.imageList1;
            this.MoveUpButton.Name = "MoveUpButton";
            this.MoveUpButton.UseVisualStyleBackColor = true;
            // 
            // MoveDownButton
            // 
            resources.ApplyResources(this.MoveDownButton, "MoveDownButton");
            this.MoveDownButton.ImageList = this.imageList1;
            this.MoveDownButton.Name = "MoveDownButton";
            this.MoveDownButton.UseVisualStyleBackColor = true;
            // 
            // updateButton
            // 
            resources.ApplyResources(this.updateButton, "updateButton");
            this.updateButton.Name = "updateButton";
            this.updateButton.UseVisualStyleBackColor = true;
            this.updateButton.Click += new System.EventHandler(this.OnUpdateButtonClick);
            // 
            // BrowseButton
            // 
            resources.ApplyResources(this.BrowseButton, "BrowseButton");
            this.BrowseButton.Name = "BrowseButton";
            this.BrowseButton.UseVisualStyleBackColor = true;
            this.BrowseButton.Click += new System.EventHandler(this.OnBrowseButtonClicked);
            // 
            // NewPackageSource
            // 
            resources.ApplyResources(this.NewPackageSource, "NewPackageSource");
            this.NewPackageSource.Name = "NewPackageSource";
            // 
            // NewPackageSourceLabel
            // 
            resources.ApplyResources(this.NewPackageSourceLabel, "NewPackageSourceLabel");
            this.NewPackageSourceLabel.Name = "NewPackageSourceLabel";
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
            // PackageSourcesListBox
            // 
            resources.ApplyResources(this.PackageSourcesListBox, "PackageSourcesListBox");
            this.PackageSourcesListBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.tableLayoutPanel1.SetColumnSpan(this.PackageSourcesListBox, 4);
            this.PackageSourcesListBox.ContextMenuStrip = this.PackageSourcesContextMenu;
            this.PackageSourcesListBox.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawVariable;
            this.PackageSourcesListBox.FormattingEnabled = true;
            this.PackageSourcesListBox.Name = "PackageSourcesListBox";
            this.PackageSourcesListBox.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.PackageSourcesListBox_DrawItem);
            this.PackageSourcesListBox.MeasureItem += new System.Windows.Forms.MeasureItemEventHandler(this.PackageSourcesListBox_MeasureItem);
            this.PackageSourcesListBox.KeyUp += new System.Windows.Forms.KeyEventHandler(this.PackageSourcesListBox_KeyUp);
            this.PackageSourcesListBox.MouseMove += new System.Windows.Forms.MouseEventHandler(this.PackageSourcesListBox_MouseMove);
            this.PackageSourcesListBox.MouseUp += new System.Windows.Forms.MouseEventHandler(this.PackageSourcesListBox_MouseUp);
            // 
            // tableLayoutPanel1
            // 
            resources.ApplyResources(this.tableLayoutPanel1, "tableLayoutPanel1");
            this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel2, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.PackageSourcesListBox, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.MachineWideSourcesLabel, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.MachineWidePackageSourcesListBox, 0, 3);
            this.tableLayoutPanel1.Controls.Add(this.NewPackageNameLabel, 0, 4);
            this.tableLayoutPanel1.Controls.Add(this.NewPackageSourceLabel, 0, 5);
            this.tableLayoutPanel1.Controls.Add(this.NewPackageName, 1, 4);
            this.tableLayoutPanel1.Controls.Add(this.NewPackageSource, 1, 5);
            this.tableLayoutPanel1.Controls.Add(this.BrowseButton, 2, 5);
            this.tableLayoutPanel1.Controls.Add(this.updateButton, 3, 5);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            // 
            // tableLayoutPanel2
            // 
            resources.ApplyResources(this.tableLayoutPanel2, "tableLayoutPanel2");
            this.tableLayoutPanel1.SetColumnSpan(this.tableLayoutPanel2, 4);
            this.tableLayoutPanel2.Controls.Add(this.HeaderLabel, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.addButton, 1, 0);
            this.tableLayoutPanel2.Controls.Add(this.removeButton, 2, 0);
            this.tableLayoutPanel2.Controls.Add(this.MoveUpButton, 3, 0);
            this.tableLayoutPanel2.Controls.Add(this.MoveDownButton, 4, 0);
            this.tableLayoutPanel2.GrowStyle = System.Windows.Forms.TableLayoutPanelGrowStyle.FixedSize;
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            // 
            // addButton
            // 
            resources.ApplyResources(this.addButton, "addButton");
            this.addButton.ImageList = this.imageList1;
            this.addButton.Name = "addButton";
            this.addButton.UseVisualStyleBackColor = true;
            this.addButton.Click += new System.EventHandler(this.OnAddButtonClick);
            // 
            // MachineWideSourcesLabel
            // 
            this.tableLayoutPanel1.SetColumnSpan(this.MachineWideSourcesLabel, 4);
            resources.ApplyResources(this.MachineWideSourcesLabel, "MachineWideSourcesLabel");
            this.MachineWideSourcesLabel.Name = "MachineWideSourcesLabel";
            // 
            // MachineWidePackageSourcesListBox
            // 
            this.MachineWidePackageSourcesListBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.tableLayoutPanel1.SetColumnSpan(this.MachineWidePackageSourcesListBox, 4);
            this.MachineWidePackageSourcesListBox.ContextMenuStrip = this.PackageSourcesContextMenu;
            this.MachineWidePackageSourcesListBox.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawVariable;
            this.MachineWidePackageSourcesListBox.FormattingEnabled = true;
            resources.ApplyResources(this.MachineWidePackageSourcesListBox, "MachineWidePackageSourcesListBox");
            this.MachineWidePackageSourcesListBox.Name = "MachineWidePackageSourcesListBox";
            this.MachineWidePackageSourcesListBox.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.PackageSourcesListBox_DrawItem);
            this.MachineWidePackageSourcesListBox.MeasureItem += new System.Windows.Forms.MeasureItemEventHandler(this.PackageSourcesListBox_MeasureItem);
            this.MachineWidePackageSourcesListBox.KeyUp += new System.Windows.Forms.KeyEventHandler(this.PackageSourcesListBox_KeyUp);
            this.MachineWidePackageSourcesListBox.MouseMove += new System.Windows.Forms.MouseEventHandler(this.PackageSourcesListBox_MouseMove);
            this.MachineWidePackageSourcesListBox.MouseUp += new System.Windows.Forms.MouseEventHandler(this.PackageSourcesListBox_MouseUp);
            // 
            // imageList2
            // 
            this.imageList2.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList2.ImageStream")));
            this.imageList2.TransparentColor = System.Drawing.Color.Transparent;
            this.imageList2.Images.SetKeyName(0, "uparrow.png");
            this.imageList2.Images.SetKeyName(1, "downarrow.png");
            this.imageList2.Images.SetKeyName(2, "delete.png");
            this.imageList2.Images.SetKeyName(3, "addgrayscale.png");
            // 
            // PackageSourcesOptionsControl
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "PackageSourcesOptionsControl";
            this.PackageSourcesContextMenu.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label HeaderLabel;
        private System.Windows.Forms.Button removeButton;
        private ContextMenuStrip PackageSourcesContextMenu;
        private ToolStripMenuItem CopyPackageSourceStripMenuItem;
        private Button MoveUpButton;
        private Button MoveDownButton;
        private ToolTip packageListToolTip;
        private Button updateButton;
        private Button BrowseButton;
        private TextBox NewPackageSource;
        private Label NewPackageSourceLabel;
        private TextBox NewPackageName;
        private TableLayoutPanel tableLayoutPanel1;
        private ListBox PackageSourcesListBox;
        private Label NewPackageNameLabel;
        private TableLayoutPanel tableLayoutPanel2;
        private ImageList imageList1;
        private ImageList imageList2;
        private Button addButton;
        private Label MachineWideSourcesLabel;
        private ListBox MachineWidePackageSourcesListBox;
    }
}
