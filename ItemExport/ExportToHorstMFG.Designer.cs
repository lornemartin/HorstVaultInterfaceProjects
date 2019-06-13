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
            this.lblQty = new System.Windows.Forms.Label();
            this.txtBoxQty = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // btnExport
            // 
            this.btnExport.Location = new System.Drawing.Point(301, 184);
            this.btnExport.Name = "btnExport";
            this.btnExport.Size = new System.Drawing.Size(75, 23);
            this.btnExport.TabIndex = 3;
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
            this.txtboxOrderNumber.TabIndex = 0;
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
            this.txtkBoxCustomerName.TabIndex = 1;
            // 
            // lblQty
            // 
            this.lblQty.AutoSize = true;
            this.lblQty.Location = new System.Drawing.Point(69, 159);
            this.lblQty.Name = "lblQty";
            this.lblQty.Size = new System.Drawing.Size(46, 13);
            this.lblQty.TabIndex = 1;
            this.lblQty.Text = "Quantity";
            // 
            // txtBoxQty
            // 
            this.txtBoxQty.Location = new System.Drawing.Point(159, 152);
            this.txtBoxQty.Name = "txtBoxQty";
            this.txtBoxQty.Size = new System.Drawing.Size(100, 20);
            this.txtBoxQty.TabIndex = 3;
            // 
            // ExportToHorstMFG
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(413, 236);
            this.Controls.Add(this.txtBoxQty);
            this.Controls.Add(this.txtkBoxCustomerName);
            this.Controls.Add(this.txtboxOrderNumber);
            this.Controls.Add(this.lblQty);
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
        private System.Windows.Forms.Label lblQty;
        private System.Windows.Forms.TextBox txtBoxQty;
    }
}