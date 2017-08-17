using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace TLO.local.Forms
{
    public partial class FolderNameDialog : Form
    {
        public string SelectedPath { get { return txtFolderName.Text; } set { txtFolderName.Text = string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim(); } }
        public FolderNameDialog()
        {
            InitializeComponent();
        }

        private void ClickButton(object sender, EventArgs e)
        {
            if (sender == btAbort)
            {
                DialogResult = System.Windows.Forms.DialogResult.Abort;
                Close();
            }
            else if (sender == btCancel)
            {
                DialogResult = System.Windows.Forms.DialogResult.Cancel;
                Close();
            }
            else if (sender == btOk)
            {
                var chars = Path.GetInvalidFileNameChars();
                foreach (var c in chars)
                {
                    if (SelectedPath.Contains(c))
                    {
                        MessageBox.Show("Название каталога содержит недопустимый симвод: " + c);
                        return;
                    }
                }
                DialogResult = System.Windows.Forms.DialogResult.OK;
                Close();
            }
        }
    }
}
