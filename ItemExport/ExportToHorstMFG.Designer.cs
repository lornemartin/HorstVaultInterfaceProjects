namespace ItemExport
{
    partial class ExportToHorstMFG
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
            this.btnExport = new System.Windows.Forms.Button();
            this.lblOrderNumber = new System.Windows.Forms.Label();
            this.txtboxOrderNumber = new System.Windows.Forms.TextBox();
            this.lblCustomerName = new System.Windows.Forms.Label();
            this.txtkBoxCustomerName = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // btnExport
            // 
            this.btnExport.Location = new System.Drawing.Point(302, 168);
            this.btnExport.Name = "btnExport";
            this.btnExport.Size = new System.Drawing.Size(75, 23);
            this.btnExport.TabIndex = 0;
            this.btnExport.Text = "Export";
            this.btnExport.UseVisualStyleBackColor = true;
            this.btnExport.Click += new System.EventHandler(this.btnExport_Click);
            // 
            // lblOrderNumber
            // 
            this.lblOrderNumber.AutoSize = true;
            this.lblOrderNumber.Location = new System.Drawing.Point(69, 43);
            this.lblOrderNumber.Name = "lblOrderNumber";
            this.lblOrderNumber.Size = new System.Drawing.Size(73, 13);
            this.lblOrderNumber.TabIndex = 1;
            this.lblOrderNumber.Text = "Order Number";
            // 
            // txtboxOrderNumber
            // 
            this.txtboxOrderNumber.Location = new System.Drawing.Point(159, 43);
            this.txtboxOrderNumber.Name = "txtboxOrderNumber";
            this.txtboxOrderNumber.Size = new System.Drawing.Size(100, 20);
            this.txtboxOrderNumber.TabIndex = 2;
            // 
            // lblCustomerName
            // 
            this.lblCustomerName.AutoSize = true;
            this.lblCustomerName.Location = new System.Drawing.Point(69, 106);
            this.lblCustomerName.Name = "lblCustomerName";
            this.lblCustomerName.Size = new System.Drawing.Size(82, 13);
            this.lblCustomerName.TabIndex = 1;
            this.lblCustomerName.Text = "Customer Name";
            // 
            // txtkBoxCustomerName
            // 
            this.txtkBoxCustomerName.Location = new System.Drawing.Point(159, 99);
            this.txtkBoxCustomerName.Name = "txtkBoxCustomerName";
            this.txtkBoxCustomerName.Size = new System.Drawing.Size(100, 20);
            this.txtkBoxCustomerName.TabIndex = 2;
            // 
            // ExportToHorstMFG
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(413, 236);
            this.Controls.Add(this.txtkBoxCustomerName);
            this.Controls.Add(this.txtboxOrderNumber);
            this.Controls.Add(this.lblCustomerName);
            this.Controls.Add(this.lblOrderNumber);
            this.Controls.Add(this.btnExport);
            this.Name = "ExportToHorstMFG";
            this.Text = "ExportToHorstMFG";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnExport;
        private System.Windows.Forms.Label lblOrderNumber;
        private System.Windows.Forms.TextBox txtboxOrderNumber;
        private System.Windows.Forms.Label lblCustomerName;
        private System.Windows.Forms.TextBox txtkBoxCustomerName;
    }
}