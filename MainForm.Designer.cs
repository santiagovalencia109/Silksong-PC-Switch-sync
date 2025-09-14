namespace SwitchFileSync
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.TextBox txtPcPath;
        private System.Windows.Forms.TextBox txtSwitchPath;
        private System.Windows.Forms.Button btnSendToSwitch;
        private System.Windows.Forms.Button btnSendToPc;
        private System.Windows.Forms.Label lblPcPath;
        private System.Windows.Forms.Label lblSwitchPath;
        private System.Windows.Forms.Button btnBrowsePc;
        private System.Windows.Forms.TreeView treeSwitchExplorer;
        private System.Windows.Forms.Label lblPlaytimePc;
        private System.Windows.Forms.Label lblPlaytimeSwitch;

        private System.Windows.Forms.ProgressBar progressBar;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.txtPcPath = new System.Windows.Forms.TextBox();
            this.txtSwitchPath = new System.Windows.Forms.TextBox();
            this.btnSendToSwitch = new System.Windows.Forms.Button();
            this.btnSendToPc = new System.Windows.Forms.Button();
            this.lblPcPath = new System.Windows.Forms.Label();
            this.lblSwitchPath = new System.Windows.Forms.Label();
            this.btnBrowsePc = new System.Windows.Forms.Button();
            this.treeSwitchExplorer = new System.Windows.Forms.TreeView();
            this.SuspendLayout();

            // lblPlaytimePc
            this.lblPlaytimePc = new System.Windows.Forms.Label();
            this.lblPlaytimePc.Location = new System.Drawing.Point(20, 425);
            this.lblPlaytimePc.Size = new System.Drawing.Size(250, 23);
            this.lblPlaytimePc.Text = "Playtime PC: N/A";
            this.Controls.Add(this.lblPlaytimePc);

            // lblPlaytimeSwitch
            this.lblPlaytimeSwitch = new System.Windows.Forms.Label();
            this.lblPlaytimeSwitch.Location = new System.Drawing.Point(280, 425);
            this.lblPlaytimeSwitch.Size = new System.Drawing.Size(250, 23);
            this.lblPlaytimeSwitch.Text = "Playtime Switch: N/A";
            this.Controls.Add(this.lblPlaytimeSwitch);

            // lblPcPath
            this.lblPcPath.AutoSize = true;
            this.lblPcPath.Location = new System.Drawing.Point(20, 20);
            this.lblPcPath.Name = "lblPcPath";
            this.lblPcPath.Size = new System.Drawing.Size(97, 15);
            this.lblPcPath.TabIndex = 0;
            this.lblPcPath.Text = "PC Save Folder:";
            // 
            // txtPcPath
            // 
            this.txtPcPath.Location = new System.Drawing.Point(20, 40);
            this.txtPcPath.Name = "txtPcPath";
            this.txtPcPath.Size = new System.Drawing.Size(400, 23);
            this.txtPcPath.TabIndex = 1;
            // 
            // btnBrowsePc
            // 
            this.btnBrowsePc.Location = new System.Drawing.Point(430, 40);
            this.btnBrowsePc.Name = "btnBrowsePc";
            this.btnBrowsePc.Size = new System.Drawing.Size(75, 23);
            this.btnBrowsePc.TabIndex = 2;
            this.btnBrowsePc.Text = "Browse";
            this.btnBrowsePc.UseVisualStyleBackColor = true;
            this.btnBrowsePc.Click += new System.EventHandler(this.btnBrowsePc_Click);
            // 
            // lblSwitchPath
            // 
            this.lblSwitchPath.AutoSize = true;
            this.lblSwitchPath.Location = new System.Drawing.Point(20, 80);
            this.lblSwitchPath.Name = "lblSwitchPath";
            this.lblSwitchPath.Size = new System.Drawing.Size(133, 15);
            this.lblSwitchPath.TabIndex = 3;
            this.lblSwitchPath.Text = "Selected Switch Folder:";
            // 
            // txtSwitchPath
            // 
            this.txtSwitchPath.Location = new System.Drawing.Point(20, 100);
            this.txtSwitchPath.Name = "txtSwitchPath";
            this.txtSwitchPath.ReadOnly = true;
            this.txtSwitchPath.Size = new System.Drawing.Size(400, 23);
            this.txtSwitchPath.TabIndex = 4;
            // 
            // btnSendToSwitch
            // 
            this.btnSendToSwitch.Location = new System.Drawing.Point(20, 140);
            this.btnSendToSwitch.Name = "btnSendToSwitch";
            this.btnSendToSwitch.Size = new System.Drawing.Size(200, 35);
            this.btnSendToSwitch.TabIndex = 5;
            this.btnSendToSwitch.Text = "Upload to Switch";
            this.btnSendToSwitch.UseVisualStyleBackColor = true;
            this.btnSendToSwitch.Click += new System.EventHandler(this.btnSendToSwitch_Click);
            // 
            // btnSendToPc
            // 
            this.btnSendToPc.Location = new System.Drawing.Point(250, 140);
            this.btnSendToPc.Name = "btnSendToPc";
            this.btnSendToPc.Size = new System.Drawing.Size(200, 35);
            this.btnSendToPc.TabIndex = 6;
            this.btnSendToPc.Text = "Download to PC";
            this.btnSendToPc.UseVisualStyleBackColor = true;
            this.btnSendToPc.Click += new System.EventHandler(this.btnSendToPc_Click);
            // 
            // treeSwitchExplorer
            // 
            this.treeSwitchExplorer.Location = new System.Drawing.Point(20, 220);
            this.treeSwitchExplorer.Name = "treeSwitchExplorer";
            this.treeSwitchExplorer.Size = new System.Drawing.Size(485, 200);
            this.treeSwitchExplorer.TabIndex = 7;
            this.treeSwitchExplorer.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.treeSwitchExplorer_AfterSelect);
            this.treeSwitchExplorer.BeforeExpand += new System.Windows.Forms.TreeViewCancelEventHandler(this.treeSwitchExplorer_BeforeExpand);
            // 
            // progressBar
            // 
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.progressBar.Location = new System.Drawing.Point(20, 185);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(430, 20);
            this.progressBar.TabIndex = 8;
            this.Controls.Add(this.progressBar);

            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(540, 450);
            this.Controls.Add(this.treeSwitchExplorer);
            this.Controls.Add(this.btnSendToPc);
            this.Controls.Add(this.btnSendToSwitch);
            this.Controls.Add(this.txtSwitchPath);
            this.Controls.Add(this.lblSwitchPath);
            this.Controls.Add(this.lblPlaytimePc);
            this.Controls.Add(this.lblPlaytimeSwitch);
            this.Controls.Add(this.btnBrowsePc);
            this.Controls.Add(this.txtPcPath);
            this.Controls.Add(this.lblPcPath);
            this.Name = "MainForm";
            this.Text = "Hollow Knight Switch/PC Save Sync";
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion
    }
}
