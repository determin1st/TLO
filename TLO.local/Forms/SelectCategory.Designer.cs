namespace TLO.local
{
    partial class SelectCategory
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
            this.treeView1 = new System.Windows.Forms.TreeView();
            this._btCancel = new System.Windows.Forms.Button();
            this._btSelected = new System.Windows.Forms.Button();
            this._txtFrom = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // treeView1
            // 
            this.treeView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.treeView1.Location = new System.Drawing.Point(12, 12);
            this.treeView1.Name = "treeView1";
            this.treeView1.Size = new System.Drawing.Size(468, 495);
            this.treeView1.TabIndex = 0;
            this.treeView1.DoubleClick += new System.EventHandler(this._btSelected_Click);
            // 
            // _btCancel
            // 
            this._btCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._btCancel.Location = new System.Drawing.Point(405, 513);
            this._btCancel.Name = "_btCancel";
            this._btCancel.Size = new System.Drawing.Size(75, 23);
            this._btCancel.TabIndex = 1;
            this._btCancel.Text = "Отмена";
            this._btCancel.UseVisualStyleBackColor = true;
            this._btCancel.Click += new System.EventHandler(this._btCancel_Click);
            // 
            // _btSelected
            // 
            this._btSelected.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._btSelected.Location = new System.Drawing.Point(324, 513);
            this._btSelected.Name = "_btSelected";
            this._btSelected.Size = new System.Drawing.Size(75, 23);
            this._btSelected.TabIndex = 2;
            this._btSelected.Text = "Выбрать";
            this._btSelected.UseVisualStyleBackColor = true;
            this._btSelected.Click += new System.EventHandler(this._btSelected_Click);
            // 
            // _txtFrom
            // 
            this._txtFrom.Location = new System.Drawing.Point(12, 513);
            this._txtFrom.Name = "_txtFrom";
            this._txtFrom.Size = new System.Drawing.Size(306, 20);
            this._txtFrom.TabIndex = 3;
            this._txtFrom.KeyDown += new System.Windows.Forms.KeyEventHandler(this._txtFrom_KeyDown);
            // 
            // SelectCategory
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(492, 548);
            this.ControlBox = false;
            this.Controls.Add(this._txtFrom);
            this.Controls.Add(this._btSelected);
            this.Controls.Add(this._btCancel);
            this.Controls.Add(this.treeView1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "SelectCategory";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Выбор категории";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TreeView treeView1;
        private System.Windows.Forms.Button _btCancel;
        private System.Windows.Forms.Button _btSelected;
        private System.Windows.Forms.TextBox _txtFrom;
    }
}