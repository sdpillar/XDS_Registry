namespace XdsRegistry
{
    partial class frmRegistry
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.logWindow = new System.Windows.Forms.TextBox();
            this.btnClose = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.lblRegUri = new System.Windows.Forms.Label();
            this.tmrRegistryConn = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // logWindow
            // 
            this.logWindow.BackColor = System.Drawing.Color.White;
            this.logWindow.Location = new System.Drawing.Point(0, 0);
            this.logWindow.Multiline = true;
            this.logWindow.Name = "logWindow";
            this.logWindow.ReadOnly = true;
            this.logWindow.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.logWindow.Size = new System.Drawing.Size(532, 535);
            this.logWindow.TabIndex = 5;
            // 
            // btnClose
            // 
            this.btnClose.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.btnClose.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnClose.Location = new System.Drawing.Point(12, 577);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(130, 27);
            this.btnClose.TabIndex = 6;
            this.btnClose.Text = "CLOSE";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(13, 548);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(109, 16);
            this.label1.TabIndex = 7;
            this.label1.Text = "Registry URI - ";
            // 
            // lblRegUri
            // 
            this.lblRegUri.AutoSize = true;
            this.lblRegUri.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblRegUri.Location = new System.Drawing.Point(122, 548);
            this.lblRegUri.Name = "lblRegUri";
            this.lblRegUri.Size = new System.Drawing.Size(0, 16);
            this.lblRegUri.TabIndex = 8;
            // 
            // tmrRegistryConn
            // 
            this.tmrRegistryConn.Enabled = true;
            this.tmrRegistryConn.Interval = 3600000;
            this.tmrRegistryConn.Tick += new System.EventHandler(this.tmrRepositoryConn_Tick);
            // 
            // frmRegistry
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.ClientSize = new System.Drawing.Size(530, 607);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.lblRegUri);
            this.Controls.Add(this.btnClose);
            this.Controls.Add(this.logWindow);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.MaximizeBox = false;
            this.Name = "frmRegistry";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
            this.Text = "HSS XDS Registry";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox logWindow;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label lblRegUri;
        private System.Windows.Forms.Timer tmrRegistryConn;
    }
}

